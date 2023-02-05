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
            var data = IO.ActiveCatalog.Extract(IO.Assets[("animations/antanimations/s_basicassets", InternalAssetType.RES)].MetaData, true, InternalAssetType.RES);
            using var stream = new MemoryStream(data);
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
        // Get the ChannelToDofAsset referenced by the Animation.
        var dof = GetDofAsset(channelToDofAsset, out GenericData gd);
        // Find the ClipControllerAsset which references the Animation.
        var clipController = gd["Anim", ID];
        // Set FPS
        FPS = (float)clipController["FPS"];
        // Get the LayoutHierarchyAsset Guid from the ClipController.
        clipController.TryGetValue("Target", out var guid);
        // Get the LayoutAssets from the LayoutHierarchyAsset.
        var layoutAssets = gd[(Guid)guid]["LayoutAssets"];

        List<string> channelNames = new();

        // Loop through all LayoutAssets and append them.
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

        //File.WriteAllLines(@$"D:\channelMap_{Name}.bin", output);

        return output;
    }
}
