namespace IceBlocLib.Frostbite2013.Animations.DCT;

public class DofTable
{
    public class BitsPerComponent
    {
        public BitsPerComponent(ushort p_Value)
        {
            Value = p_Value;
        }

        public ushort Value { get; set; }


        public ushort BitsW => (ushort)(Value >> 4 * 0 & 0xF);
        public ushort BitsZ => (ushort)(Value >> 4 * 1 & 0xF);
        public ushort BitsY => (ushort)(Value >> 4 * 2 & 0xF);
        public ushort BitsX => (ushort)(Value >> 4 * 3 & 0xF);

        public ushort SafeBitsW(ushort p_CatchAllBitCount) => BitsW == 0xF ? p_CatchAllBitCount : BitsW;
        public ushort SafeBitsZ(ushort p_CatchAllBitCount) => BitsZ == 0xF ? p_CatchAllBitCount : BitsZ;
        public ushort SafeBitsY(ushort p_CatchAllBitCount) => BitsY == 0xF ? p_CatchAllBitCount : BitsY;
        public ushort SafeBitsX(ushort p_CatchAllBitCount) => BitsX == 0xF ? p_CatchAllBitCount : BitsX;


        public int BitSum => BitsX + BitsY + BitsZ + BitsW;

        public int SafeSum(ushort p_CatchAllBitCount) => SafeBitsX(p_CatchAllBitCount) + SafeBitsY(p_CatchAllBitCount) + SafeBitsZ(p_CatchAllBitCount) + SafeBitsW(p_CatchAllBitCount);
    }



    public ushort SubBlockCount { get; set; } = 0;

    public short[] DeltaBase = new short[4];

    public BitsPerComponent[] BitsPerSubBlock = new BitsPerComponent[0];


    public DofTable(ushort p_SubBlockCount)
    {
        SubBlockCount = p_SubBlockCount;
    }

    public DofTable(RimeReader p_Reader, ushort p_SubBlockCount)
        : this(p_SubBlockCount)
    {
        Deserialize(p_Reader);
    }

    public void Deserialize(RimeReader p_Reader)
    {
        DeltaBase = new short[4]
        {
            p_Reader.ReadInt16(),
            p_Reader.ReadInt16(),
            p_Reader.ReadInt16(),
            p_Reader.ReadInt16(),
        };

        BitsPerSubBlock = new BitsPerComponent[SubBlockCount];

        for (var i = 0; i < SubBlockCount; i++)
            BitsPerSubBlock[i] = new BitsPerComponent(p_Reader.ReadUInt16());
    }


    public void Deserialize(byte[] p_Data)
    {
        using var s_Reader = new RimeReader(new MemoryStream(p_Data));
        Deserialize(s_Reader);
    }
}
