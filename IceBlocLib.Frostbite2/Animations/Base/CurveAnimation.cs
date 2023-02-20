using IceBlocLib.InternalFormats;
using System.Numerics;
using System.Security.Cryptography;

namespace IceBlocLib.Frostbite2.Animations.Base;

public class CurveAnimation : Animation
{
    public ushort NumRotations;
    public ushort NumVectors;
    public ushort NumFloats;
    public float[] Values;
    public ushort[] Keys;
    public ushort[] ChannelOffsets;

    public CurveAnimation(Stream stream, int index, ref GenericData gd, bool bigEndian)
    {
        using var r = new BinaryReader(stream);
        r.ReadGdDataHeader(bigEndian, out uint hash, out uint type, out uint baseOffset);

        var data = gd.ReadValues(r, index, baseOffset, type, false);

        Name = (string)data["__name"];
        ID = (Guid)data["__guid"];
        FPS = (float)data["FPS"];
        NumRotations = (ushort)data["NumRotations"];
        NumVectors = (ushort)data["NumVectors"];
        NumFloats = (ushort)data["NumFloats"];
        Values = (float[])data["Values"];
        Keys = (ushort[])data["Keys"];
        ChannelOffsets = (ushort[])data["ChannelOffsets"];

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
    }

    public InternalAnimation ConvertToInternal()
    {
        InternalAnimation ret = new InternalAnimation();

        List<string> posChannels = new();
        List<string> rotChannels = new();

        var frame = new InternalAnimation.Frame();

        // Get all names.
        for (int i = 0; i < Channels.Length; i++)
        {
            if (Channels[i].EndsWith(".q"))
                rotChannels.Add(Channels[i].Replace(".q", ""));
            else if (Channels[i].EndsWith(".t"))
                posChannels.Add(Channels[i].Replace(".t", ""));
        }

        int dataIndex = 0;

        // For each frame.
        for (int frameIndex = 0; frameIndex < Keys.Length; frameIndex++)
        {
            List<Vector3> positions = new();
            List<Quaternion> rotations = new();

            for (int i = 0; i < NumRotations; i++)
            {
                rotations.Add(new Quaternion(Values[dataIndex++], Values[dataIndex++], Values[dataIndex++], Values[dataIndex++]));
            }
            for (int i = 0; i < NumVectors; i++)
            {
                positions.Add(new Vector3(Values[dataIndex++], Values[dataIndex++], Values[dataIndex++]));
            }

            frame.FrameIndex = Keys[frameIndex];
            frame.Positions = positions;
            frame.Rotations = rotations;
            ret.Frames.Add(frame);
        }

        ret.Name = Name;
        ret.PositionChannels = posChannels;
        ret.RotationChannels = rotChannels;
        ret.Additive = Additive;
        ret.AnimType = OriginalAnimType.CurveAnimation;

        return ret;
    }
}
