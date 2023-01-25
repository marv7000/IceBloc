using IceBlocLib.Frostbite;
using IceBlocLib.Utility;
using System;
using System.IO;

namespace IceBlocLib.Frostbite2.Animations.Base;

public class Animation
{
    public string Name;

    public int CodecType;
    public int AnimId;
    public float TrimOffset;
    public ushort EndFrame;
    public bool Additive;
    public Guid ChannelToDofAsset;
    public byte[] IndexData;

    public Animation() { }

    public Animation(Stream stream, ref GenericData gd, bool bigEndian)
    {
        using var r = new BinaryReader(stream);
        r.ReadGdDataHeader(bigEndian, out uint base_hash, out uint base_type, out uint base_baseOffset);

        var baseData = gd.ReadValues(r, base_baseOffset, base_type, false);

        Name = (string)baseData["__name"];
        CodecType = (int)baseData["CodecType"];
        AnimId = (int)baseData["AnimId"];
        TrimOffset = (float)baseData["TrimOffset"];
        EndFrame = (ushort)baseData["EndFrame"];
        Additive = (bool)baseData["Additive"];
        ChannelToDofAsset = (Guid)baseData["ChannelToDofAsset"];
        IndexData = GetChannelToDofAsset(ChannelToDofAsset);
    }

    public static byte[] GetChannelToDofAsset(Guid channelToDofAsset)
    {
        if (Settings.CurrentGame == Game.Battlefield3)
        {
            var sha = IO.Assets[("animations/antanimations/s_basicassets", InternalAssetType.RES)].MetaData;
            using var stream = new MemoryStream(IO.ActiveCatalog.Extract(sha, true, InternalAssetType.RES));
            GenericData gd = new GenericData(stream);

            for (int i = 0; i < gd.Data.Count; i++)
            {
                using var s = new MemoryStream(gd.Data[i].Bytes.ToArray());
                using var r = new BinaryReader(s);
                r.ReadGdDataHeader(gd.Data[i].BigEndian, out uint base_hash, out uint base_type, out uint base_baseOffset);
                var values = gd.ReadValues(r, base_baseOffset, base_type, false);

                if ((Guid)values["__guid"] == channelToDofAsset)
                {
                    return values["IndexData"] as byte[];
                }
            }
        }
        return null;
    }
}
