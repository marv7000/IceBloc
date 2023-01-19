using IceBloc.Utility;
using System;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using static IceBloc.Frostbite.Animation.ChannelDofMapCache;

namespace IceBloc.Frostbite.Database;

public class Catalog : IDisposable
{
    public byte[] Key;
    public Dictionary<string, CatalogEntry> Entries = new();
    public Dictionary<int, BinaryReader> CasStreams = new();

    public Catalog(string path)
    {
        IO.DecryptAndCache(path);

        // Use the cached file for further operations.
        using var r = new BinaryReader(File.OpenRead($"Cache\\{Settings.CurrentGame}\\{Path.GetFileName(path)}"));
        switch (Settings.CurrentGame)
        {
            case Game.Battlefield3:
            case Game.Battlefield4:
                {
                    r.BaseStream.Position += 16;
                    while (r.BaseStream.Position < r.BaseStream.Length)
                    {
                        CatalogEntry catEntry = r.ReadCatalogEntry();
                        Entries[Convert.ToBase64String(catEntry.SHA)] = catEntry;
                    }
                    foreach (var file in Directory.EnumerateFiles(Settings.GamePath + "\\Data", "*.cas"))
                    {
                        // Get the index number of the cas archive by removing "cas_" and the file extension.
                        int index = int.Parse(Path.GetFileNameWithoutExtension(file).Replace("cas_", ""));
                        // Open the file corresponding to the cas archive index.
                        CasStreams.Add(index, new(File.OpenRead(file), Encoding.ASCII, true));
                    }
                }
                break;
            default:
                break;
        }
    }

    public CatalogEntry GetEntry(byte[] sha)
    {
        if (!Entries.TryGetValue(Convert.ToBase64String(sha), out var entry))
            throw new Exception($"SHA {Convert.ToBase64String(sha)} was not found in the Cas entry list.");
        return entry;
    }

    public byte[] Extract(byte[] sha, bool isBundle, InternalAssetType type)
    {
        // Throw an exception if we can't find a chunk with the given SHA.
        var entry = GetEntry(sha);

        bool compressed = false;

        switch (Settings.CurrentGame)
        {
            case Game.Battlefield3:
                if (type == InternalAssetType.RES)
                    compressed = true;
                else if (type == InternalAssetType.EBX)
                    compressed = false;
                else if (type == InternalAssetType.Chunk)
                    compressed = entry.IsCompressed;
                break;
        }

        switch (Settings.CurrentGame)
        {
            case Game.Battlefield3:
                {
                    BinaryReader r = CasStreams[entry.CasFileIndex];
                    r.BaseStream.Position = entry.Offset;

                    if (compressed)
                        return ZLibDecompress(r, entry.DataSize);
                    else
                        return r.ReadBytes(entry.DataSize);
                }
        }
        return null;
    }


    private byte[] ZLibDecompress(BinaryReader r, int size)
    {
        using var s = new MemoryStream();

        long startOffset = r.BaseStream.Position;

        while (r.BaseStream.Position < startOffset + size - 8)
        {
            int uSize = r.ReadInt32(true);
            int cSize = r.ReadInt32(true);

            using var memory = new MemoryStream(r.ReadBytes(cSize));
            using var deflator = new ZLibStream(memory, CompressionMode.Decompress);

            try
            {
                deflator.CopyTo(s);
            }
            catch
            {
                memory.CopyTo(s);
            }
        }

        return s.ToArray();
    }

    public byte[] CasChunkPayload(CatalogEntry entry)
    {
        return null;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var stream in CasStreams)
            {
                stream.Value.Close();
            }

            CasStreams.Clear();
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}