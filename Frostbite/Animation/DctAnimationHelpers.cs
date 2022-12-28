using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace IceBloc.Frostbite.Animation;

public unsafe class DctAnimDecompressor
{
    DctAnimation mDctAnim;
    ChannelDofMap mDofMap;
    FIXED_Decompressor mCodec;
    ScratchPad mScratchPad;
    __m128* mPrevMemBuffer;
    __m128* mNextMemBuffer;
    uint mPrevFrameInBuffer;
    uint mNextFrameInBuffer;

    public unsafe DctAnimDecompressor(DctAnimation dctAnim, ChannelDofMap dofMap, ScratchPad scratchPad)
    {
        uint v5; // edi
        __m128* v6; // eax
        ScratchPad mScratchPad; // ecx

        mDctAnim = dctAnim;
        mDofMap = dofMap;

        Memory<byte> dctAnimData = dctAnim.mData;
        var dctAnimDataPtr = dctAnimData.Pin();

        mCodec = new FIXED_Decompressor((byte*)dctAnimDataPtr.Pointer, TargetEndian.TARGET_ENDIAN_BIG);
        this.mScratchPad = scratchPad;
        this.mPrevFrameInBuffer = 0xFFFFFFFF;
        this.mNextFrameInBuffer = 0xFFFFFFFF;

        ushort mNQ = this.mCodec.mHeader->mNumQuats;
        ushort mNV = this.mCodec.mHeader->mNumVec3s;
        ushort mNF = this.mCodec.mHeader->mNumFloatVecs;

        v5 = (uint)(16 * (mNQ + mNV + mNF));
        v6 = (__m128*)this.mScratchPad.Alloc(v5);
        mScratchPad = this.mScratchPad;
        this.mPrevMemBuffer = v6;
        this.mNextMemBuffer = (__m128*)mScratchPad.Alloc(v5);
    }
}

public unsafe class FIXED_Decompressor
{
    public byte* mCompressedSource_All;
    public TargetEndian mTargetEndian;
    public FIXED_Header* mHeader;
    public FIXED_DofTableDescriptor* mDofTableDescriptor;
    public FIXED_DofTable* mDofTable;
    public byte* mCompressedData;

    public FIXED_Decompressor(byte* CompressedSource_All, TargetEndian targetEndian)
    {
        FIXED_DofTable* v3; // edi

        mTargetEndian = targetEndian;
        mDofTable = (FIXED_DofTable*)0;
        mCompressedData = (byte*)0;
        mCompressedSource_All = CompressedSource_All;
        mHeader = (FIXED_Header*)CompressedSource_All;
        mDofTableDescriptor = (FIXED_DofTableDescriptor*)(CompressedSource_All + 12);
        v3 = (FIXED_DofTable*)((uint)&CompressedSource_All[*((ushort*)CompressedSource_All + 3) +13 + *((ushort *)CompressedSource_All + 2) + *((ushort *)CompressedSource_All + 1)] &0xFFFFFFFE);
        mDofTable = v3;
        mCompressedData = (byte*)v3 + FIXED_DofTableDescriptor.GetSerializedDofTableSize(
            (FIXED_DofTableDescriptor*)CompressedSource_All + 12,
            (uint)(*((ushort *)CompressedSource_All + 1) +*((ushort*)CompressedSource_All + 3) +*((ushort*)CompressedSource_All + 2)));
    }
}

public unsafe class FIXED_DofTableDescriptor
{
    public static unsafe int GetSerializedDofTableSize(FIXED_DofTableDescriptor* DescriptorTable, uint NumDescriptorEntries)
    {
        uint v2; // edi
        int v3; // ebx
        int v4; // ecx
        int v5; // edx
        uint v6; // eax
        int v7; // ebx
    
        v2 = NumDescriptorEntries;
        v3 = 0;
        v4 = 0;
        v5 = 0;
        v6 = 0;
        if ((int) NumDescriptorEntries >= 2 )
        {
            while (v6 < NumDescriptorEntries - 1)
            {
                v4 += 2 * (*(byte*)&DescriptorTable[v6] >> 4) + 8;
                v7 = *(byte*)&DescriptorTable[v6 + 1] >> 4;
                v6 += 2;
                v5 += 2 * v7 + 8;
            }
            v2 = NumDescriptorEntries;
            v3 = 0;
        }
        if (v6 < v2)
            v3 = 2 * (*(byte*)&DescriptorTable[v6] >> 4) + 8;
        return v3 + v5 + v4;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public unsafe class FIXED_Header
{
    public ushort mNumFrames;
    public ushort mNumQuats;
    public ushort mNumVec3s;
    public ushort mNumFloatVecs;
    public ushort mQuantizeMult_Block;
    public byte mQuantizeMult_Subblock;
    public byte mCatchAllBitCount;
    public FIXED_DofTableDescriptor* mDofDescriptor; // const [1]
}

public unsafe class FIXED_DofTable
{
    public (short X, short Y, short Z, short W) mDeltaBase;
    public BitsPerComponent[] mBitsPerSubblock = new BitsPerComponent[8];

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    public struct BitsPerComponent
    {
        ushort mBitsW;
        ushort mBitsZ;
        ushort mBitsY;
        ushort mBitsX;
    }
}

public class ChannelDofMap
{
    public ChannelDofMapCache mCache;
    public uint mSize;
    public uint mTrajOffset;
    public uint mDeltaTrajOffset;
    public bool mTrajExist;
    public int mTrajQChanIdx;
    public int mTrajTChanIdx;
    public uint mTrajQDefaultOffset;
    public uint mTrajTDefaultOffset;
    public bool mTrajQMask;
    public bool mTrajTMask;
    public int mTrajPoseDofIdx;
    public int mDeltaTrajPoseDofIdx;
    public uint mNumDofs;
    public uint mNumDefaultDofs;
    public uint mDefaultBufferSize;
    public uint mDefaultMappingOffset;
    public uint mDefaultBufferOffset;
    public uint mActivityMaskOffset;
    public uint mActivityMaskSize;
}

public class ChannelDofMapCache
{
    public List<Entry> mCache;
    public ChannelDofMapCache mNext;
    public ChannelDofMapCache mPrev;

    public class Entry
    {
        public RigBinding mRigBinding;
        public LayoutHierarchyAsset mDofSetList;
        public ChannelDofMap mDofMap;
        public bool mAdditive;
    }
}

public class LayoutHierarchyAsset
{
}

public class RigBinding
{
    public LayoutMask mPermissionMask;
    public Rig mRig;
    public LayoutHierarchyAsset mLayoutHierarchyAsset;
    public LayoutHierarchyAsset mOverrideLayoutHierarchyAsset;
}

public class Rig
{
}

public class LayoutMask
{
}

[StructLayout(LayoutKind.Explicit, Size = 16)]
public unsafe struct __m128
{
    [FieldOffset(0)]
    float[] m128_f32 = new float[4];
    [FieldOffset(0)]
    ulong[] m128_u64 = new ulong[2];
    [FieldOffset(0)]
    sbyte[] m128_i8 = new sbyte[16];
    [FieldOffset(0)]
    short[] m128_i16 = new short[8];
    [FieldOffset(0)]
    int[] m128_i32 = new int[4];
    [FieldOffset(0)]
    long[] m128_i64 = new long[2];
    [FieldOffset(0)]
    byte[] m128_u8 = new byte[16];
    [FieldOffset(0)]
    ushort[] m128_u16 = new ushort[8];
    [FieldOffset(0)]
    uint[] m128_u32 = new uint[4];

    public __m128()
    {
    }
}

public unsafe struct ScratchPad
{
    //BlendMask mBlendMask;
    uint mCurrentPos;
    uint mLastLock;
    byte* mMemory;
    uint mCurrentMax;

    public ScratchPad()
    {
    }

    public byte* Alloc(uint aSize)
    {
        uint mCurrentPos; // ecx
        uint mCurrentMax; // edx
        byte* result; // eax
        uint offsetPos; // ecx

        mCurrentPos = this.mCurrentPos;
        mCurrentMax = this.mCurrentMax;
        result = (byte*)mMemory[mCurrentPos];
        offsetPos = ((aSize + 15) & 0xFFFFFFF0) + mCurrentPos;
        this.mCurrentPos = offsetPos;
        if (offsetPos <= mCurrentMax)
            this.mCurrentMax = mCurrentMax;
        else
            this.mCurrentMax = offsetPos;
        return result;
    }
}

