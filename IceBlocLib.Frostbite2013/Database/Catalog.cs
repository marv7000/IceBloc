using IceBlocLib.Frostbite;
using IceBlocLib.Utility;
using System.IO.Compression;
using System.Text;

namespace IceBlocLib.Frostbite2013.Database;

public class Catalog : IDisposable
{
    public Dictionary<string, CatalogEntry> Entries = new();
    public Dictionary<int, BinaryReader> CasStreams = new();

    public Catalog(string path)
    {
        IO.DecryptAndCache(path);

        // Use the cached file for further operations.
        using var r = new BinaryReader(File.OpenRead($"Cache\\{Settings.CurrentGame}\\{Path.GetFileName(path)}"));
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

        switch (Settings.CurrentGame)
        {
            case Game.Battlefield4:
                {
                    BinaryReader r = CasStreams[entry.CasFileIndex];
                    r.BaseStream.Position = entry.Offset;

                    uint num1 = r.ReadUInt32(true);
                    uint num2 = r.ReadUInt32(true);
                    var dictFlag = num1 & 0xFF000000;
                    var uncompressedSize = num1 & 0x00FFFFFF;
                    var comType = (num2 & 0xFF000000) >> 24;
                    var typeFlag = (num2 & 0x00F00000) >> 20;
                    var compressedSize = num2 & 0x000FFFFF;

                    switch (comType)
                    {
                        case 0x00:
                            return r.ReadBytes((int)compressedSize);
                        case 0x02:
                            return ZLibDecompress(r, (int)compressedSize);
                        case 0x09:
                            return LZ4Decompress(r, (int)compressedSize, (int)uncompressedSize);
                        case 0x0F:
                            return ZStdDecompress(r, (int)compressedSize, (int)uncompressedSize);
                        case 0x15:
                            return OodleDecompress(r, (int)compressedSize, (int)uncompressedSize);
                    }
                } break;
        }
        return null;
    }

    private byte[] OodleDecompress(BinaryReader r, int compressedSize, int uncompressedSize)
    {
        throw new NotImplementedException();
    }

    private byte[] ZStdDecompress(BinaryReader r, int compressedSize, int uncompressedSize)
    {
        throw new NotImplementedException();
    }

    private unsafe byte[] LZ4Decompress(BinaryReader r, int compressedSize, int uncompressedSize)
    {
        Memory<byte> srcBuf = r.ReadBytes(compressedSize);
        Memory<byte> destBuf = new byte[uncompressedSize];

        nint srcPtr = (nint)srcBuf.Pin().Pointer;
        nint destPtr = (nint)destBuf.Pin().Pointer;

        Compression.Lz4DecompressSafePartial(srcPtr, destPtr, compressedSize, uncompressedSize, uncompressedSize);

        return destBuf.ToArray();
    }

    private byte[] ZLibDecompress(BinaryReader r, int size)
    {
        using var s = new MemoryStream();

        long startOffset = r.BaseStream.Position;

        while (r.BaseStream.Position < startOffset + size - 8)
        {
            int uSize = r.ReadInt32(true);
            //int cSize = r.ReadInt32(true);
            int cSize = size;

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