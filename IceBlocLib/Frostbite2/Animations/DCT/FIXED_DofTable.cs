using IceBlocLib.Utility;
using System.Runtime.InteropServices;

namespace IceBlocLib.Frostbite2.Animations.DCT;

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 24)]
public unsafe struct FIXED_DofTable
{
    [FieldOffset(0)]
    public Vector4i mDeltaBase;
    [FieldOffset(8), MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public BitsPerComponent[] mBitsPerSubblock;

    public static uint GetSerializedSize(byte NumSubblocks)
    {
        return (uint)(8 + 2 * NumSubblocks);
    }

    public static FIXED_DofTable GetNextEntry(FIXED_DofTable dofTable, byte mNumSubblocks)
    {
        throw new NotImplementedException();
    }

    public uint ComputeSum_BitsPerSubblock(byte StartSubblock, byte EndSubblock, byte aCatchAllBitCount)
    {
        uint TotalBits = 0;

        for (uint subblock = StartSubblock; subblock <= EndSubblock; subblock++)
        {
            TotalBits += (uint)(QuantizedBlock.GetBitCount_WithCatchAll((byte)mBitsPerSubblock[subblock].mBitsX, aCatchAllBitCount)
                + QuantizedBlock.GetBitCount_WithCatchAll((byte)mBitsPerSubblock[subblock].mBitsY, aCatchAllBitCount)
                + QuantizedBlock.GetBitCount_WithCatchAll((byte)mBitsPerSubblock[subblock].mBitsZ, aCatchAllBitCount)
                + QuantizedBlock.GetBitCount_WithCatchAll((byte)mBitsPerSubblock[subblock].mBitsW, aCatchAllBitCount));
        }

        return TotalBits;
    }
}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 2)]
public unsafe struct BitsPerComponent
{
    [FieldOffset(0)]
    public ushort Data;

    public ushort mBitsW { get => (ushort)(Data & 0x000F); set => Data = value; }
    public ushort mBitsZ { get => (ushort)((Data & 0x00F0) >> 4); set => Data = value; }
    public ushort mBitsY { get => (ushort)((Data & 0x0F00) >> 8); set => Data = value; }
    public ushort mBitsX { get => (ushort)((Data & 0xF000) >> 12); set => Data = value; }
}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 1)]
public unsafe struct FIXED_DofTableDescriptor
{
    [FieldOffset(0)]
    public byte Data = 0;

    public FIXED_DofTableDescriptor() { }

    public byte mNumSubblocks { get => (byte)(Data >> 4); set => Data = value; }

    public static uint GetSerializedDofTableSize(in FIXED_DofTableDescriptor[] descriptorTable)
    {
        uint bytes = 0;

        for (uint i = 0; i < descriptorTable.Length; i++)
        {
            bytes += FIXED_DofTable.GetSerializedSize(descriptorTable[i].mNumSubblocks);
        }

        return bytes;
    }

    public static int GetSerializedEntrySize()
    {
        return 1;
    }
}