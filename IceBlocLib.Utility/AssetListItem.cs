using IceBlocLib.Frostbite;
using IceBlocLib.InternalFormats;

namespace IceBlocLib.Utility;

public class AssetListItem
{
    public string Name { get; set; }
    public string Type { get; set; }
    public InternalAssetType AssetType { get; set; }
    public long Size { get; set; }

    public byte[] MetaData;

    public static InternalSkeleton LastSkeleton { get; set; }
    public static InternalMesh LastMesh { get; set; }

    public AssetListItem(string name, string type, InternalAssetType iaType, long size, byte[] sha)
    {
        Name = name;
        Type = type;
        AssetType = iaType;
        Size = size;
        MetaData = sha;

    }
}
