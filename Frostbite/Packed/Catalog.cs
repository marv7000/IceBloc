using IceBloc.Utility;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace IceBloc.Frostbite.Packed;

public class Catalog : IDisposable
{
    public Dictionary<string, CatalogEntry> Entries = new();

    public Dictionary<int, BinaryReader> CasStreams = new();

    public Catalog(BinaryReader catReader)
    {
        switch (Settings.CurrentGame)
        {
            case Game.Battlefield3:
                // Decrypt the cat archive.
                MemoryStream stream = UnXor(catReader);

                break;

            case Game.Battlefield4:
                // BF4 doesn't have encrypted toc or cascat archives.
                catReader.BaseStream.Position += 16;
                while (catReader.BaseStream.Position < catReader.BaseStream.Length)
                {
                    CatalogEntry catEntry = new(2, catReader);
                    Entries[Convert.ToBase64String(catEntry.SHA)] = catEntry;
                }
                foreach (var file in Directory.EnumerateFiles(Settings.GamePath + "\\Data", "*.cas"))
                {
                    // Get the index number of the cas archive by removing "cas_" and the file extension.
                    int index = int.Parse(Path.GetFileNameWithoutExtension(file).Replace("cas_", ""));
                    // Open the file corresponding to the cas archive index.
                    CasStreams.Add(index, new(File.OpenRead(file)));
                }
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Takes an encrypted <see cref="DbObject"/> and decrypts it.
    /// </summary>
    public static MemoryStream UnXor(BinaryReader r)
    {
        byte[] magic = r.ReadBytes(4);
        byte[] data;
        
        // Is XOR encrypted.
        if (magic.SequenceEqual(new byte[] { 0x00, 0xD1, 0xCE, 0x00 }) || magic.SequenceEqual(new byte[] { 0x00, 0xD1, 0xCE, 0x01 }))
        {
            r.BaseStream.Position += 296; // Skip the signature.
            var key = r.ReadBytes(260);
            for (int i = 0; i <= key.Length; i++)
            {
                key[i] ^= 0x7B; // XOR with 0x7B (Bytes 257, 258 and 259 are unused).
            }
            byte[] encryptedData = r.ReadUntilStreamEnd();
            data = new byte[encryptedData.Length];
            for (int i = 0; i <= encryptedData.Length; i++)
                data[i] = (byte)(key[i % 257] ^ encryptedData[i]);
        }
        // Is not XOR encrypted.
        else
        {
            r.BaseStream.Position = 0; // Go back to the start of the file;
            data = r.ReadUntilStreamEnd(); // Read data.
        }

        return new MemoryStream(data);
    }

    public CatalogEntry GetEntry(byte[] sha)
    {
        if (!Entries.TryGetValue(Convert.ToBase64String(sha), out var entry))
            throw new Exception();
        return entry;
    }

    public byte[] Extract(byte[] sha, bool isCompressed)
    {
        // Throw an exception if we can't find a chunk with the given SHA.
        if (!Entries.TryGetValue(Convert.ToBase64String(sha), out var entry)) throw new KeyNotFoundException($"Could not get a value for {Encoding.ASCII.GetString(sha)}!");
        BinaryReader r = CasStreams[entry.CasFileIndex];
        r.BaseStream.Position = entry.Offset;

        if (!isCompressed)
        {

        }
        else
        {
            MemoryStream output = new(entry.DataSize);
            long end = r.BaseStream.Position + entry.DataSize;

            while (r.BaseStream.Position < end)
            {
                int uSize = r.ReadInt32();
                int cSize = r.ReadInt32();

                uSize = BinaryPrimitives.ReverseEndianness(uSize);
                cSize = BinaryPrimitives.ReverseEndianness(cSize);

                using (var memory = new MemoryStream(r.ReadBytes(cSize)))
                {
                    memory.Position += 2;

                    using (var deflator = new DeflateStream(memory, CompressionMode.Decompress))
                    {
                        deflator.CopyTo(output);
                    }
                }
            }

            return output.ToArray();
        }

        return null;
    }

    public static Stream LoadDBFile(string fileName)
    {
        using var reader = new BinaryReader(File.OpenRead(fileName));

        var magicNumber = reader.ReadUInt32();
        var reserved = reader.ReadUInt32();
        var signature = reader.ReadBytes(288);
        var sequence = reader.ReadBytes(260);
        var data = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));

        if (magicNumber == 0x1CED100 || magicNumber == 0xCED100)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(data[i] ^ 0x7B ^ sequence[i % 0x101]);
            }
        }

        File.WriteAllBytes(Path.GetFileName(fileName), data);
        return new MemoryStream(data);
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