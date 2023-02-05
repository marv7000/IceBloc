using IceBloc.Utility;
using IceBlocLib.Frostbite.Database;
using IceBlocLib.Utility;
using System.Reflection;
using System.Text.RegularExpressions;

namespace IceBlocLib.Frostbite;

public class IO
{
    public static Dictionary<string, bool> DataBaseObjects = new();
    public static DbObject ActiveDataBaseObject;
    public static Catalog ActiveCatalog = null;
    public static DbMetaData MetaData;
    public static Dictionary<(string, InternalAssetType), AssetListItem> Assets = new();
    public static Game ActiveGame;
    public static Dictionary<Guid, (CatalogEntry Entry, bool IsBundle, bool IsCas)> ChunkTranslations = new();

    /// <summary>
    /// Finds the given chunk and gets its contents.
    /// </summary>
    public static byte[] GetChunk(Guid streamingChunkId)
    {
        var t = ChunkTranslations[streamingChunkId];
        return ActiveCatalog.Extract(t.Entry.SHA, t.IsBundle, InternalAssetType.Chunk);
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

    public static void LoadSbFile(string path, bool isToc)
    {
        try
        {
            ActiveDataBaseObject = DbObject.UnpackDbObject(path, out bool wasCached);
            var adboData = ActiveDataBaseObject.Data as List<DbObject>;
            if (adboData is not null)
            {
                bool isCas = false;
                // Check for cas.
                if (isToc)
                {
                    foreach (var element in adboData)
                    {
                        if (element.Name == "cas")
                            isCas = (bool)element.Data;
                    }

                    DataBaseObjects.Add(Path.GetFileNameWithoutExtension(path), isCas);
                }

                isCas = DataBaseObjects[Path.GetFileNameWithoutExtension(path)];

                foreach (var element in adboData)
                {
                    if (element.Name == "bundles")
                    {
                        foreach (DbObject asset in element.Data as List<DbObject>)
                        {
                            LoadDbObject(asset, false, isCas);
                        }
                    }
                    // If we have pure chunks.
                    else if (element.Name == "chunks")
                    {
                        LoadDbObject(element, true, isCas);
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

    public static void LoadNonCasFile(string path)
    {
        // TODO
    }

    public static void LoadDbObject(DbObject asset, bool isChunks, bool isCas)
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
                HandleChunkData(asset.GetField("chunks"), isChunks, isCas);
            }
        }

        else
        {
            HandleChunkData(asset, isChunks, isCas);
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

            var item = new AssetListItem(idString, type.ToString(), InternalAssetType.EBX, size, sha);

            // Check if we already have an asset with that name (Some EBX are defined multiple times).
            if (!Assets.ContainsKey((idString, InternalAssetType.EBX)))
                Assets.Add((idString, InternalAssetType.EBX), item);
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

            var item = new AssetListItem(idString, type.ToString(), InternalAssetType.RES, size, sha);

            // Check if we already have an asset with that name (Some RES are defined multiple times).
            item.GetHashCode();
            if (!Assets.ContainsKey((idString, InternalAssetType.RES)))
                Assets.Add((idString, InternalAssetType.RES), item);
        }
    }

    public static void HandleChunkData(DbObject asset, bool isChunk, bool isCas)
    {
        var chunks = asset.Data as List<DbObject>;
        for (int i = 0; i < chunks.Count; i++)
        {
            try
            {
                switch (Settings.CurrentGame)
                {
                    case Game.Battlefield3:
                    case Game.Battlefield4:
                        {
                            Guid chunkGuid = (Guid)chunks[i].GetField("id").Data;

                            if (isCas)
                            {
                                var chunkSha = chunks[i].GetField("sha1");
                                if (chunkSha is not null)
                                {
                                    // Add the chunk to the database. If we fail, it means that we have a duplicate chunk.
                                    // In this case, check if the new one is larger. If yes, replace it.
                                    CatalogEntry e = new();
                                    e.SHA = chunkSha.Data as byte[];
                                    if (!ChunkTranslations.TryAdd(chunkGuid, (e, isChunk, isCas)))
                                    {
                                        if (ActiveCatalog.GetEntry(chunkSha.Data as byte[]).DataSize >
                                            ChunkTranslations[chunkGuid].Entry.DataSize)
                                        {
                                            ChunkTranslations[chunkGuid] = (e, isChunk, isCas);
                                        }
                                    }
                                    if ((chunkGuid.ToByteArray()[15] & 1) == 1)
                                    {
                                        ActiveCatalog.Entries[Convert.ToBase64String(chunkSha.Data as byte[])].IsCompressed = true;
                                    }
                                }
                            }
                            else
                            {
                                var chunkOffset = chunks[i].GetField("offset");
                                var chunkSize = chunks[i].GetField("size");
                                if (chunkOffset is not null)
                                {
                                    // Add the chunk to the database. If we fail, it means that we have a duplicate chunk.
                                    // In this case, check if the new one is larger. If yes, replace it.
                                    CatalogEntry e = new();
                                    e.Offset = (uint)(long)chunkOffset.Data;
                                    e.DataSize = (int)chunkSize.Data;
                                    ChunkTranslations.TryAdd(chunkGuid, (e, isChunk, isCas));
                                }
                            }
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ChunkError: " + e.Message);
            }
        }
    }

    /// <summary>
    /// Makes sure the given path exists.
    /// </summary>
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
                    foreach (object val in value as Array)
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
        DataBaseObjects = new();

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
        string[] files = Directory.GetFiles(Settings.GamePath + "\\Data\\Win32\\", "*", SearchOption.AllDirectories);
        int i = 0;
        // First load all .toc files.
        foreach (var toc in files)
        {
            if (toc.Contains(".toc"))
            {
                LoadSbFile(toc, true);
                i++;
                Settings.Progress = i / (double)files.Length * 100.0;
            }
        }
        // Then, load all .sb files.
        foreach (var sb in files)
        {
            if (sb.Contains(".sb"))
            {
                // No noncas yet! If the toc says the sb is noncas then skip TODO
                if (DataBaseObjects[Path.GetFileNameWithoutExtension(sb)])
                    LoadSbFile(sb, false);
                else
                    LoadNonCasFile(sb);
                i++;
                Settings.Progress = i / (double)files.Length * 100.0;
            }
        }

    }

    /// <summary>
    /// Same as <see cref="string.Split(char[]?)"/>, but taking spaces into account when they're inside quotes.
    /// </summary>
    public static string[] SplitLiteral(string input)
    {
        return Regex.Matches(input, @"[\""].+?[\""]|[^ ]+").Cast<Match>().Select(m => m.Value).ToArray();
    }

    public static int GetIndentCount(string input)
    {
        return input.Count(ch => ch == '\t');
    }

    public static bool GetEbxCachedName(byte[] sha, out string s)
    {
        var path = $"Cache\\{Settings.CurrentGame}\\EbxNameCache.txt";
        if (Path.Exists(path))
        {
            string[] lines = File.ReadAllLines(path);
            string base64 = Convert.ToBase64String(sha);
            foreach (var l in lines)
            {
                if (l.Contains(base64))
                {
                    s = l.Split(";")[1];
                    return true;
                }
            }
        }
        s = string.Empty;
        return false;
    }

    public static void AppendEbxCachedName(byte[] sha, string v)
    {
        var path = $"Cache\\{Settings.CurrentGame}\\EbxNameCache.txt";
        if (!Path.Exists(path))
            File.WriteAllText(path, "");
        File.AppendAllText(path, $"{Convert.ToBase64String(sha)};{v}\n");
    }
}
