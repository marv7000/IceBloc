namespace IceBlocLib.Frostbite2.Animations.DCT;

public class DofTable
{
    public class BitsPerComponent
    {
        public BitsPerComponent(ushort p_Value)
        {
            Value = p_Value;
        }

        public ushort Value { get; set; }


        public ushort BitsW => (ushort)((Value >> (4 * 0)) & 0xF);
        public ushort BitsZ => (ushort)((Value >> (4 * 1)) & 0xF);
        public ushort BitsY => (ushort)((Value >> (4 * 2)) & 0xF);
        public ushort BitsX => (ushort)((Value >> (4 * 3)) & 0xF);

        public ushort SafeBitsW(ushort p_CatchAllBitCount) => (BitsW != 0xF) ? BitsW : p_CatchAllBitCount;
        public ushort SafeBitsZ(ushort p_CatchAllBitCount) => (BitsZ != 0xF) ? BitsZ : p_CatchAllBitCount;
        public ushort SafeBitsY(ushort p_CatchAllBitCount) => (BitsY != 0xF) ? BitsX : p_CatchAllBitCount;
        public ushort SafeBitsX(ushort p_CatchAllBitCount) => (BitsX != 0xF) ?  BitsY : p_CatchAllBitCount;


        public int BitSum => BitsX + BitsY + BitsZ + BitsW;

        public int SafeSum(ushort p_CatchAllBitCount) => SafeBitsX(p_CatchAllBitCount) + SafeBitsY(p_CatchAllBitCount) + SafeBitsZ(p_CatchAllBitCount) + SafeBitsW(p_CatchAllBitCount);
    }

    public ushort SubBlockCount { get; set; } = 0;

    public short[] DeltaBase = new short[4];

    public BitsPerComponent[] BitsPerSubBlock = new BitsPerComponent[0];


    public DofTable(ushort subBlockCount)
    {
        SubBlockCount = subBlockCount;
    }
}
