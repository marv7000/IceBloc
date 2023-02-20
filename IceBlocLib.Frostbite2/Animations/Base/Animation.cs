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

    public StorageType StorageType;

    public static GenericData BasicAssets = null;

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

    private Dictionary<string, object> GetDofAsset(Guid channelToDofAsset)
    {
        if (Settings.CurrentGame == Game.Battlefield3)
        {
            if (BasicAssets == null)
            {
                var data = IO.ActiveCatalog.Extract(IO.Assets[("animations/antanimations/s_basicassets", InternalAssetType.RES)].MetaData, true, InternalAssetType.RES);
                using var stream = new MemoryStream(data);
                BasicAssets = new GenericData(stream);
            }
            return BasicAssets[channelToDofAsset];
        }

        return null;
    }

    public string[] GetChannels(Guid channelToDofAsset)
    {
        // Get the ChannelToDofAsset referenced by the Animation.
        var dof = GetDofAsset(channelToDofAsset);
        StorageType = (StorageType)(int)dof["StorageType"];

        // Find the ClipControllerAsset which references the Animation.
        var clipController = BasicAssets["Anim", ID];
        // Set FPS
        FPS = (float)clipController["FPS"];
        // Get the LayoutHierarchyAsset Guid from the ClipController.
        clipController.TryGetValue("Target", out var guid);
        // Get the LayoutAssets from the LayoutHierarchyAsset.
        var layoutAssets = BasicAssets[(Guid)guid]["LayoutAssets"];

        List<string> channelNames = new();

        // Loop through all LayoutAssets and append them.
        if (layoutAssets is Guid[] assets)
        {
            for (int i = 0; i < assets.Length; i++)
            {
                var layoutAsset = BasicAssets[assets[i]];

                string typeName = BasicAssets.Classes[BasicAssets.GetDataType(assets[i])].Name;
                if (typeName == "LayoutAsset")
                {
                    var entries = layoutAsset["Slots"] as Dictionary<string, object>[];

                    for (int x = 0; x < entries.Length; x++)
                    {
                        channelNames.Add((string)entries[x]["Name"]);
                    }
                }
            }
        }

        byte[] data = (byte[])dof["IndexData"];
        string[] output = new string[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            output[i] = channelNames[data[i]];
        }

        return output;
    }
}
