using IceBlocLib.Frostbite;
using IceBlocLib.Frostbite2.Animations;
using IceBlocLib.InternalFormats;

namespace IceBlocLib.Frostbite2.Misc;

public class AntPackageAsset
{
    public static List<InternalAnimation> ConvertToInternal(in Dbx dbx)
    {
        List<InternalAnimation> result = new();

        Guid guid = (Guid)dbx.Prim["StreamingGuid"].Value;
        using var chunk = new MemoryStream(IO.GetChunk(guid));

        GenericData gd = new(chunk);
        for (int i = 0; i < gd.Data.Count; i++)
        {
            using var stream = new MemoryStream(gd.Data[i].Bytes.ToArray());
            object entry = gd.Deserialize(stream);
            if (entry is FrameAnimation frameAnim)
                result.Add(frameAnim.ConvertToInternal());
            else if (entry is RawAnimation rawAnim)
                result.Add(rawAnim.ConvertToInternal());
        }
        return result;
    }
}
