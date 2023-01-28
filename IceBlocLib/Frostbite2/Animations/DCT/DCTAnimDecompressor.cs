using IceBlocLib.Frostbite;
using IceBlocLib.Frostbite2.Animations.Base;
using IceBlocLib.Utility;

namespace IceBlocLib.Frostbite2.Animations.DCT;

public class DCTAnimDecompressor
{
    public DctAnimation mDctAnim;
    public FIXED_Decompressor mCodec;
    public M128[] mPrevMemBuffer;
    public M128[] mNextMemBuffer;
    public uint mPrevFrameInBuffer;
    public uint mNextFrameInBuffer;

    public DCTAnimDecompressor(DctAnimation dctAnim)
    {
        mDctAnim = dctAnim;
        mCodec = new FIXED_Decompressor(dctAnim.SourceCompressedAll, TargetEndian.TARGET_ENDIAN_LITTLE);
        mPrevFrameInBuffer = 0xFFFFFFFF;
        mNextFrameInBuffer = 0xFFFFFFFF;
        uint numQuats = mCodec.mHeader.mNumQuats;
        uint numVec3s = mCodec.mHeader.mNumVec3s;
        uint numFloatVecs = mCodec.mHeader.mNumFloatVecs;
        uint vecCount = numQuats + numVec3s + numFloatVecs;

        mPrevMemBuffer = new M128[vecCount];
        mNextMemBuffer = new M128[vecCount];
    }

    public void Decompress()
    {
        for (uint i = 0; i < mDctAnim.NumFrames; i++)
        {
            DecompressBlock(i, out mNextMemBuffer);
        }
    }

    public unsafe bool DecompressBlock(uint targetFrame, out M128[] decompressedFrame)
    {
        uint targetBlock = targetFrame / 8;
        decompressedFrame = new M128[mDctAnim.Channels.Length];
        short[] unpackedData = new short[0];

        uint bitOffset = 0;
        uint columnBitLength = mCodec.GetColumnBitLength();
        if (targetBlock > 0)
        {
            uint column0BitLength = mCodec.GetBitLength_Column0Subblock0();
            bitOffset = (columnBitLength * targetBlock) - column0BitLength;
        }

        mCodec.UnpackColumn_Sequential(columnBitLength, bitOffset, unpackedData);

        mCodec.TransformSingleFrame_Sequential(unpackedData, targetFrame, decompressedFrame);
        return true;
    }

    public void SwapFrameBuffers()
    {
        M128[] temp = mPrevMemBuffer;
        uint tempFrame = mPrevFrameInBuffer;
        mPrevMemBuffer = mNextMemBuffer;
        mPrevFrameInBuffer = mNextFrameInBuffer;
        mNextMemBuffer = temp;
        mNextFrameInBuffer = tempFrame;
    }


}