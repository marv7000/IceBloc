using IceBlocLib.Frostbite;
using IceBlocLib.Frostbite2;
using IceBlocLib.Frostbite2.Meshes;
using IceBlocLib.Frostbite2.Misc;
using IceBlocLib.InternalFormats;
using IceBlocLib.Utility;

namespace IceBlocLib;

public static class Extractor
{
    public static void LinkEbx(this AssetListItem assetListItem)
    {
        if (assetListItem.Type == "EBX")
        {
            using var stream = new MemoryStream(IO.ActiveCatalog.Extract(assetListItem.MetaData, true, InternalAssetType.EBX));
            var dbx = new Dbx(stream);
            assetListItem.Type = dbx.PrimType;
            Ebx.LinkTargets.TryAdd(dbx.FileGuid, assetListItem.MetaData);
        }
    }

    public static void Export(this AssetListItem assetListItem)
    {
        string path = $"Output\\{Settings.CurrentGame}\\{assetListItem.Name}";
        Directory.CreateDirectory(Path.GetDirectoryName(path)); // Make sure the output directory exists.

        byte[] data = IO.ActiveCatalog.Extract(assetListItem.MetaData, true, assetListItem.AssetType);

        // If the user wants to export the raw RES.
        if (Settings.ExportRaw)
        {
            File.WriteAllBytes(path + ("_raw." + assetListItem.Type), data);
        }

        if (Settings.ExportConverted)
        {
            using var stream = new MemoryStream(data);
            if (assetListItem.AssetType == InternalAssetType.EBX)
            {
                ExportInterpretedEBX(assetListItem, stream, path);
            }

            // RES Export
            else if (assetListItem.AssetType == InternalAssetType.RES)
            {
                if (assetListItem.Type == "DxTexture")
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
                else if (assetListItem.Type == "MeshSet")
                {
                    List<InternalMesh> output = MeshSet.ConvertToInternal(stream);
                    for (int i = 0; i < output.Count; i++)
                    {
                        if (AssetListItem.LastSkeleton is null)
                            Settings.CurrentModelExporter.Export(output[i], path + $"_{output[i].Name}");
                        else
                            Settings.CurrentModelExporter.Export(output[i], AssetListItem.LastSkeleton, path + $"_{output[i].Name}");
                    }
                }
                else if (assetListItem.Type == "AssetBank")
                {
                    List<InternalAnimation> s = AntPackageAsset.ConvertToInternal(stream);
                    for (int i = 0; i < s.Count; i++)
                    {
                        Settings.CurrentAnimationExporter.Export(s[i], AssetListItem.LastSkeleton, path);
                    }
                }
            }
        }
        Console.WriteLine($"Exported {assetListItem.Name}...");
    }

    public static void ExportInterpretedEBX(this AssetListItem assetListItem, MemoryStream stream, string path)
    {
        switch (Settings.CurrentGame)
        {
            case Game.Battlefield3:
                {
                    var dbx = new Frostbite2.Dbx(stream);
                    if (assetListItem.Type == "SkeletonAsset")
                    {
                        var s = SkeletonAsset.ConvertToInternal(in dbx);
                        AssetListItem.LastSkeleton = s;
                        Settings.CurrentSkeletonExporter.Export(s, path);
                    }
                    else if (assetListItem.Type == "SoundWaveAsset")
                    {
                        var s = SoundWaveAsset.ConvertToInternal(in dbx);
                        if (s.Count > 1)
                        {
                            for (int i = 0; i < s.Count; i++)
                                Settings.CurrentSoundExporter.Export(s[i], path + $"_v{i}");
                        }
                        else
                            Settings.CurrentSoundExporter.Export(s[0], path);
                    }
                    else if (assetListItem.Type == "AntPackageAsset")
                    {
                        List<InternalAnimation> s = AntPackageAsset.ConvertToInternal(in dbx);
                        for (int i = 0; i < s.Count; i++)
                        {
                            Settings.CurrentAnimationExporter.Export(s[i], AssetListItem.LastSkeleton, path);
                        }
                    }
                    else
                    {
                        dbx.Dump(path + ".ebx");
                    }
                }
                break;
        }

    }
}
