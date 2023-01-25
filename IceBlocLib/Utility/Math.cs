using System.Runtime.InteropServices;

namespace IceBlocLib.Utility;

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 8)]
public struct Vector4i
{
    [FieldOffset(0)]
    public short X;
    [FieldOffset(2)]
    public short Y;
    [FieldOffset(4)]
    public short Z;
    [FieldOffset(6)]
    public short W;

    public Vector4i(short x, short y, short z, short w)
    {
        X = x; Y = y; Z = z; W = w;
    }

    public void Set(short x, short y,short z,short w)
    {
        X = x; Y = y; Z = z; W = w;
    }

    public short this[int index]
    {
        get
        {
            switch (index)
            {
                case 0: return X;
                case 1: return Y;
                case 2: return Z;
                case 3: return W;
            }
            throw new IndexOutOfRangeException();
        }
        set
        {
            switch (index)
            {
                case 0: X = value; break;
                case 1: Y = value; break;
                case 2: Z = value; break;
                case 3: W = value; break;
            }
        }
    }
}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 16)]
public unsafe struct M128
{
    [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] m128_f32;
    [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public ulong[] m128_u64;
    [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public sbyte[] m128_i8;
    [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public short[] m128_i16;
    [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public int[] m128_i32;
    [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public long[] m128_i64;
    [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] m128_u8;
    [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public ushort[] m128_u16;
    [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public uint[] m128_u32;

    public M128()
    {
    }
}
