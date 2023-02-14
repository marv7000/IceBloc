namespace IceBlocLib.Utility;

public interface IOInterface
{
    public Dictionary<(string, InternalAssetType), AssetListItem> GetAssets();
}
