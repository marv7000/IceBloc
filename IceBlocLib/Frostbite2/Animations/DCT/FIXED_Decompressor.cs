using IceBlocLib.Frostbite;
using IceBlocLib.Utility;

namespace IceBlocLib.Frostbite2.Animations.DCT;

public class FIXED_Decompressor
{
    public Memory<byte> mCompressedSource_All;
    public TargetEndian mTargetEndian;
    public FIXED_Header mHeader;
    public FIXED_DofTableDescriptor[] mDofTableDescriptor = new FIXED_DofTableDescriptor[0];
    public FIXED_DofTable mDofTable;
    public Memory<byte> mCompressedData;

    public FIXED_Decompressor(Memory<byte> CompressedSource_All, TargetEndian targetEndian)
    {
        mCompressedSource_All = CompressedSource_All;
        mTargetEndian = targetEndian;
    }

    public unsafe void GetHeader()
    {
        // Read the header from the source.
        mHeader = UnsafeOperations.StructFromMemory<FIXED_Header>(mCompressedSource_All);

        // Get all sizes for DCT parts.
        int headerSize = FIXED_Header.GetSerializedSize();
        int dofTableDescriptorSize = FIXED_DofTableDescriptor.GetSerializedEntrySize() * mHeader.GetNumTableEntriesPerFrame();
        int dofTableSize = (int)FIXED_DofTableDescriptor.GetSerializedDofTableSize(in mDofTableDescriptor);

        mDofTableDescriptor = new FIXED_DofTableDescriptor[mHeader.GetNumTableEntriesPerFrame()];
        for (int i = 0; i < mHeader.GetNumTableEntriesPerFrame(); i++)
        {
            mDofTableDescriptor[i] = UnsafeOperations.StructFromMemory<FIXED_DofTableDescriptor>(mCompressedSource_All, headerSize);
        }

        mDofTable = UnsafeOperations.StructFromMemory<FIXED_DofTable>(mCompressedSource_All, headerSize + dofTableSize);

        mCompressedData = mCompressedSource_All.Slice(headerSize + dofTableDescriptorSize + dofTableSize);
    }
}