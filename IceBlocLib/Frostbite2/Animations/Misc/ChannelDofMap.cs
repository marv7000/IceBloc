using IceBlocLib.Frostbite2.Animations.DCT;

namespace IceBlocLib.Frostbite2.Animations.Misc;

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
