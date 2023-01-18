using IceBloc.InternalFormats;
using IceBloc.Utility;
using System;
using System.IO;
using System.Text;
namespace IceBloc.Frostbite.Textures;

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
    public byte MipmapCount;
    public byte MipmapBaseIndex;
    public Guid StreamingChunkId;
    public uint[] MipmapSizes = new uint[15];
    public uint MipmapChainSize;
    public uint ResourceNameHash;
    public string TextureGroup;

    public DxTexture(BinaryReader rr)
    {
        Version = rr.ReadUInt32();
        TexType = (TextureType)rr.ReadInt32();
        TexFormat = (TextureFormat)rr.ReadUInt32();
        Flags = rr.ReadUInt32();
        Width = rr.ReadUInt16();
        Height = rr.ReadUInt16();
        Depth = rr.ReadUInt16();
        SliceCount = rr.ReadUInt16();
        rr.ReadUInt16();
        MipmapCount = rr.ReadByte();
        MipmapBaseIndex = rr.ReadByte();
        StreamingChunkId = new Guid(rr.ReadBytes(16));
        // Mipmaps.
        for (int i = 0; i < 15; i++) MipmapSizes[i] = rr.ReadUInt32();
        MipmapChainSize = rr.ReadUInt32();
        ResourceNameHash = rr.ReadUInt32();
        // A TextureGroup is always 16 chars long, we will reinterpret as string for ease of use.
        TextureGroup = Encoding.ASCII.GetString(rr.ReadBytes(16)).Replace("\0", "");
    }

    public static InternalTexture ConvertToInternal(Stream res)
    {
        using var rr = new BinaryReader(res);

        InternalTexture internalTex = new();
        var tex = new DxTexture(rr);

        using var mem = new MemoryStream(IO.GetChunk(tex.StreamingChunkId));
        using var cr = new BinaryReader(mem);
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
        switch (texFormat)
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
            case TextureFormat.NormalDXN:
                return InternalTextureFormat.DXN;
            case TextureFormat.NormalDXT1:
                return InternalTextureFormat.DXT1;
            case TextureFormat.NormalDXT5:
                return InternalTextureFormat.DXT5;
        }
        return InternalTextureFormat.UNKNOWN;
    }

    public bool GetFlag(TextureHeaderFlags flag)
    {
        return (Flags & (uint)flag) != 0;
    }
}
