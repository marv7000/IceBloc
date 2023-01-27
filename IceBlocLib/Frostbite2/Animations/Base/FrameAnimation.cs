using IceBlocLib.InternalFormats;
using System.Numerics;

namespace IceBlocLib.Frostbite2.Animations.Base;

public class FrameAnimation : Animation
{
    public int FloatCount = 0;
    public int Vec3Count = 0;
    public int QuatCount = 0;
    public float[] Data = new float[0];

    public FrameAnimation(Stream stream, ref GenericData gd, bool bigEndian)
    {
        using var r = new BinaryReader(stream);
        r.ReadGdDataHeader(bigEndian, out uint hash, out uint type, out uint baseOffset);

        var data = gd.ReadValues(r, baseOffset, type, false);

        Name = (string)data["__name"];
        Data = data["Data"] as float[];
        FloatCount = (int)data["FloatCount"];
        Vec3Count = (int)data["Vec3Count"];
        QuatCount = (int)data["QuatCount"];

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

    public InternalAnimation ConvertToInternal()
    {
        InternalAnimation ret = new InternalAnimation();
        ret.Name = Name;
        Vector3[] positions = new Vector3[Vec3Count];
        Quaternion[] rotations = new Quaternion[QuatCount];

        for (int i = 0; i < QuatCount; i++)
        {
            int floatDataIndex = i * 4;
            rotations[i] = new Quaternion(Data[floatDataIndex + 0], Data[floatDataIndex + 1], Data[floatDataIndex + 2], Data[floatDataIndex + 3]);
        }
        for (int i = 0; i < Vec3Count; i++)
        {
            int floatDataIndex = i * 4 + QuatCount * 4;
            positions[i] = new Vector3(Data[floatDataIndex + 0], Data[floatDataIndex + 1], Data[floatDataIndex + 2]);
        }

        ret.Frames = new InternalAnimation.Frame[1];

        ret.Frames[0].FrameIndex = 0;
        ret.Frames[0].Positions = positions;
        ret.Frames[0].Rotations = rotations;


        return ret;
    }
}
