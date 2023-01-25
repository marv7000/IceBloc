using IceBlocLib.Frostbite2.Animations.Base;
using IceBlocLib.Frostbite2.Animations.Misc;
using IceBlocLib.Utility;

namespace IceBlocLib.Frostbite2.Animations.DCT;

public class DCTAnimDecompressor
{
    public DctAnimation mDctAnim;
    public ChannelDofMap mDofMap;
    public FIXED_Decompressor mCodec;
    public M128[] mPrevMemBuffer;
    public M128[] mNextMemBuffer;
    public uint mPrevFrameInBuffer;
    public uint mNextFrameInBuffer;

    public DCTAnimDecompressor(DctAnimation dctAnim, ChannelDofMap dofMap)
    {
        mDctAnim = dctAnim;
        mDofMap = dofMap;
        mCodec = new FIXED_Decompressor(dctAnim.Data, Frostbite.TargetEndian.TARGET_ENDIAN_LITTLE);
        mPrevFrameInBuffer = 0xFFFFFFFF;
        mNextFrameInBuffer = 0xFFFFFFFF;
        uint numQuats = mCodec.mHeader.mNumQuats;
        uint numVec3s = mCodec.mHeader.mNumVec3s;
        uint numFloatVecs = mCodec.mHeader.mNumFloatVecs;
        uint vecCount = numQuats + numVec3s + numFloatVecs;

        mPrevMemBuffer = new M128[vecCount];
        mNextMemBuffer = new M128[vecCount];
    }
}

public class LayoutMask
{
}