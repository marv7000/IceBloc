using IceBlocLib.InternalFormats;
using System.IO;
using System.Numerics;

namespace IceBlocLib.Frostbite2.Animations;

public class RawAnimation : Animation
{
    public int NumKeys;
    public int FloatCount;
    public int Vec3Count;
    public int QuatCount;
    public ushort[] KeyTimes;
    public float[] Data;
    public bool Cycle;

    public RawAnimation(Stream stream, ref GenericData gd, bool bigEndian)
    {
        using var r = new BinaryReader(stream);
        r.ReadGdDataHeader(bigEndian, out uint hash, out uint type, out uint baseOffset);

        var data = gd.ReadValues(r, baseOffset, type, false);

        NumKeys = (int)data["NumKeys"];
        FloatCount = (int)data["FloatCount"];
        Vec3Count = (int)data["Vec3Count"];
        QuatCount = (int)data["QuatCount"];
        KeyTimes = data["KeyTimes"] as ushort[];
        Data = data["Data"] as float[];
        Cycle = (bool)data["Cycle"];

        // Read the Base class (Animation).
        r.BaseStream.Position = (long)data["__base"];
        r.ReadGdDataHeader(bigEndian, out uint base_hash, out uint base_type, out uint base_baseOffset);

        var baseData = gd.ReadValues(r, (uint)((long)data["__base"] + base_baseOffset), base_type, false);

        CodecType = (int)baseData["CodecType"];
        AnimId = (int)baseData["AnimId"];
        TrimOffset = (float)baseData["TrimOffset"];
        EndFrame = (ushort)baseData["EndFrame"];
        Additive = (bool)baseData["Additive"];
    }

    public InternalAnimation ConvertToInternal()
    {
        InternalAnimation ret = new InternalAnimation();

        ret.Frames = new InternalAnimation.Frame[NumKeys];

        for (int i = 0; i < NumKeys; i++)
        {
            Vector3[] positions = new Vector3[Vec3Count];
            Quaternion[] rotations = new Quaternion[QuatCount];

            for (int k = 0; k < QuatCount; k++)
            {
                int floatDataIndex = k * 4;
                rotations[k] = new Quaternion(Data[floatDataIndex + 0], Data[floatDataIndex + 1], Data[floatDataIndex + 2], Data[floatDataIndex + 3]);
            }
            for (int k = 0; k < Vec3Count; k++)
            {
                int floatDataIndex = k * 4 + QuatCount * 4;
                positions[k] = new Vector3(Data[floatDataIndex + 0], Data[floatDataIndex + 1], Data[floatDataIndex + 2]);
            }

            ret.Frames[i].FrameIndex = KeyTimes[i];
            ret.Frames[i].Positions = positions;
            ret.Frames[i].Rotations = rotations;
        }

        return ret;
    }
}
