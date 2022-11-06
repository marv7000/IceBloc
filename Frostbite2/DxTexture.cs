using IceBloc.Utility;
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
    public byte[] PixelData;

    /// <summary>
    /// Deserializes a DxTexture asset.
    /// </summary>
    public DxTexture(Stream dxTextureStream)
    {
        using var reader = new BinaryReader(dxTextureStream);

        Version             = reader.ReadUInt32();
        TexType             = (TextureType)reader.ReadInt32();
        TexFormat           = (TextureFormat)reader.ReadUInt16();
        Flags               = reader.ReadUInt32();
        Width               = reader.ReadUInt16();
        Height              = reader.ReadUInt16();
        Depth               = reader.ReadUInt16();
        SliceCount          = reader.ReadUInt16();
        _Pad0               = reader.ReadUInt16();
        MipmapCount         = reader.ReadByte();
        MipmapBaseIndex     = reader.ReadByte();
        StreamingChunkId    = new Guid(reader.ReadBytes(16));
        // Mipmaps.
        for (int i = 0; i < 15; i++) MipmapSizes[i] = reader.ReadUInt32();
        MipmapChainSize     = reader.ReadUInt32();
        ResourceNameHash    = reader.ReadUInt32();
        // A TextureGroup is always 16 chars long, we will reinterpret as string for ease of use.
        TextureGroup        = Encoding.ASCII.GetString(reader.ReadBytes(16)).Replace("\0", "");
        // Finally, load the chunk containing the image data.
        PixelData           = IO.GetAssetFromGuid(StreamingChunkId);
    }

    public bool GetFlag(TextureHeaderFlags flag)
    {
        return (Flags & (uint)flag) != 0;
    }
}
