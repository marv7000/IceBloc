using IceBloc.InternalFormats;
using System;
using System.IO;
using System.Text;
namespace IceBloc.Frostbite2;

public class DxTexture
{
    public uint Version;
    public TextureType TexType;
    public TextureFormat TexFormat;
    public uint Flags;
    public ushort Width;
    public ushort Height;
    public ushort Depth;
    public ushort SliceCount;
    private readonly ushort _Pad0;
    public byte MipmapCount;
    public byte MipmapBaseIndex;
    public Guid StreamingChunkId;
    public uint[] MipmapSizes = new uint[15];
    public uint MipmapChainSize;
    public uint ResourceNameHash;
    public string TextureGroup;

    public DxTexture() { }

    public static InternalTexture ConvertToInternal(Stream res, Stream chunk)
    {
        using var rr = new BinaryReader(res);
        using var cr = new BinaryReader(chunk);

        InternalTexture internalTex = new();
        var tex = rr.ReadDxTexture();
        // Load the chunk containing the image data.
        byte[] data = cr.ReadBytes((int)cr.BaseStream.Length);

        // Start converting to InternalTexture.
        internalTex.Width = tex.Width;
        internalTex.Height = tex.Height;
        internalTex.Depth = tex.Depth;
        internalTex.MipmapCount = tex.MipmapCount;
        internalTex.Format = GetInternalTextureFormat(tex.TexFormat);
        internalTex.Data = data;

        return internalTex;
    }

    private static InternalTextureFormat GetInternalTextureFormat(TextureFormat texFormat)
    {
        switch(texFormat)
        {
            case TextureFormat.DXT1:
                return InternalTextureFormat.DXT1;
            case TextureFormat.DXT3:
                return InternalTextureFormat.DXT3;
            case TextureFormat.DXT5:
                return InternalTextureFormat.DXT5;
            case TextureFormat.RGB888:
                return InternalTextureFormat.RGB0;
            case TextureFormat.ARGB8888:
                return InternalTextureFormat.RGBA;
        }
        return InternalTextureFormat.UNKNOWN;
    }

    public bool GetFlag(TextureHeaderFlags flag)
    {
        return (Flags & (uint)flag) != 0;
    }
}
