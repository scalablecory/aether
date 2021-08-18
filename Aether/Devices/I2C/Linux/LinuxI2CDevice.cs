using Aether.Interop;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Aether.Devices.I2C.Linux
{
    /// <summary>
    /// An I2C device for Linux, via SMBus APIs.
    /// </summary>
    internal sealed class LinuxI2CDevice : I2CDevice
    {
        private readonly LinuxI2CBus _bus;
        private readonly FileDescriptorSafeHandle _fd;
        private readonly ushort _addr;

        public unsafe LinuxI2CDevice(LinuxI2CBus bus, string filePath, int deviceAddress)
        {
            int len = Encoding.UTF8.GetByteCount(filePath) + 1;

            byte[] utf8FilePath = new byte[len + 1];

            len = Encoding.UTF8.GetBytes(filePath, utf8FilePath);
            utf8FilePath[len] = 0;

            int fd;

            fixed (byte* utf8FilePathPointer = utf8FilePath)
            {
                fd = Libc.open(utf8FilePathPointer, Libc.O_RDWR);
            }

            CheckError(nameof(Libc.open), fd);

            _bus = bus;
            _fd = new FileDescriptorSafeHandle(fd);
            _addr = (ushort)deviceAddress;

            SetDeviceAddress(deviceAddress);
        }

        public override void Dispose()
        {
            _fd.Dispose();
        }

        private unsafe void SetDeviceAddress(int deviceAddress)
        {
            long addr = deviceAddress;

            int err = Libc.ioctl(_fd.FileDescriptor, Libc.I2C_SLAVE, &addr);
            CheckError(nameof(Libc.ioctl), err);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> writeBuffer, CancellationToken cancellationToken)
        {
            if (writeBuffer.Length == 0) throw new ArgumentException($"{nameof(writeBuffer)} must have a non-zero length.", nameof(writeBuffer));

            await _bus.LockAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Write(writeBuffer.Span);
            }
            finally
            {
                _bus.Unlock();
            }
        }

        private unsafe void Write(ReadOnlySpan<byte> writeBuffer)
        {
            nint len;

            fixed (byte* pWriteBuffer = writeBuffer)
            {
                len = Libc.write(_fd.FileDescriptor, pWriteBuffer, (nuint)writeBuffer.Length);
            }

            CheckError(nameof(Libc.ioctl), len);

            if (len != writeBuffer.Length)
            {
                throw new Exception("Write completed partially.");
            }
        }

        public override async ValueTask ReadAsync(Memory<byte> readBuffer, CancellationToken cancellationToken)
        {
            if (readBuffer.Length == 0) throw new ArgumentException($"{nameof(readBuffer)} must have a non-zero length.", nameof(readBuffer));

            await _bus.LockAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Read(readBuffer.Span);
            }
            finally
            {
                _bus.Unlock();
            }
        }

        private unsafe void Read(Span<byte> readBuffer)
        {
            nint len;

            fixed (byte* pReadBuffer = readBuffer)
            {
                len = Libc.read(_fd.FileDescriptor, pReadBuffer, (nuint)readBuffer.Length);
            }

            CheckError(nameof(Libc.ioctl), len);

            if (len != readBuffer.Length)
            {
                throw new Exception("Read completed partially.");
            }
        }

        public override async ValueTask WriteAndReadAsync(ReadOnlyMemory<byte> writeBuffer, Memory<byte> readBuffer, CancellationToken cancellationToken)
        {
            if (writeBuffer.Length > ushort.MaxValue) throw new ArgumentException($"{nameof(writeBuffer)} must be at most {ushort.MaxValue} bytes in length.", nameof(writeBuffer));
            if (readBuffer.Length > ushort.MaxValue) throw new ArgumentException($"{nameof(readBuffer)} must be at most {ushort.MaxValue} bytes in length.", nameof(readBuffer));
            if (writeBuffer.Length == 0 && readBuffer.Length == 0) throw new ArgumentException($"One of {nameof(writeBuffer)} or {nameof(readBuffer)} must have a non-zero length.");

            await _bus.LockAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                WriteAndRead(writeBuffer.Span, readBuffer.Span);
            }
            finally
            {
                _bus.Unlock();
            }
        }

        private unsafe void WriteAndRead(ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer)
        {
            fixed (byte* pWriteBuffer = writeBuffer)
            fixed (byte* pReadBuffer = readBuffer)
            {
                Unsafe.SkipInit(out Libc.i2c_msg2 msg);
                uint msgLen = 0;

                Libc.i2c_msg* firstMsg = &msg.read;

                if (readBuffer.Length != 0)
                {
                    msg.read.addr = _addr;
                    msg.read.flags = Libc.I2C_M_RD | Libc.I2C_M_RECV_LEN;
                    msg.read.len = (ushort)readBuffer.Length;
                    msg.read.buf = pReadBuffer;

                    firstMsg = &msg.read;
                    ++msgLen;
                }

                if (writeBuffer.Length != 0)
                {
                    msg.write.addr = _addr;
                    msg.write.flags = 0;
                    msg.write.len = (ushort)writeBuffer.Length;
                    msg.write.buf = pWriteBuffer;

                    firstMsg = &msg.write;
                    ++msgLen;
                }

                var ioctl = new Libc.i2c_rdwr_ioctl_data
                {
                    msgs = firstMsg,
                    nmsgs = msgLen
                };

                int err = Libc.ioctl(_fd.FileDescriptor, Libc.I2C_RDWR, &ioctl);
                CheckError(nameof(Libc.ioctl), err);
            }
        }

        private static void CheckError(string func, nint err)
        {
            if (err < 0)
            {
                ThrowError(func);
            }

            static void ThrowError(string @func)
            {
                // TODO: throw something more descriptive.
                int err = Marshal.GetLastWin32Error();
                throw new Exception($"Function {@func} failed; errno {err}.");
            }
        }
    }
}
