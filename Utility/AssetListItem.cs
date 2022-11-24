using IceBloc.Frostbite2;
using System;
using System.Collections.Generic;
using System.IO;

namespace IceBloc.Utility;

public class AssetListItem
{
    /// <summary>
    /// 
    /// </summary>
    public string Name { get; set; }
    public ResType Type { get; set; }
    public long Size { get; set; }
    public ExportStatus Status { get; set; }

    public List<MetaDataObject> MetaData;

    public AssetListItem(string name, ResType type, long size, ExportStatus status, List<MetaDataObject> mdo)
    {
        Name = name;
        Type = type;
        Size = size;
        Status = status;
        MetaData = mdo;
    }

    public void Export()
    {
        try
        {
            Console.WriteLine("Exporting Unknown");
            foreach (var meta in MetaData)
            {
                string path = $"Output\\{Name}";
                Directory.CreateDirectory(path);
                var entry = MainWindow.ActiveCatalog.GetEntry(meta.SHA);
                File.WriteAllBytes($"{path}\\{meta.GUID}.{Type}", MainWindow.ActiveCatalog.Extract(meta.SHA));
            }
            Status = ExportStatus.Exported;
        }
        catch
        {
            Status = ExportStatus.Error;
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