using IceBloc.Frostbite.Database;
using IceBloc.Utility;
using System.IO;
using System.Reflection;

namespace IceBloc.Frostbite;

public class IO
{
    public static DbObject ActiveDataBaseObject = null;
    public static Catalog ActiveCatalog = null;
    public static DbMetaData MetaData;
    public static Dictionary<string, AssetListItem> Assets = new();
    public static Game ActiveGame;
    public static Dictionary<Guid, byte[]> ChunkTranslations = new();

    /// <summary>
    /// Finds the given chunk and gets its contents.
    /// </summary>
    public static byte[] GetChunk(Guid streamingChunkId)
    {
        var sha = ChunkTranslations[streamingChunkId];
        return ActiveCatalog.Extract(sha);
    }

    /// <summary>
    /// Opens a Frostbite database file, tries to decrypt it and saves it to the cache.
    /// </summary>
    public static bool DecryptAndCache(string path)
    {
        if (!Path.Exists($"Cache\\{Settings.CurrentGame}\\{Path.GetFileName(path)}"))
        {
            using (var r = new BinaryReader(File.OpenRead(path)))
            {
                byte[] magic = r.ReadBytes(4);
                byte[] data;

                // Is XOR encrypted.
                if (magic.SequenceEqual(new byte[] { 0x00, 0xD1, 0xCE, 0x00 }) ||
                    magic.SequenceEqual(new byte[] { 0x00, 0xD1, 0xCE, 0x01 }))
                {
                    r.BaseStream.Position = 296; // Skip the signature.
                    var key = r.ReadBytes(260);
                    for (int i = 0; i < key.Length; i++)
                    {
                        key[i] ^= 0x7B; // XOR with 0x7B (Bytes 257, 258 and 259 are unused).
                    }
                    byte[] encryptedData = r.ReadUntilStreamEnd();
                    data = new byte[encryptedData.Length];
                    for (int i = 0; i < encryptedData.Length; i++)
                        data[i] = (byte)(key[i % 257] ^ encryptedData[i]);
                }
                // Is not XOR encrypted but has sequence + key.
                else if (magic.SequenceEqual(new byte[] { 0x00, 0xD1, 0xCE, 0x03 }))
                {
                    r.BaseStream.Position = 296; // Skip the signature.
                    r.ReadBytes(260); // Empty key.
                    data = r.ReadUntilStreamEnd();
                }
                // Not encrypted.
                else
                {
                    r.BaseStream.Position = 0; // Go back to the start of the file;
                    data = r.ReadUntilStreamEnd(); // Read data.
                }

                // Write the Catalog to file to cache it.
                Directory.CreateDirectory($"Cache\\{Settings.CurrentGame}");
                File.WriteAllBytes($"Cache\\{Settings.CurrentGame}\\{Path.GetFileName(path)}", data);
            }
            return false;
        }
        return true;
    }

    public static void LoadSbFile(string path)
    {
        try
        {
            ActiveDataBaseObject = DbObject.UnpackDbObject(path, out bool wasCached);
            var adboData = ActiveDataBaseObject.Data as List<DbObject>;
            if (adboData is not null)
            {
                foreach (var element in adboData)
                {
                    if (element.Name == "bundles")
                    {
                        foreach (DbObject asset in element.Data as List<DbObject>)
                        {
                            LoadDbObject(asset, false);
                        }
                    }
                    // If we have pure chunks.
                    else if (element.Name == "chunks")
                    {
                        LoadDbObject(element, true);
                    }
                }
            }

            if (wasCached)
                Console.WriteLine($"Loaded file \"{path}\" from Cache.");
            else
                Console.WriteLine($"Loaded file \"{path}\".");
        }
        catch
        {
            Console.WriteLine($"Tried to load an unsupported file \"{path}\", skipping...");
        }

    }

    public static void LoadDbObject(DbObject asset, bool isChunks)
    {
        if (!isChunks)
        {
            if (Settings.LoadMode == AssetLoadMode.All || Settings.LoadMode == AssetLoadMode.OnlyRes)
            {
                // If we have RES information, use it.
                if (!(asset.GetField("res") is null || (asset.GetField("res").Data as List<DbObject>).Count == 0))
                {
                    HandleResData(asset);
                }
            }
            if (Settings.LoadMode == AssetLoadMode.All || Settings.LoadMode == AssetLoadMode.OnlyEbx)
            {
                // If we have EBX information, use it.
                if (!(asset.GetField("ebx") is null || (asset.GetField("ebx").Data as List<DbObject>).Count == 0))
                {
                    HandleEbxData(asset);
                }
            }
            // If we have ChunkBundle information, use it.
            if (!(asset.GetField("chunks") is null || (asset.GetField("chunks").Data as List<DbObject>).Count == 0))
            {
                HandleChunkData(asset.GetField("chunks"));
            }
        }

        else
        {
            HandleChunkData(asset);
        }
    }

    public static void HandleEbxData(DbObject asset)
    {
        List<DbObject> ebxData = asset.GetField("ebx").Data as List<DbObject>;

        for (int i = 0; i < ebxData.Count; i++)
        {
            string idString = ebxData[i].GetField("name").Data as string;

            ResType type = ResType.EBX;
            object data = ebxData[i].GetField("size").Data;
            byte[] sha = ebxData[i].GetField("sha1").Data as byte[];

            // Check if we need to cast the read size to a long.
            long size = 0;
            if (data is int var) size = (int)data;
            else if (data is long var1) size = (long)data;

            var item = new AssetListItem(idString, type, InternalAssetType.EBX, size, ExportStatus.Ready, sha);

            // Check if we already have an asset with that name (Some RES are defined multiple times).
            item.GetHashCode();
            if (!Assets.ContainsKey(idString))
                Assets.Add(idString, item);
        }
    }

    public static void HandleResData(DbObject asset)
    {
        List<DbObject> resData = asset.GetField("res").Data as List<DbObject>;

        for (int i = 0; i < resData.Count; i++)
        {
            string idString = resData[i].GetField("name").Data as string;

            ResType type = (ResType)(int)resData[i].GetField("resType").Data;
            object data = resData[i].GetField("size").Data;
            byte[] sha = resData[i].GetField("sha1").Data as byte[];

            // Check if we need to cast the read size to a long.
            long size = 0;
            if (data is int var) size = (int)data;
            else if (data is long var1) size = (long)data;

            var item = new AssetListItem(idString, type, InternalAssetType.RES, size, ExportStatus.Ready, sha);

            // Check if we already have an asset with that name (Some RES are defined multiple times).
            item.GetHashCode();
            if (!Assets.ContainsKey(idString))
                Assets.Add(idString, item);
        }
    }

    public static void HandleChunkData(DbObject asset)
    {
        var chunks = asset.Data as List<DbObject>;
        for (int i = 0; i < chunks.Count; i++)
        {
            try
            {
                switch (Settings.CurrentGame)
                {
                    case Game.Battlefield3:
                        {
                            Guid chunkGuid = (Guid)chunks[i].GetField("id").Data;
                            var chunkSha = chunks[i].GetField("sha1");
                            if (chunkSha is not null)
                            {
                                // Add the chunk to the database. If we fail, it means that we have a duplicate chunk.
                                // In this case, check if the new one is larger. If yes, replace it.
                                if (!ChunkTranslations.TryAdd(chunkGuid, chunkSha.Data as byte[]))
                                {
                                    // TODO
                                }
                            }
                            else
                            {
                                // noncas bundle entry, cant read yet
                            }


                            break;
                        }
                        break;
                    case Game.Battlefield4:
                        {
                            Guid chunkGuid = (Guid)chunks[i].GetField("id").Data;
                            var chunkSha = chunks[i].GetField("sha1");
                            if (chunkSha is not null)
                            {
                                // Add the chunk to the database. If we fail, it means that we have a duplicate chunk.
                                // In this case, check if the new one is larger. If yes, replace it.
                                if (!ChunkTranslations.TryAdd(chunkGuid, chunkSha.Data as byte[]))
                                {
                                    // TODO
                                }
                            }
                            else
                            {
                                // noncas bundle entry, cant read yet
                            }


                            break;
                        }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }
    }

    public static string EnsurePath(string path, string extension)
    {
        if (!Path.Exists($"Output\\{Settings.CurrentGame}"))
            Directory.CreateDirectory($"Output\\{Settings.CurrentGame}");

        return $"Output\\{Settings.CurrentGame}\\" + Path.GetFileNameWithoutExtension(path) + extension;
    }

    public static void Dump(object o, string path)
    {
        FieldInfo[] info = o.GetType().GetFields();
        using var w = new StreamWriter(EnsurePath(path, "_dump.txt"));

        w.WriteLine(o.GetType().Name + ":");
        foreach (FieldInfo fi in info)
        {
            object value = fi.GetValue(o);
            w.Write($"\t{fi.FieldType.Name} {fi.Name}");
            if (value is Enum)
            {
                w.WriteLine($" = {(value as Enum).ToString("g")} ({(value as Enum).ToString("d")})");
            }
            else
            {
                if (value is Array)
                {
                    w.Write($"[{(value as Array).Length}] = ");
                    foreach (object val in (value as Array))
                    {
                        w.Write($"{val.ToString()}, ");
                    }
                    w.WriteLine("");
                }
                else
                {
                    w.WriteLine(" = " + value.ToString());
                }
            }
        }
    }

    public static async Task LoadGame()
    {
        // Clear existing DbObject selection.
        ActiveDataBaseObject = null;

        // Find the game name and set it.
        if (Settings.GamePath.Contains("Battlefield 3"))
            Settings.CurrentGame = Game.Battlefield3;
        else if (Settings.GamePath.Contains("Battlefield 4"))
            Settings.CurrentGame = Game.Battlefield4;
        else if (Settings.GamePath.Contains("BFH"))
            Settings.CurrentGame = Game.BattlefieldHardline;
        else
        {
            Console.WriteLine("Error: Tried to load an unsupported game!");
            return;
        }

        // Load the cascat.
        ActiveCatalog = new(Settings.GamePath + "\\Data\\cas.cat");
        Assets = new();

        // Get all file names in the Data\Win32\ dir.
        string[] sbFiles = Directory.GetFiles(Settings.GamePath + "\\Data\\Win32\\", "*", SearchOption.AllDirectories);

        for (int i = 0; i < sbFiles.Length; i++)
        {
            LoadSbFile(sbFiles[i]);
            Settings.Progress = ((double)i / (double)sbFiles.Length) * 100.0;
        }
    }
}
