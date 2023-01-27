using IceBlocLib.Frostbite;
using IceBlocLib.Utility;

namespace IceBlocLib.Frostbite2.Animations.Base;

public class Animation
{
    public string Name;

    public int CodecType;
    public int AnimId;
    public float TrimOffset;
    public ushort EndFrame;
    public bool Additive;
    public Guid ID;
    public Guid ChannelToDofAsset;
    public string[] Channels;
    public float FPS;

    public Animation() { }

    public Animation(Stream stream, int index, ref GenericData gd, bool bigEndian)
    {
        using var r = new BinaryReader(stream);
        r.ReadGdDataHeader(bigEndian, out uint base_hash, out uint base_type, out uint base_baseOffset);

        var baseData = gd.ReadValues(r, index, base_baseOffset, base_type, false);

        Name = (string)baseData["__name"];
        ID = (Guid)baseData["__guid"];
        CodecType = (int)baseData["CodecType"];
        AnimId = (int)baseData["AnimId"];
        TrimOffset = (float)baseData["TrimOffset"];
        EndFrame = (ushort)baseData["EndFrame"];
        Additive = (bool)baseData["Additive"];
        ChannelToDofAsset = (Guid)baseData["ChannelToDofAsset"];
        Channels = GetChannels(ChannelToDofAsset);
    }

    private static Dictionary<string, object> GetDofAsset(Guid channelToDofAsset, out GenericData gd)
    {
        if (Settings.CurrentGame == Game.Battlefield3)
        {
            var sha = IO.Assets[("animations/antanimations/s_basicassets", InternalAssetType.RES)].MetaData;
            using var stream = new MemoryStream(IO.ActiveCatalog.Extract(sha, true, InternalAssetType.RES));
            gd = new GenericData(stream);

            var dof = gd[channelToDofAsset];

            return dof;
        }
        else
        {
            gd = null;
        }

        return null;
    }

    public string[] GetChannels(Guid channelToDofAsset)
    {
        var dof = GetDofAsset(channelToDofAsset, out GenericData gd);
        var clipController = gd["Anim", ID];
        FPS = (float)clipController["FPS"];
        clipController.TryGetValue("Target", out var guid);
        var layoutAssets = gd[(Guid)guid]["LayoutAssets"];

        List<string> channelNames = new();

        if (layoutAssets is Guid[] assets)
        {
            for (int i = 0; i < assets.Length; i++)
            {
                var layoutAsset = gd[assets[i]];
                var entries = layoutAsset["Slots"] as Dictionary<string, object>[];
                for (int x = 0; x < entries.Length; x++)
                {
                    channelNames.Add((string)entries[x]["Name"]);
                }
            }
        }

        byte[] data = dof["IndexData"] as byte[];
        string[] output = new string[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            output[i] = channelNames[data[i]];
        }

        return output;
    }
}
