using System.Buffers.Binary;

namespace IceBlocLib.Frostbite2.Animations.Misc;

public class BitReader
{
    public Stream BaseStream { get; set; }

    public bool BigEndian { get; set; }

    private uint BitOffset { get; set; }
    public long Position { 
        get => BaseStream.Position * 8 + BitOffset;
        set
        {
            BaseStream.Position = value / 8;
            BitOffset = (uint)(value % 8);
        }
    }

    public BitReader(Stream baseStream, bool bigEndian)
    { 
        BaseStream = baseStream; 
        BigEndian = bigEndian;
    }

    public int ReadBits(uint bits)
    {
        if (BigEndian)
        {
            return GetSwappedBitData(Position, bits);
        }
        else
        {
            return GetBitData(Position, bits);
        }
    }

    private int GetBitData(long bitOffset, uint numBits)
    {
        ulong dat;
        ulong output;

        long startByteOffset = bitOffset / 8;
        long startBitOffset = bitOffset % 8;

        byte[] buf = new byte[8];
        BaseStream.Position = startByteOffset;
        BaseStream.Read(buf, 0, 8);
        BaseStream.Position -= 8;
        dat = BinaryPrimitives.ReadUInt64LittleEndian(buf);
        output = dat << (int)(64 - startBitOffset - numBits);

        output >>= (int)(64 - numBits);

        Position += numBits;
        return (int) output;
    }

    private int GetSwappedBitData(long bitOffset, uint numBits)
    {
        ulong dat;
        ulong output;

        long byteOffset = bitOffset / 8;
        long startBitOffset = bitOffset % 8;

        byte[] buf = new byte[8];
        BaseStream.Position = byteOffset;
        BaseStream.Read(buf, 0, 8);
        BaseStream.Position -= 8;
        dat = BinaryPrimitives.ReadUInt64BigEndian(buf);
        output = dat << (int)(64 - startBitOffset - numBits);

        output >>= (int)(64 - numBits);

        Position += numBits;
        return (int)output;
    }
}

