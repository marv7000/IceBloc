using System;
using System.IO;
using System.Linq;

using IceBloc.Frostbite;
using IceBloc.Frostbite.Packed;

namespace IceBloc.Utility;

public class AssetListItem
{
    public string Name { get; set; }
    public AssetType Type { get; set; }
    public long Size { get; set; }
    public ExportStatus Status { get; set; }
    public string Remarks { get; set; }

    public byte[] CatalogEntrySHA;

    public AssetListItem(string name, AssetType type, long size, ExportStatus status, string remarks, byte[] sha)
    {
        Name = name;
        Type = type;
        Size = size;
        Status = status;
        Remarks = remarks;
        CatalogEntrySHA = sha;
    }

    public void Export()
    {
        try
        {
            switch (Type)
            {
                case AssetType.Unknown:
                case AssetType.Chunk:
                    var entry = MainWindow.ActiveCatalog.GetEntry(CatalogEntrySHA);
                    Directory.CreateDirectory("Output");
                    File.WriteAllBytes($"Output\\{Name}.bin", MainWindow.ActiveCatalog.Extract(entry.SHA, true));
                    break;
            }
            Status = ExportStatus.Exported;
        }
        catch(Exception ex)
        {
            Status = ExportStatus.Error;
            Remarks = ex.Message;
        }
    }
}

public enum AssetType
{
    Unknown,
    Chunk,
    EBX,
    Mesh,
    Texture,
    Animation,
    Audio
}

public enum ExportStatus
{
    Error,
    Ready,
    Exported
}