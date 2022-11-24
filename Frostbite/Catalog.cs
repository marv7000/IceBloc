using IceBloc.Utility;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace IceBloc.Frostbite;

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
                break;
            default:
                break;
        }
    }

    public CatalogEntry GetEntry(byte[] sha)
    {
        if (!Entries.TryGetValue(Convert.ToBase64String(sha), out var entry))
            throw new Exception("SHA was not found in the Cas entry list.");
        return entry;
    }

    public byte[] Extract(byte[] sha)
    {
        // Throw an exception if we can't find a chunk with the given SHA.
        if (!Entries.TryGetValue(Convert.ToBase64String(sha), out var entry)) 
            throw new KeyNotFoundException($"Could not get a value for {Encoding.ASCII.GetString(sha)}!");

        BinaryReader r = CasStreams[entry.CasFileIndex];
        r.BaseStream.Position = entry.Offset;

        using MemoryStream output = new(entry.DataSize);

        long end = r.BaseStream.Position + entry.DataSize;

        while (r.BaseStream.Position < end)
        {
            int uSize = r.ReadInt32();
            int cSize = r.ReadInt32();

            uSize = BinaryPrimitives.ReverseEndianness(uSize);
            cSize = BinaryPrimitives.ReverseEndianness(cSize);

            using (var memory = new MemoryStream(r.ReadBytes(cSize)))
            {
                //memory.Position += 2;
                try
                {
                    using (var deflator = new ZLibStream(memory, CompressionMode.Decompress))
                    {
                        deflator.CopyTo(output);
                    }
                }
                catch
                {
                    memory.CopyTo(output);
                }
            }
        }
        return output.ToArray();
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