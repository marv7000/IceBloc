﻿using IceBlocLib.Frostbite;
using IceBlocLib.Frostbite2013.Animations.Base;
using IceBlocLib.Frostbite2013.Animations.DCT;
using IceBlocLib.InternalFormats;
using IceBlocLib.Utility;

namespace IceBlocLib.Frostbite2013.Misc;

public class AntPackageAsset
{
    public static List<InternalAnimation> ConvertToInternal(in Dbx dbx)
    {
        Guid guid = (Guid)dbx.Prim["StreamingGuid"].Value;
        if (guid == Guid.Empty)
            return new List<InternalAnimation>();

        using var chunk = new MemoryStream(IO.GetChunk(guid));

        return ConvertToInternal(chunk);
    }

    public static List<InternalAnimation> ConvertToInternal(Stream chunk)
    {
        List<InternalAnimation> result = new();
        GenericData gd = new(chunk);
        for (int i = 0; i < gd.Data.Count; i++)
        {
            using var stream = new MemoryStream(gd.Data[i].Bytes.ToArray());
            object entry = gd.Deserialize(stream, i, gd.Data[i].BigEndian);
            if (entry is FrameAnimation frameAnim)
                result.Add(frameAnim.ConvertToInternal());
            else if (entry is RawAnimation rawAnim)
                result.Add(rawAnim.ConvertToInternal());
            else if (entry is DctAnimation dctAnim)
                result.Add(dctAnim.ConvertToInternal());

            Settings.Progress = (double)(i / (gd.Data.Count - 1));
        }
        return result;
    }
}
