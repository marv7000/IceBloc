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
    public Dictionary<string, BoneChannelType> Channels;
    public float FPS;

    public StorageType StorageType;

    public static GenericData BasicAssets = null;

    public Animation() { }

    public Animation(Stream stream, int index, ref GenericData gd, bool bigEndian)
    {
        using var r = new BinaryReader(stream);
        r.ReadGdDataHeader(bigEndian, out uint base_hash, out uint base_type, out uint base_baseOffset);

        var baseData = gd.ReadValues(r, index, base_baseOffset, base_type, bigEndian);

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
        if (BasicAssets == null)
        {
            var data = IO.ActiveCatalog.Extract(IO.Assets[("animations/antanimations/s_basicassets", InternalAssetType.RES)].MetaData, true, InternalAssetType.RES);
            using var stream = new MemoryStream(data);
            BasicAssets = new GenericData(stream);
        }
        return BasicAssets[channelToDofAsset];
    }

    public enum BoneChannelType
    {
        None = 0,
        Rotation = 14,
        Position = 2049856663,
        Scale = 2049856454,
    }

    public Dictionary<string, BoneChannelType> GetChannels(Guid channelToDofAsset)
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

        Dictionary<string, BoneChannelType> channelNames = new();

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
                        channelNames.Add((string)entries[x]["Name"], (BoneChannelType)(int)entries[x]["Type"]);
                    }
                }
                // Not sure what this does.
                else if (typeName == "DeltaTrajLayoutAsset")
                {
                    for (int x = 0; x < 8; x++)
                    {
                        channelNames.Add($"DeltaTrajectory{x}.traj", BoneChannelType.None);
                    }
                }
            }
        }

        byte[] data = (byte[])dof["IndexData"];
        List<string> channels = new();
         
        switch (StorageType)
        {
            // If we overwrite the channels, then just remap the orders.
            case StorageType.OVERWRITE:
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        channels.Add("");
                    }

                    for (int i = 0; i < data.Length; i++)
                    {
                        int channelId = data[i];
                        channels[i] = channelNames.ElementAt(channelId).Key;
                    }
                } break;
            // If we append the channels, the first byte indicates the taget, then second byte the value.
            case StorageType.APPEND:
                {
                    Dictionary<int, int> offsets = new();
                    int offset = 0;
                    for (int i = 0; i < data.Length; i+=2)
                    {
                        int appendTo = data[i];
                        int channelId = data[i+1];

                        offsets[appendTo] = offset;
                        offset++;

                        channels.Insert(offsets[appendTo], channelNames.ElementAt(channelId).Key);
                    }
                } break;
        }

        // Reorder
        Dictionary<string, BoneChannelType> output = new();
        for (int i = 0; i < channels.Count; i++)
        {
            output.TryAdd(channels[i], channelNames[channels[i]]);
        }

        return output;
    }
}
