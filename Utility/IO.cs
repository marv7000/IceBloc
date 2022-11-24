using System;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Linq;
using IceBloc.InternalFormats;
using IceBloc.Frostbite2;

namespace IceBloc.Utility;

/// <summary>
/// Handles in- and output for all assets.
/// </summary>
public class IO
{
    /// <summary>
    /// Serializes an object to XML and saves it at the specified path.
    /// </summary>
    /// <param name="obj">Object to serialize.</param>
    /// <param name="filePath">Path to save the XML at.</param>
    public static void SaveXML<T>(T obj, string filePath)
    {
        var serializer = new XmlSerializer(obj.GetType());

        using (var writer = new StreamWriter(filePath))
        using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true, IndentChars = "    ", OmitXmlDeclaration = true }))
        {
            serializer.Serialize(xmlWriter, obj);
        }
    }

    /// <summary>
    /// Finds the given chunk and gets its contents.
    /// </summary>
    public static byte[] GetChunk(Guid streamingChunkId)
    {
        var sha = MainWindow.ChunkTranslations[streamingChunkId];
        return MainWindow.ActiveCatalog.Extract(sha);
    }

    /// <summary>
    /// Opens a Frostbite database file, tries to decrypt it and saves it to the cache.
    /// </summary>
    public static void DecryptAndCache(string path)
    {
        using (var r = new BinaryReader(File.OpenRead(path)))
        {
            byte[] magic = r.ReadBytes(4);
            byte[] data;

            // Is XOR encrypted.
            if (magic.SequenceEqual(new byte[] { 0x00, 0xD1, 0xCE, 0x00 }) || magic.SequenceEqual(new byte[] { 0x00, 0xD1, 0xCE, 0x01 }))
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
            // Is not XOR encrypted.
            else
            {
                r.BaseStream.Position = 0; // Go back to the start of the file;
                data = r.ReadUntilStreamEnd(); // Read data.
            }

            // Write the Catalog to file to cache it.
            Directory.CreateDirectory($"Cache\\{Settings.CurrentGame}");
            File.WriteAllBytes($"Cache\\{Settings.CurrentGame}\\{Path.GetFileName(path)}", data);
        }
    }

    #region Textures


    #endregion
}
