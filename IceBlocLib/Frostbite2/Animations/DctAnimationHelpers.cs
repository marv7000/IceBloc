using IceBlocLib.Frostbite;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace IceBlocLib.Frostbite2.Animations;

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
        uint mMatricesSize; // edi
        __m128* mFinalMatrices; // eax
        ScratchPad mScratchPad; // ecx

        mDctAnim = dctAnim;
        mDofMap = dofMap;

        Memory<byte> dctAnimData = dctAnim.Data;
        var dctAnimDataPtr = dctAnimData.Pin();

        mCodec = new FIXED_Decompressor((byte*)dctAnimDataPtr.Pointer, TargetEndian.TARGET_ENDIAN_BIG);
        this.mScratchPad = scratchPad;
        this.mPrevFrameInBuffer = 0xFFFFFFFF;
        this.mNextFrameInBuffer = 0xFFFFFFFF;

        ushort mNQ = mCodec.mHeader->mNumQuats;
        ushort mNV = mCodec.mHeader->mNumVec3s;
        ushort mNF = mCodec.mHeader->mNumFloatVecs;

        mMatricesSize = (uint)(16 * (mNQ + mNV + mNF));
        mFinalMatrices = (__m128*)this.mScratchPad.Alloc(mMatricesSize);
        mScratchPad = this.mScratchPad;
        mPrevMemBuffer = mFinalMatrices;
        mNextMemBuffer = (__m128*)mScratchPad.Alloc(mMatricesSize);
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
        FIXED_DofTable* v3;

        mTargetEndian = targetEndian;
        mDofTable = (FIXED_DofTable*)0U;
        mCompressedData = (byte*)0U;
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

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 0x1)]
public unsafe struct FIXED_DofTableDescriptor
{
    [FieldOffset(0)]
    private byte Data = 0;

    public FIXED_DofTableDescriptor()
    {
    }

    public byte mNumSubblocks { get => (byte)(Data >> 4); set => Data = value; }

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

[StructLayout(LayoutKind.Explicit, Pack = 2, Size = 0x0E)]
public unsafe struct FIXED_Header
{
    [FieldOffset(0)]
    public ushort mNumFrames = 0;
    [FieldOffset(2)]
    public ushort mNumQuats = 0;
    [FieldOffset(4)]
    public ushort mNumVec3s = 0;
    [FieldOffset(6)]
    public ushort mNumFloatVecs = 0;
    [FieldOffset(8)]
    public ushort mQuantizeMult_Block = 0;
    [FieldOffset(10)]
    public byte mQuantizeMult_Subblock = 0;
    [FieldOffset(11)]
    public byte mCatchAllBitCount = 0;
    [FieldOffset(12)]
    public FIXED_DofTableDescriptor mDofDescriptor; // const [1]

    public FIXED_Header()
    {
    }
}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 0x18)]
public unsafe struct FIXED_DofTable
{
    [FieldOffset(0)]
    public (short X, short Y, short Z, short W) mDeltaBase;
    [FieldOffset(8), MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public BitsPerComponent[] mBitsPerSubblock = new BitsPerComponent[8];

    public FIXED_DofTable()
    {
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

public unsafe struct ChannelDofMap
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

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 16)]
public unsafe struct __m128
{
    [FieldOffset(0)]
    fixed float m128_f32[4];
    [FieldOffset(0)]
    fixed ulong m128_u64[2];
    [FieldOffset(0)]
    fixed sbyte m128_i8[16];
    [FieldOffset(0)]
    fixed short m128_i16[8];
    [FieldOffset(0)]
    fixed int m128_i32[4];
    [FieldOffset(0)]
    fixed long m128_i64[2];
    [FieldOffset(0)]
    fixed byte m128_u8[16];
    [FieldOffset(0)]
    fixed ushort m128_u16[8];
    [FieldOffset(0)]
    fixed uint m128_u32[4];

    public __m128()
    {
    }
}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 20)]
public unsafe struct ScratchPad
{
    [FieldOffset(0)]
    BlendMask* mBlendMask;
    [FieldOffset(4)]
    uint mCurrentPos;
    [FieldOffset(8)]
    uint mLastLock;
    [FieldOffset(12)]
    byte* mMemory;
    [FieldOffset(16)]
    uint mCurrentMax;

    public ScratchPad()
    {
        //QueueManagerPageAllocator* v2; // ecx
        //TlsEntry* TlsEntry; // eax
        //
        //v2 = gQueueManagerPageAllocator;
        //
        //mBlendMask = 0;
        //TlsEntry = QueueManagerPageAllocator.GetTlsEntry(v2);
        //mMemory = TlsEntry.GetScratchPad();
        mCurrentPos = 0;
        mLastLock = 0;
        mCurrentMax = 0;
    }

    public byte* Alloc(uint aSize)
    {
        uint mCurrentPos; // ecx
        uint mCurrentMax; // edx
        byte* result; // eax
        uint offsetPos; // ecx

        mCurrentPos = this.mCurrentPos;
        mCurrentMax = this.mCurrentMax; 
        result = &mMemory[mCurrentPos];
        offsetPos = ((aSize + 15) & 0xFFFFFFF0) + mCurrentPos;
        this.mCurrentPos = offsetPos;
        if (offsetPos <= mCurrentMax)
            this.mCurrentMax = mCurrentMax;
        else
            this.mCurrentMax = offsetPos;
        return result;
    }
}

public unsafe struct BlendMask
{

}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 0xD0)]
public unsafe struct QueueManagerPageAllocator
{
    [FieldOffset(0x0)]
    public ThreadLocalStorage mStorage;
    [FieldOffset(0x4)]
    public ThreadLocalStorage mID;
    [FieldOffset(0x8)]
    public fbStack mTlsEntries;
    [FieldOffset(0x10)]
    public FixedAllocator mScratchPadAllocator;
    [FieldOffset(0x50)]
    public FixedAllocator mTlsEntryAllocator;
    [FieldOffset(0x90)]
    public FixedAllocator mPageAllocator;
}

public unsafe struct ThreadLocalStorage
{
}

public unsafe struct FixedAllocator
{
    //public ICoreAllocator* mAllocator;
    public uint mObjectSize;
    public uint mCountPerCoreBlock;
    public uint mObjectAlignment;
    public uint mHeaderSize;
    public uint mBlockSize;
    public bool mAssertOnExhaust;
    public char* mName;
    //public CoreBlock* mHeadCoreBlock;
    public fbStack mHeadChunk;
    public int mWaitLock;
    public int mNumAllocated;
    public int mMaxAllocated;
}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 8)]
public unsafe struct fbStack
{
    [FieldOffset(0)]
    public long mHeader;
}