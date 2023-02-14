using IceBlocLib.InternalFormats;
using System.Numerics;
using static IceBlocLib.InternalFormats.InternalAnimation;
using IceBlocLib.Frostbite2013.Animations.DCT;

namespace IceBlocLib.Frostbite2013.Animations.Base;

public class DctAnimation : Animation
{
    public ushort[] KeyTimes = new ushort[0];
    public byte[] Data = new byte[0];
    public ushort NumKeys;
    public ushort NumVec3;
    public ushort NumFloat;
    public int DataSize;
    public bool Cycle;

    public ushort NumQuats;
    public ushort NumFloatVec;
    public ushort QuantizeMultBlock;
    public byte QuantizeMultSubblock;
    public byte CatchAllBitCount;
    public byte[] DofTableDescBytes;
    public short[] DeltaBaseX;
    public short[] DeltaBaseY;
    public short[] DeltaBaseZ;
    public short[] DeltaBaseW;
    public ushort[] BitsPerSubblock;

    private List<Vector4> DecompressedData = new();

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
        QuantizeMultBlock = (ushort)data["QuantizeMultBlock"];
        QuantizeMultSubblock = (byte)data["QuantizeMultSubblock"];
        CatchAllBitCount = (byte)data["CatchAllBitCount"];

        DofTableDescBytes = data["DofTableDescBytes"] as byte[];

        DeltaBaseX = data["DeltaBaseX"] as short[];
        DeltaBaseY = data["DeltaBaseY"] as short[];
        DeltaBaseZ = data["DeltaBaseZ"] as short[];
        DeltaBaseW = data["DeltaBaseW"] as short[];
        BitsPerSubblock = data["BitsPerSubblock"] as ushort[];

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
        Channels = GetChannels(ChannelToDofAsset);

        // Decompress the animation.
        DecompressedData = this.Decompress();
    }

    public InternalAnimation ConvertToInternal()
    {
        InternalAnimation ret = new();

        List<string> posChannels = new();
        List<string> rotChannels = new();

        // Get all names.
        for (int i = 0; i < Channels.Length; i++)
        {
            if (Channels[i].EndsWith(".q"))
                rotChannels.Add(Channels[i].Replace(".q", ""));
            else if (Channels[i].EndsWith(".t"))
                posChannels.Add(Channels[i].Replace(".t", ""));
        }

        // Assign values to Channels.
        for (int i = 0; i < KeyTimes.Length; i++)
        {
            Frame frame = new Frame();

            for (int channelIdx = 0; channelIdx < rotChannels.Count; channelIdx++)
            {
                int pos = (int)(i * GetDofCount() + channelIdx);
                Vector4 element = DecompressedData[pos];
                frame.Rotations.Add(Quaternion.Normalize(new Quaternion(element.X, element.Y, element.Z, element.W)));
            }
            for (int channelIdx = 0; channelIdx < posChannels.Count; channelIdx++)
            {
                int pos = (int)(i * GetDofCount() + NumQuats + channelIdx);
                Vector4 element = DecompressedData[pos];
                frame.Positions.Add(new Vector3(element.X, element.Y, element.Z));
            }
            ret.Frames.Add(frame);
        }

        for (int i = 0; i < KeyTimes.Length; i++)
        {
            Frame f = ret.Frames[i];
            f.FrameIndex = KeyTimes[i];
            ret.Frames[i] = f;
        }

        ret.Name = Name;
        ret.PositionChannels = posChannels;
        ret.RotationChannels = rotChannels;
        ret.Additive = Additive;
        return ret;
    }

    private int GetDofCount()
    {
        return NumQuats + NumVec3 + NumFloatVec;
    }
}