using System;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

using IceBloc.Frostbite.Texture;
using IceBloc.Frostbite.Mesh;

namespace IceBloc.Utility;

/// <summary>
/// Handles in- and output for all assets.
/// </summary>
public class IO
{
    /// <summary>
    /// Gets the asset data of an <see cref="FrostbiteAsset"/>.
    /// </summary>
    /// <returns>A stream containing the requested asset.</returns>
    public static byte[] GetAssetFromGuid(Guid guid)
    {
        try
        {
            string path = Settings.GamePath + guid.ToString() + ".chunk";

            return File.ReadAllBytes(path);
        }
        catch
        {
            // If we encounter a failure as big as Harry.
            throw new FileNotFoundException("Chunk was not found in the specified dump! Make sure the game has been dumped correctly.");
        }
    }

    /// <summary>
    /// Serializes an object to XML and saves it at the specified path.
    /// </summary>
    /// <param name="obj">Object to serialize.</param>
    /// <param name="filePath">Path to save the XML at.</param>
    public static void Save<T>(T obj, string filePath)
    {
        var serializer = new XmlSerializer(obj.GetType());

        using (var writer = new StreamWriter(filePath))
        using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true, IndentChars = "    ", OmitXmlDeclaration = true }))
        {
            serializer.Serialize(xmlWriter, obj);
        }
    }

    public static void Export<T>(T obj, string filePath)
    {
        switch (obj)
        {
            case DxTexture:
                ExportTextureAsDDS(obj as DxTexture, filePath);
                break;
            default:
                break;
        }
    }

    #region Textures

    private static void ExportTextureAsDDS(DxTexture texture, string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Create);
        using var writer = new BinaryWriter(stream);
        {
            // DDS Header
            writer.Write(new char[] { 'D', 'D', 'S', ' ' });
            writer.Write(124U);
            writer.Write(0x07100AU); // Flags
            writer.Write((uint)texture.Height);
            writer.Write((uint)texture.Width);
            writer.Write((uint)Math.Pow(texture.Width * texture.Height >> 1, 2));
            writer.Write((uint)texture.Depth);
            writer.Write((uint)texture.MipmapCount);
            writer.Write(new byte[44]);
            writer.Write(32U);
            writer.Write(0x00U);
            writer.Write(texture.Format.ToString().ToCharArray());
            writer.Write(new byte[5 * 4]);
            writer.Write(new byte[16 * 4]);
            writer.Write(0U);
            // Pixel data
            writer.Write(texture.PixelData);
        }
    }

    #endregion
}
