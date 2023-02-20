using IceBlocLib.InternalFormats;
using System.Collections.Generic;
using System.Numerics;

namespace IceBlocLib.Frostbite2.Animations.Base;

public class FrameAnimation : Animation
{
    public int FloatCount = 0;
    public int Vec3Count = 0;
    public int QuatCount = 0;
    public float[] Data = new float[0];

    public FrameAnimation(Stream stream, int index, ref GenericData gd, bool bigEndian)
    {
        using var r = new BinaryReader(stream);
        r.ReadGdDataHeader(bigEndian, out uint hash, out uint type, out uint baseOffset);

        var data = gd.ReadValues(r, index, baseOffset, type, false);

        Name = (string)data["__name"];
        ID = (Guid)data["__guid"];
        Data = data["Data"] as float[];
        FloatCount = (int)data["FloatCount"];
        Vec3Count = (int)data["Vec3Count"];
        QuatCount = (int)data["QuatCount"];

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
        List<Vector3> positions = new();
        List<Quaternion> rotations = new();

        List<string> posChannels = new();
        List<string> rotChannels = new();

        int dataIndex = 0;
        // Get all names.
        for (int i = 0; i < Channels.Length; i++)
        {
            
            if (Channels[i].EndsWith(".q"))
            {
                rotChannels.Add(Channels[i].Replace(".q", ""));
                rotations.Add(new Quaternion(Data[dataIndex++], Data[dataIndex++], Data[dataIndex++], Data[dataIndex++]));
            }
            else if (Channels[i].EndsWith(".t"))
            {
                posChannels.Add(Channels[i].Replace(".t", ""));
                positions.Add(new Vector3(Data[dataIndex++], Data[dataIndex++], Data[dataIndex++]));
                dataIndex++;
            }
        }

        // FrameAnimations are like RawAnimations but with one fixed frame.
        ret.Name = Name;
        var frame = new InternalAnimation.Frame();
        frame.FrameIndex = 0;
        frame.Positions = positions;
        frame.Rotations = rotations;
        ret.Frames.Add(frame);
        ret.PositionChannels = posChannels;
        ret.RotationChannels = rotChannels;
        ret.Additive = Additive;
        ret.AnimType = OriginalAnimType.FrameAnimation;

        return ret;
    }
}
