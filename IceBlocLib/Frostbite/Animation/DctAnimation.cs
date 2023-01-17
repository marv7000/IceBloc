using IceBloc.InternalFormats;
using System;
using System.IO;

namespace IceBloc.Frostbite.Animation;

public class DctAnimation : Animation
{
    public ushort[] KeyTimes;
    public byte[] mData;
    public ushort NumKeys;
    public ushort NumVec3;
    public ushort NumFloat;
    public int DataSize;
    public bool Cycle;

    // EA::Ant::Anim::DCT::FIXED_Header
    public ushort NumFrames;
    public ushort NumQuats;
    public ushort NumFloatVec;
    public ushort QuantizeMult_Block;
    public byte QuantizeMult_Subblock;
    public byte CatchAllBitCount;
    public byte[] NumSubblocks; // Bitfield at offset 4, needs to be bitshifted right when read.

    public DctAnimation(Stream stream, ref GenericData gd, bool bigEndian)
    {
        using var r = new BinaryReader(stream);
        r.ReadGdDataHeader(bigEndian, out uint hash, out uint type, out uint baseOffset);

        var data = gd.ReadValues(r, baseOffset, type, false);

        KeyTimes = data["KeyTimes"] as ushort[];
        mData = data["Data"] as byte[];
        NumKeys = (ushort)data["NumKeys"];
        NumVec3 = (ushort)data["NumVec3"];
        NumFloat = (ushort)data["NumFloat"];
        DataSize = (int)data["DataSize"];
        Cycle = (bool)data["Cycle"];

        NumQuats = (ushort)data["NumQuats"];
        NumFloatVec = (ushort)data["NumFloatVec"];
        QuantizeMult_Block = (ushort)data["QuantizeMultBlock"];
        QuantizeMult_Subblock = (byte)data["QuantizeMultSubblock"];
        CatchAllBitCount = (byte)data["CatchAllBitCount"];

        byte[] dofTableDescBytes = data["DofTableDescBytes"] as byte[];
        for (int i = 0; i < dofTableDescBytes.Length; i++) { dofTableDescBytes[i] >>= 4; }
        NumSubblocks = dofTableDescBytes;

        // Read the Base class (Animation).
        r.BaseStream.Position = (long)data["__base"];
        r.ReadGdDataHeader(bigEndian, out uint base_hash, out uint base_type, out uint base_baseOffset);

        var baseData = gd.ReadValues(r, (uint)((long)data["__base"] + base_baseOffset), base_type, false);

        CodecType = (int)baseData["CodecType"];
        AnimId = (int)baseData["AnimId"];
        TrimOffset = (float)baseData["TrimOffset"];
        EndFrame = (ushort)baseData["EndFrame"];
        Additive = (bool)baseData["Additive"];
        ChannelToDofAsset = (Guid)baseData["ChannelToDofAsset"];
}

    public unsafe InternalAnimation ConvertToInternal()
    {
        InternalAnimation ret = new();

        //var a = sizeof(FIXED_Header);

        //var decompressor = new DctAnimDecompressor(this, new ChannelDofMap(), new ScratchPad());

        return ret;
    }

}
