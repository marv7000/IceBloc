using System;
using System.Collections.Generic;
using System.IO;

namespace IceBloc.Utility;

public class AssetListItem
{
    public string Name { get; set; }
    public AssetType Type { get; set; }
    public long Size { get; set; }
    public ExportStatus Status { get; set; }
    public string Remarks { get; set; }

    public List<MetaDataObject> MetaData;

    public AssetListItem(string name, AssetType type, long size, ExportStatus status, string remarks, List<MetaDataObject> mdo)
    {
        Name = name;
        Type = type;
        Size = size;
        Status = status;
        Remarks = remarks;
        MetaData = mdo;
    }

    public void Export()
    {
        try
        {
            switch (Type)
            {
                case AssetType.Unknown:
                case AssetType.Chunk:
                    foreach (var meta in MetaData)
                    {
                        string path = $"Output\\{Name}";
                        Directory.CreateDirectory(path);
                        var entry = MainWindow.ActiveCatalog.GetEntry(meta.SHA);
                        File.WriteAllBytes($"{path}\\{meta.GUID}.chunk", MainWindow.ActiveCatalog.Extract(meta.SHA, true));
                    }
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

public class MetaDataObject
{
    public byte[] SHA;
    public Guid GUID;
    public long Size;

    public MetaDataObject(byte[] sHA, Guid gUID, long size)
    {
        SHA = sHA;
        GUID = gUID;
        Size = size;
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