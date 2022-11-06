using IceBloc.InternalFormats;
using System.IO;
using System;

namespace IceBloc.Export;

public class TextureExporterDDS : ITextureExporter
{
    public void Export(InternalTexture texture, string path)
    {
        using var w = new BinaryWriter(File.Create(path));
        {
            w.Write(new char[] { 'D', 'D', 'S', ' ' }); // DDS Header
            w.Write(124U);
            w.Write(0x07100AU); // Flags
            w.Write((uint)texture.Height);
            w.Write((uint)texture.Width);
            w.Write((uint)Math.Pow(texture.Width * texture.Height >> 1, 2));
            w.Write((uint)texture.Depth);
            w.Write((uint)texture.MipmapCount);
            w.Write(new byte[44]);
            w.Write(32U);
            w.Write(0x00U);
            w.Write(texture.Format.ToString().ToCharArray());
            w.Write(new byte[5 * 4]);
            w.Write(new byte[16 * 4]);
            w.Write(0U);
            // Pixel data
            w.Write(texture.Data);
        }
    }
}
