using IceBloc.Frostbite;
using IceBloc.Frostbite.Meshes;
using IceBloc.Frostbite.Textures;
using IceBloc.InternalFormats;

namespace IceBloc.Utility;

public class AssetListItem
{
    public string Name { get; set; }
    public ResType Type { get; set; }
    public InternalAssetType AssetType { get; set; }
    public long Size { get; set; }
    public ExportStatus Status { get; set; }

    public byte[] MetaData;

    public AssetListItem(string name, ResType type, InternalAssetType iaType, long size, ExportStatus status, byte[] sha)
    {
        Name = name;
        Type = type;
        AssetType = iaType;
        Size = size;
        Status = status;
        MetaData = sha;
    }

    public void Export()
    {
        string path = $"Output\\{Settings.CurrentGame}\\{Name}";
        Directory.CreateDirectory(Path.GetDirectoryName(path)); // Make sure the output directory exists.

        byte[] data = IO.ActiveCatalog.Extract(MetaData, true, AssetType);

        // If the user wants to export the raw RES.
        if (Settings.ExportRaw)
        {
            File.WriteAllBytes(path + ("_raw." + Type).ToLower(), data);
        }

        if (Settings.ExportConverted)
        {
            using var stream = new MemoryStream(data);
            switch (Type)
            {
                case ResType.DxTexture:
                    {
                        InternalTexture output = DxTexture.ConvertToInternal(stream);
                        Settings.CurrentTextureExporter.Export(output, path);
                        break;
                    }
                case ResType.MeshSet:
                    {
                        List<InternalMesh> output = MeshSet.ConvertToInternal(stream, out var chunk);
                        for (int i = 0; i < output.Count; i++)
                        {
                            Settings.CurrentModelExporter.Export(output[i], path + i.ToString());
                        }
                        break;
                    }
                case ResType.EBX:
                    {
                        var output = new Dbx(stream);
                        output.Dump(path + ".ebx");
                        break;
                    }
                default:
                    break;
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