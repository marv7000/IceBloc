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

    public List<short> AsShortList()
    {
        var list = new List<short>();

        list.Add(X);
        list.Add(Y);
        list.Add(Z);
        list.Add(W);

        return list;
    }
}
