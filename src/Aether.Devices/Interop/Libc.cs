using System.Runtime.InteropServices;

namespace Aether.Devices.Interop
{
    internal static unsafe class Libc
    {
        private const string libc = "libc";

        public const uint I2C_SLAVE = 0x703;
        public const uint I2C_FUNCS = 0x705;
        public const uint I2C_RDWR = 0x707;
        public const uint I2C_SMBUS = 0x720;

        public const ushort I2C_M_RD = 0x0001;  /* guaranteed to be 0x0001! */
        public const ushort I2C_M_TEN = 0x0010; /* use only if I2C_FUNC_10BIT_ADDR */
        public const ushort I2C_M_DMA_SAFE = 0x0200;    /* use only in kernel space */
        public const ushort I2C_M_RECV_LEN = 0x0400;    /* use only if I2C_FUNC_SMBUS_READ_BLOCK_DATA */
        public const ushort I2C_M_NO_RD_ACK = 0x0800;   /* use only if I2C_FUNC_PROTOCOL_MANGLING */
        public const ushort I2C_M_IGNORE_NAK = 0x1000;  /* use only if I2C_FUNC_PROTOCOL_MANGLING */
        public const ushort I2C_M_REV_DIR_ADDR = 0x2000;    /* use only if I2C_FUNC_PROTOCOL_MANGLING */
        public const ushort I2C_M_NOSTART = 0x4000; /* use only if I2C_FUNC_NOSTART */
        public const ushort I2C_M_STOP = 0x8000;	/* use only if I2C_FUNC_PROTOCOL_MANGLING */

        public const byte I2C_SMBUS_READ = 1;
        public const byte I2C_SMBUS_WRITE = 0;

        public const byte I2C_SMBUS_QUICK = 0;
        public const byte I2C_SMBUS_BYTE = 1;
        public const byte I2C_SMBUS_BYTE_DATA = 2;
        public const byte I2C_SMBUS_WORD_DATA = 3;
        public const byte I2C_SMBUS_PROC_CALL = 4;
        public const byte I2C_SMBUS_BLOCK_DATA = 5;
        public const byte I2C_SMBUS_I2C_BLOCK_BROKEN = 6;
        public const byte I2C_SMBUS_BLOCK_PROC_CALL = 7;        /* SMBus 2.0 */
        public const byte I2C_SMBUS_I2C_BLOCK_DATA = 8;

        public const ulong I2C_FUNC_I2C = 0x00000001;
        public const ulong I2C_FUNC_10BIT_ADDR = 0x00000002 /* required for I2C_M_TEN */;
        public const ulong I2C_FUNC_PROTOCOL_MANGLING = 0x00000004 /* required for I2C_M_IGNORE_NAK etc. */;
        public const ulong I2C_FUNC_SMBUS_PEC = 0x00000008;
        public const ulong I2C_FUNC_NOSTART = 0x00000010 /* required for I2C_M_NOSTART */;
        public const ulong I2C_FUNC_SLAVE = 0x00000020;
        public const ulong I2C_FUNC_SMBUS_BLOCK_PROC_CALL = 0x00008000 /* SMBus 2.0 or later */;
        public const ulong I2C_FUNC_SMBUS_QUICK = 0x00010000;
        public const ulong I2C_FUNC_SMBUS_READ_BYTE = 0x00020000;
        public const ulong I2C_FUNC_SMBUS_WRITE_BYTE = 0x00040000;
        public const ulong I2C_FUNC_SMBUS_READ_BYTE_DATA = 0x00080000;
        public const ulong I2C_FUNC_SMBUS_WRITE_BYTE_DATA = 0x00100000;
        public const ulong I2C_FUNC_SMBUS_READ_WORD_DATA = 0x00200000;
        public const ulong I2C_FUNC_SMBUS_WRITE_WORD_DATA = 0x00400000;
        public const ulong I2C_FUNC_SMBUS_PROC_CALL = 0x00800000;
        public const ulong I2C_FUNC_SMBUS_READ_BLOCK_DATA = 0x01000000; /* required for I2C_M_RECV_LEN */
        public const ulong I2C_FUNC_SMBUS_WRITE_BLOCK_DATA = 0x02000000;
        public const ulong I2C_FUNC_SMBUS_READ_I2C_BLOCK = 0x04000000; /* I2C-like block xfer  */
        public const ulong I2C_FUNC_SMBUS_WRITE_I2C_BLOCK = 0x08000000; /* w/ 1-byte reg. addr. */
        public const ulong I2C_FUNC_SMBUS_HOST_NOTIFY = 0x10000000; /* SMBus 2.0 or later */

        public const int I2C_SMBUS_BLOCK_MAX = 32;

        public const int O_RDWR = 2;

        [DllImport(libc, SetLastError = true)]
        public static extern int open(byte* pathname, int flags);

        [DllImport(libc, SetLastError = true)]
        public static extern int close(int fd);

        [DllImport(libc, SetLastError = true)]
        public static extern nint read(int fd, void* buf, nuint count);

        [DllImport(libc, SetLastError = true)]
        public static extern nint write(int fd, void* buf, nuint count);

        [DllImport(libc, SetLastError = true)]
        public static extern int ioctl(int fd, uint request, void* argp);

        public unsafe struct i2c_msg
        {
            public ushort addr;
            public ushort flags;
            public ushort len;
            public void* buf;
        }

        public unsafe struct i2c_rdwr_ioctl_data
        {
            public i2c_msg* msgs;
            public uint nmsgs;
        }
    }
}
