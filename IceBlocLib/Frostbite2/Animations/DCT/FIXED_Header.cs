using System.Drawing;
using System.Runtime.InteropServices;

namespace IceBlocLib.Frostbite2.Animations.DCT;

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 13)]
public unsafe struct FIXED_Header
{
    [FieldOffset(0)]
    public ushort mNumFrames;
    [FieldOffset(2)]
    public ushort mNumQuats;
    [FieldOffset(4)]
    public ushort mNumVec3s;
    [FieldOffset(6)]
    public ushort mNumFloatVecs;
    [FieldOffset(8)]
    public ushort mQuantizeMult_Block;
    [FieldOffset(10)]
    public byte mQuantizeMult_Subblock;
    [FieldOffset(11)]
    public byte mCatchAllBitCount;
    [FieldOffset(12)]
    public FIXED_DofTableDescriptor mDofDescriptor;

    public static int GetSerializedSize()
    {
        return 12;
    }

    public int GetNumTableEntriesPerFrame()
    {
        return mNumQuats + mNumVec3s + mNumFloatVecs;
    }
}