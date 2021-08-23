using Aether.Devices.Interop;
using System.Diagnostics;
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
        private readonly SemaphoreSlim _busLock;
        private readonly FileDescriptorSafeHandle _fd;
        private readonly ushort _addr;
        private readonly ulong _funcs;
        private readonly byte[] _nullTerminatedFilePath;

        public unsafe LinuxI2CDevice(byte[] nullTerminatedFilePath, int deviceAddress, SemaphoreSlim busLock)
        {
            Debug.Assert(nullTerminatedFilePath[^1] == 0, $"{nameof(nullTerminatedFilePath)} must be null-terminated.");

            int fd;

            fixed (byte* utf8FilePathPointer = nullTerminatedFilePath)
            {
                fd = Libc.open(utf8FilePathPointer, Libc.O_RDWR);
            }

            CheckError(nameof(Libc.open), fd);

            _nullTerminatedFilePath = nullTerminatedFilePath;
            _busLock = busLock;
            _fd = new FileDescriptorSafeHandle(fd);
            _addr = (ushort)deviceAddress;

            SetDeviceAddress(deviceAddress);
            _funcs = GetSupportedFuncs();
        }

        public override void Dispose() =>
            _fd.Dispose();

        public override string ToString() =>
            $"{{ \"{Encoding.UTF8.GetString(_nullTerminatedFilePath.AsSpan(0, _nullTerminatedFilePath.Length - 1))}\" , 0x{_addr:X} }}";

        private unsafe void SetDeviceAddress(long deviceAddress)
        {
            int err = Libc.ioctl(_fd.FileDescriptor, Libc.I2C_SLAVE, &deviceAddress);
            CheckError(nameof(Libc.ioctl), err);
        }

        private unsafe ulong GetSupportedFuncs()
        {
            Unsafe.SkipInit(out ulong funcs);

            int err = Libc.ioctl(_fd.FileDescriptor, Libc.I2C_FUNCS, &funcs);
            CheckError(nameof(Libc.ioctl), err);

            return funcs;
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> writeBuffer, CancellationToken cancellationToken = default)
        {
            if (writeBuffer.Length == 0) throw new ArgumentException($"{nameof(writeBuffer)} must have a non-zero length.", nameof(writeBuffer));

            await _busLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Write(writeBuffer.Span);
            }
            finally
            {
                _busLock.Release();
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

        public override async ValueTask ReadAsync(Memory<byte> readBuffer, CancellationToken cancellationToken = default)
        {
            if (readBuffer.Length == 0) throw new ArgumentException($"{nameof(readBuffer)} must have a non-zero length.", nameof(readBuffer));

            await _busLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Read(readBuffer.Span);
            }
            finally
            {
                _busLock.Release();
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

        public override async ValueTask WriteAndReadAsync(ReadOnlyMemory<byte> writeBuffer, Memory<byte> readBuffer, CancellationToken cancellationToken = default)
        {
            if (writeBuffer.Length > ushort.MaxValue) throw new ArgumentException($"{nameof(writeBuffer)} must be at most {ushort.MaxValue} bytes in length.", nameof(writeBuffer));
            if (readBuffer.Length > ushort.MaxValue) throw new ArgumentException($"{nameof(readBuffer)} must be at most {ushort.MaxValue} bytes in length.", nameof(readBuffer));
            if (writeBuffer.Length == 0 && readBuffer.Length == 0) throw new ArgumentException($"One of {nameof(writeBuffer)} or {nameof(readBuffer)} must have a non-zero length.");

            await _busLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if ((_funcs & Libc.I2C_FUNC_I2C) != 0)
                {
                    WriteAndRead(writeBuffer.Span, readBuffer.Span);
                }
                else
                {
                    Write(writeBuffer.Span);
                    Read(readBuffer.Span);
                }
            }
            finally
            {
                _busLock.Release();
            }
        }

        private unsafe void WriteAndRead(ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer)
        {
            int err;

            fixed (byte* pWriteBuffer = writeBuffer)
            fixed (byte* pReadBuffer = readBuffer)
            {
                Unsafe.SkipInit(out i2c_msg2 msg);
                uint msgLen = 0;

                Libc.i2c_msg* firstMsg = &msg.read;

                if (readBuffer.Length != 0)
                {
                    msg.read.addr = _addr;
                    msg.read.flags = Libc.I2C_M_RD;
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

                err = Libc.ioctl(_fd.FileDescriptor, Libc.I2C_RDWR, &ioctl);
            }

            CheckError(nameof(Libc.ioctl), err);
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

        private struct i2c_msg2
        {
            public Libc.i2c_msg write;
            public Libc.i2c_msg read;
        }
    }
}
