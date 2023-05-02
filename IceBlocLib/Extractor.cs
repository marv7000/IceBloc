using IceBlocLib.InternalFormats;
using IceBlocLib.Utility;
using System.Linq;

namespace IceBlocLib;

public static class Extractor
{
    public static void LinkEbx(this AssetListItem assetListItem)
    {
        if (assetListItem.Type == "EBX")
        {
            switch (Settings.CurrentGame)
            {
                case Game.Battlefield3:
                    {
                        using var stream = new MemoryStream(Frostbite2.IO.ActiveCatalog.Extract(assetListItem.MetaData, true, InternalAssetType.EBX));
                        var dbx = new Frostbite2.Dbx(stream);
                        assetListItem.Type = dbx.PrimType;
                        Frostbite2.Ebx.LinkTargets.TryAdd(dbx.FileGuid, assetListItem.MetaData);
                    } break;
            }
        }
    }

    public static async Task LoadGame()
    {
        if (Settings.GamePath.Contains("Battlefield 3"))
        {
            Settings.CurrentGame = Game.Battlefield3;
            Settings.IOClass = new Frostbite2.IO();
            Frostbite2.IO.LoadGame();
        }
    }

    public static void Export(this AssetListItem assetListItem)
    {
        string path = $"Output\\{Settings.CurrentGame}\\{assetListItem.Name}";
        Directory.CreateDirectory(Path.GetDirectoryName(path)); // Make sure the output directory exists.

        byte[] data = null;
        switch (Settings.CurrentGame)
        {
            case Game.Battlefield3:
                data = Frostbite2.IO.ActiveCatalog.Extract(assetListItem.MetaData, true, assetListItem.AssetType); break;
        }

        // If the user wants to export the raw RES.
        if (true)
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
                switch (assetListItem.Type)
                {
                    case "DxTexture":
                        {
                            InternalTexture output = new();
                            switch (Settings.CurrentGame)
                            {
                                case Game.Battlefield3:
                                    output = Frostbite2.Textures.DxTexture.ConvertToInternal(stream); break;
                            }
                            Settings.CurrentTextureExporter.Export(output, path);
                        } break;
                    case "MeshSet":
                        {
                            List<InternalMesh> output = new();
                            switch (Settings.CurrentGame)
                            {
                                case Game.Battlefield3:
                                    output = Frostbite2.Meshes.MeshSet.ConvertToInternal(stream); break;
                            }
                            if (AssetListItem.LastSkeleton is null)
                            {
                                for (int i = 0; i < output.Count; i++)
                                {
                                    Settings.CurrentModelExporter.Export(output[i], path + $"_{output[i].Name}");
                                }
                            }
                            else
                                Settings.CurrentModelExporter.Export(output, AssetListItem.LastSkeleton, path);
                        } break;
                    case "AssetBank":
                        {
                            List<InternalAnimation> s = Frostbite2.Misc.AntPackageAsset.ConvertToInternal(stream);
                            for (int i = 0; i < s.Count; i++)
                            {
                                Settings.CurrentAnimationExporter.Export(s[i], AssetListItem.LastSkeleton, path);
                            }
                        } break;
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
                        var s = Frostbite2.Misc.SkeletonAsset.ConvertToInternal(in dbx);
                        AssetListItem.LastSkeleton = s;
                        Settings.CurrentSkeletonExporter.Export(s, path);
                    }
                    else if (assetListItem.Type == "SoundWaveAsset")
                    {
                        var s = Frostbite2.Misc.SoundWaveAsset.ConvertToInternal(in dbx);
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
                        List<InternalAnimation> s = Frostbite2.Misc.AntPackageAsset.ConvertToInternal(in dbx);
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
