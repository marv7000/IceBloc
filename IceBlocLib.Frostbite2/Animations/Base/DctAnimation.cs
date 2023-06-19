using IceBlocLib.InternalFormats;
using System.Numerics;
using static IceBlocLib.InternalFormats.InternalAnimation;

namespace IceBlocLib.Frostbite2.Animations.Base;

public partial class DctAnimation : Animation
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

        var data = gd.ReadValues(r, index, baseOffset, type, bigEndian);

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

        var baseData = gd.ReadValues(r, index, (uint)((long)data["__base"] + base_baseOffset), base_type, bigEndian);

        CodecType = (int)baseData["CodecType"];
        AnimId = (int)baseData["AnimId"];
        TrimOffset = (float)baseData["TrimOffset"];
        EndFrame = (ushort)baseData["EndFrame"];
        Additive = (bool)baseData["Additive"];
        ChannelToDofAsset = (Guid)baseData["ChannelToDofAsset"];
        Channels = GetChannels(ChannelToDofAsset);

        // Decompress the animation.
        DecompressedData = Decompress();
    }

    public InternalAnimation ConvertToInternal()
    {
        InternalAnimation ret = new();

        List<string> posChannels = new();
        List<string> rotChannels = new();
        List<string> scaleChannels = new();

        if (Name == "ParachuteRight Anim")
        {
            Console.Write("foo");
        }

        // Get all names.
        foreach (var channel in Channels)
        {
            if (channel.Value == BoneChannelType.Rotation)
                rotChannels.Add(channel.Key);
            else if (channel.Value == BoneChannelType.Position)
                posChannels.Add(channel.Key);
            else if (channel.Value == BoneChannelType.Scale)
                scaleChannels.Add(channel.Key);
        }

        // Assign values to Channels.

        var dofCount = NumQuats + NumVec3 + NumFloatVec;

        for (int i = 0; i < KeyTimes.Length; i++)
        {
            Frame frame = new Frame();

            List<Quaternion> rotations = new();
            List<Vector3> positions = new();
            List<Vector3> scales = new();

            for (int channelIdx = 0; channelIdx < NumQuats; channelIdx++)
            {
                int pos = (int)(i * dofCount + channelIdx);
                Vector4 element = DecompressedData[pos];

                rotations.Add(Quaternion.Normalize(new Quaternion(element.X, element.Y, element.Z, element.W)));
            }
            // We need to differentiate between Scale and Position.
            for (int channelIdx = 0; channelIdx < NumVec3; channelIdx++)
            {
                if (Channels.ElementAt(NumQuats + channelIdx).Value == BoneChannelType.Position)
                {
                    int pos = (int)(i * dofCount + NumQuats + channelIdx);
                    Vector4 element = DecompressedData[pos];

                    positions.Add(new Vector3(element.X, element.Y, element.Z));
                }
            }

            frame.Rotations = rotations;
            frame.Positions = positions;

            ret.Frames.Add(frame);
        }

        for (int i = 0; i < KeyTimes.Length; i++)
        {
            Frame f = ret.Frames[i];
            f.FrameIndex = KeyTimes[i];
            ret.Frames[i] = f;
        }

        for (int r = 0; r < rotChannels.Count; r++)
            rotChannels[r] = rotChannels[r].Replace(".q", "");
        for (int r = 0; r < posChannels.Count; r++)
            posChannels[r] = posChannels[r].Replace(".t", "");
        for (int r = 0; r < scaleChannels.Count; r++)
            scaleChannels[r] = scaleChannels[r].Replace(".s", "");

        ret.Name = Name;
        ret.PositionChannels = posChannels;
        ret.RotationChannels = rotChannels;
        ret.Additive = Additive;
        ret.AnimType = OriginalAnimType.DctAnimation;
        return ret;
    }
}