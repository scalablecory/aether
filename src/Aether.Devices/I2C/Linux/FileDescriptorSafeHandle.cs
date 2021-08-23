using Aether.Devices.Interop;
using System.Runtime.InteropServices;

namespace Aether.Devices.I2C.Linux
{
    /// <summary>
    /// A safe handle for a Linux file descriptor.
    /// </summary>
    internal sealed unsafe class FileDescriptorSafeHandle : SafeHandle
    {
        private static IntPtr InvalidHandleValue => (nint)(-1);

        public override bool IsInvalid => handle == InvalidHandleValue;

        public int FileDescriptor
        {
            get
            {
                nint h = handle;
                if (h == InvalidHandleValue) ThrowODE(this);
                return (int)h;

                static void ThrowODE(FileDescriptorSafeHandle @this) => new ObjectDisposedException(@this.GetType().Name);
            }
        }

        public FileDescriptorSafeHandle(int fd)
            : base(InvalidHandleValue, ownsHandle: true)
        {
            handle = (nint)fd;
        }

        protected override bool ReleaseHandle()
        {
            return Libc.close(FileDescriptor) != 0;
        }
    }
}
