using IceBlocLib.Frostbite2.Animations.DCT;
using IceBlocLib.InternalFormats;
using System;

namespace IceBlocLib.Frostbite2.Animations.Base;

public class DctAnimation : Animation
{
    public ushort[] KeyTimes = new ushort[0];
    public byte[] Data = new byte[0];
    public byte[] SourceCompressedAll = new byte[0];
    public ushort NumKeys;
    public ushort NumVec3;
    public ushort NumFloat;
    public int DataSize;
    public bool Cycle;

    public ushort NumFrames;
    public ushort NumQuats;
    public ushort NumFloatVec;
    public ushort QuantizeMult_Block;
    public byte QuantizeMult_Subblock;
    public byte CatchAllBitCount;
    public byte[] NumSubblocks; // Bitfield at offset 4, needs to be bitshifted right when read.

    public DctAnimation(Stream stream, int index, ref GenericData gd, bool bigEndian)
    {
        using var r = new BinaryReader(stream);
        r.ReadGdDataHeader(bigEndian, out uint hash, out uint type, out uint baseOffset);

        var data = gd.ReadValues(r, index, baseOffset, type, false);

        Name = (string)data["__name"];
        ID = (Guid)data["__guid"];
        KeyTimes = data["KeyTimes"] as ushort[];
        Data = data["Data"] as byte[];
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
        // Bitfield at offset 4, needs to be bitshifted right when read.
        for (int i = 0; i < dofTableDescBytes.Length; i++) { dofTableDescBytes[i] >>= 4; }
        NumSubblocks = dofTableDescBytes;

        // Read the Base class (Animation).
        r.BaseStream.Position = (long)data["__base"];
        r.ReadGdDataHeader(bigEndian, out uint base_hash, out uint base_type, out uint base_baseOffset);

        var baseData = gd.ReadValues(r, index, (uint)((long)data["__base"] + base_baseOffset), base_type, false);

        CodecType = (int)baseData["CodecType"];
        AnimId = (int)baseData["AnimId"];
        TrimOffset = (float)baseData["TrimOffset"];
        EndFrame = (ushort)baseData["EndFrame"];
        Additive = (bool)baseData["Additive"];
        ChannelToDofAsset = (Guid)baseData["ChannelToDofAsset"];
        SourceCompressedAll = PatchSourceChunk(r, type, in gd);
        Channels = GetChannels(ChannelToDofAsset);
    }

    /// <summary>
    /// Rearrange the data so that it matches what the <see cref="FIXED_Header"/> expects.
    /// </summary>
    public byte[] PatchSourceChunk(BinaryReader r, uint type, in GenericData gd)
    {
        r.BaseStream.Position = 0;

        int headerOffset = 0;
        int dataOffset = 0;
        foreach (var element in gd.Classes[type].Elements)
        {
            if (element.Name == "NumKeys") headerOffset = element.Offset;
            else if (element.Name == "DofTableDescBytes") dataOffset = element.Offset;
        }

        r.BaseStream.Position = headerOffset + 32;
        byte[] headerBytes = r.ReadBytes(12);


        r.BaseStream.Position = dataOffset + 32 + 8;
        r.BaseStream.Position = r.ReadUInt32();
        byte[] dataBytes = r.ReadBytes(DataSize - 12);

        byte[] result = headerBytes.Concat(dataBytes).ToArray();

        return result;
    }

    public unsafe InternalAnimation ConvertToInternal()
    {
        InternalAnimation ret = new();

        ret.Name = Name;

        // TODO ChannelToDofAsset
        var decompressor = new DCTAnimDecompressor(this, null);
        decompressor.mCodec.GetHeader();
        return ret;
    }
}