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

    public byte[] MetaData;

    public AssetListItem(string name, ResType type, long size, ExportStatus status, byte[] sha)
    {
        Name = name;
        Type = type;
        Size = size;
        Status = status;
        MetaData = sha;
    }


    public void Export()
    {
        Console.WriteLine($"Exporting {Name}!");

        string path = $"Output\\{Name}.{Type}";
        Directory.CreateDirectory(Path.GetDirectoryName(path)); // Make sure the output directory exists.
        var entry = MainWindow.ActiveCatalog.GetEntry(MetaData);
        File.WriteAllBytes(path, MainWindow.ActiveCatalog.Extract(MetaData));

        Status = ExportStatus.Exported;
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