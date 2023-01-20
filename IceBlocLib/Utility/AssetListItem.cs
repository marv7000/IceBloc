using IceBlocLib.Frostbite;
using IceBlocLib.Frostbite2.Meshes;
using IceBlocLib.Frostbite2.Misc;
using IceBlocLib.Frostbite2.Textures;
using IceBlocLib.InternalFormats;

namespace IceBlocLib.Utility;

public class AssetListItem
{
    public string Name { get; set; }
    public string Type { get; set; }
    public InternalAssetType AssetType { get; set; }
    public long Size { get; set; }
    public ExportStatus Status { get; set; }

    public byte[] MetaData;

    public AssetListItem(string name, string type, InternalAssetType iaType, long size, ExportStatus status, byte[] sha)
    {
        Name = name;
        Type = type;
        AssetType = iaType;
        Size = size;
        Status = status;
        MetaData = sha;

        if (type == "EBX")
        {
            using var s = new MemoryStream(IO.ActiveCatalog.Extract(sha, true, iaType));
            var d = new Dbx(s);
            int n = d.Instances[d.PrimaryInstanceGuid].Desc.Name;
            Type = Ebx.StringTable[n];
        }
    }

    public void Export()
    {
        string path = $"Output\\{Settings.CurrentGame}\\{Name}";
        Directory.CreateDirectory(Path.GetDirectoryName(path)); // Make sure the output directory exists.

        byte[] data = IO.ActiveCatalog.Extract(MetaData, true, AssetType);

        // If the user wants to export the raw RES.
        if (Settings.ExportRaw)
        {
            File.WriteAllBytes(path + ("_raw." + Type), data);
        }

        if (Settings.ExportConverted)
        {
            using var stream = new MemoryStream(data);
            if (AssetType == InternalAssetType.EBX)
            {
                var dbx = new Dbx(stream);
                if (Type == "SkeletonAsset")
                {
                    SkeletonAsset.ConvertToInternal(in dbx);
                }
                else
                {
                    dbx.Dump(path + ".ebx");
                }
            }
            else if (AssetType == InternalAssetType.RES)
            {
                if (Type == "DxTexture")
                {
                    InternalTexture output = DxTexture.ConvertToInternal(stream);
                    Settings.CurrentTextureExporter.Export(output, path);
                }
                if (Type == "MeshSet")
                {
                    List<InternalMesh> output = MeshSet.ConvertToInternal(stream, out var chunk);
                    for (int i = 0; i < output.Count; i++)
                    {
                        Settings.CurrentModelExporter.Export(output[i], path + i.ToString());
                    }
                }
            }
        }
        Console.WriteLine($"Exported {Name}...");
        Status = ExportStatus.Exported;
    }
}

public enum InternalAssetType
{
    Unknown,
    Chunk,
    EBX,
    RES
}

public enum ExportStatus
{
    Error,
    Ready,
    Exported
}