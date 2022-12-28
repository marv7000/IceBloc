using IceBloc.InternalFormats;
using System.IO;
using System.Numerics;

namespace IceBloc.Frostbite.Animation;

public class FrameAnimation : Animation
{
    public int FloatCount = 0;
    public int Vec3Count = 0;
    public int QuatCount = 0;
    public float[] Data = new float[0];

    public FrameAnimation(Stream stream, ref GenericData gd, bool bigEndian)
    {
        using var r = new BinaryReader(stream);
        r.ReadGdDataHeader(out uint hash, out uint type, out uint baseOffset);

        var data = gd.ReadValues(r, baseOffset, type, false);

        Data = data["Data"] as float[];
        FloatCount = (int)data["FloatCount"];
        Vec3Count = (int)data["Vec3Count"];
        QuatCount = (int)data["QuatCount"];
    }

    public InternalAnimation ConvertToInternal()
    {
        InternalAnimation ret = new InternalAnimation();

        Vector3[] positions = new Vector3[Vec3Count];
        Quaternion[] rotations = new Quaternion[QuatCount];

        for (int i = 0; i < QuatCount; i++)
        {
            int floatDataIndex = i * 4;
            rotations[i] = new Quaternion(Data[floatDataIndex + 0], Data[floatDataIndex + 1], Data[floatDataIndex + 2], Data[floatDataIndex + 3]);
        }
        for (int i = 0; i < Vec3Count; i++)
        {
            int floatDataIndex = (i * 4) + (QuatCount * 4);
            positions[i] = new Vector3(Data[floatDataIndex + 0], Data[floatDataIndex + 1], Data[floatDataIndex + 2]);
        }

        ret.Frames = new InternalAnimation.Frame[1];

        ret.Frames[0].FrameIndex = 0;
        ret.Frames[0].Positions = positions;
        ret.Frames[0].Rotations = rotations;

        return ret;
    }
}
