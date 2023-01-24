﻿using IceBlocLib.Export;
using IceBlocLib.Frostbite;
using IceBlocLib.Frostbite2;
using IceBlocLib.Frostbite2.Meshes;
using IceBlocLib.Frostbite2.Misc;
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
            using var stream = new MemoryStream(IO.ActiveCatalog.Extract(sha, true, InternalAssetType.EBX));
            var dbx = new Dbx(stream);
            int n = dbx.Instances[dbx.PrimaryInstanceGuid].Desc.Name;
            Type = Ebx.StringTable[n];
            Ebx.LinkTargets.TryAdd(dbx.FileGuid, sha);
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
                    var s = SkeletonAsset.ConvertToInternal(in dbx);
                    Settings.CurrentSkeletonExporter.Export(s, path);
                }
                else if (Type == "SoundWaveAsset")
                {
                    var s = SoundWaveAsset.ConvertToInternal(in dbx);
                    if (s.Count > 1)
                    {
                        for (int i = 0; i < s.Count; i++)
                            Settings.CurrentSoundExporter.Export(s[i], path + $"_var{i}");
                    }
                    else
                        Settings.CurrentSoundExporter.Export(s[0], path);
                }
                else if (Type == "AntPackageAsset")
                {
                    List<InternalAnimation> s = AntPackageAsset.ConvertToInternal(in dbx);
                    for (int i = 0; i < s.Count; i++)
                    {
                        Settings.CurrentAnimationExporter.Export(s[i], path);
                    }
                }
                else
                {
                    dbx.Dump(path + ".ebx");
                }
            }

            // RES Export
            else if (AssetType == InternalAssetType.RES)
            {
                if (Type == "DxTexture")
                {
                    InternalTexture output = new();
                    switch (Settings.CurrentGame)
                    {
                        case Game.Battlefield3:
                            output = Frostbite2.Textures.DxTexture.ConvertToInternal(stream); break;
                        case Game.Battlefield4:
                            output = Frostbite2013.Textures.DxTexture.ConvertToInternal(stream); break;
                    }
                    Settings.CurrentTextureExporter.Export(output, path);
                }
                else if (Type == "MeshSet")
                {
                    List<InternalMesh> output = MeshSet.ConvertToInternal(stream);
                    for (int i = 0; i < output.Count; i++)
                    {
                        Settings.CurrentModelExporter.Export(output[i], path + $"_var{i}");
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