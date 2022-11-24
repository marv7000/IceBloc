﻿using IceBloc.Frostbite2;
using IceBloc.InternalFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime;

namespace IceBloc.Utility;

public class AssetListItem
{
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

        string path = $"Output\\{Name}";
        Directory.CreateDirectory(Path.GetDirectoryName(path)); // Make sure the output directory exists.
        byte[] data = MainWindow.ActiveCatalog.Extract(MetaData);

        // If the user wants to export the raw RES.
        if (Settings.ExportRaw)
        {
            File.WriteAllBytes(path + "." + Type, data);
        }
        if (Settings.ExportConverted)
        {
            switch (this.Type)
            {
                case ResType.DxTexture:
                    {
                        using var stream = new MemoryStream(data);
                        InternalTexture output = DxTexture.ConvertToInternal(stream);
                        Settings.CurrentTextureExporter.Export(output, path);
                        break;
                    }
                case ResType.MeshSet:
                    {
                        using var stream = new MemoryStream(data);
                        List<InternalMesh> output = MeshSet.ConvertToInternal(stream);
                        for (int i = 0; i < output.Count; i++)
                        {
                            Settings.CurrentModelExporter.Export(output[i], path + i.ToString());
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        MainWindow.WriteUIOutputLine($"Exported {Name}...");
        Status = ExportStatus.Exported;
    }
}

/// <summary>
/// Provides a mechanism to connect Guid and SHA values.
/// </summary>
public struct MetaDataObject
{
    public byte[] SHA;
    public Guid GUID;

    public MetaDataObject(byte[] sha, Guid guid)
    {
        SHA = sha;
        GUID = guid;
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