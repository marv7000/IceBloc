using IceBreaker.Utility.Export;
using System;
using System.IO;

namespace IceBreaker.Frostbite.Texture;

public class DxTexture : IFrostbiteResource
{
    public TextureType Ttype;
    public TextureFormat Format;
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
    public char[] TextureGroup = new char[16];

    public byte[] Data;

    /// <summary>
    /// Deserializes a DxTexture asset.
    /// </summary>
    /// <param name="texture"><see cref="DxTexture"/> asset to deserialize.</param>
    public DxTexture(byte[] texture)
    {
        using var stream = new MemoryStream(texture);
        using var reader = new BinaryReader(stream);
        reader.BaseStream.Position = 6;
        Ttype = (TextureType)reader.ReadInt32();
        Format = (TextureFormat)reader.ReadInt32();
        Flags = reader.ReadUInt32();
        Width = reader.ReadUInt16();
        Height = reader.ReadUInt16();
        Depth = reader.ReadUInt16();
        SliceCount = reader.ReadUInt16();
        MipmapCount = reader.ReadByte();
        MipmapBaseIndex = reader.ReadByte();
        StreamingChunkId = Guid.Parse((ReadOnlySpan<char>)reader.ReadChars(16));
        for (int i = 0; i < 15; i++)
            MipmapSizes[i] = reader.ReadUInt32();
        MipmapChainSize = reader.ReadUInt32();
        ResourceNameHash = reader.ReadUInt32();
        for (int i = 0; i < 16; i++)
            TextureGroup[i] = reader.ReadChar();
    }

    /// <inheritdoc/>
    public byte[] Export()
    {
        return DdsExport.ToBytes();
    }
}

public enum TextureType
{
    Type1D = 0x5,
    Type1DArray = 0x4,
    Type2D = 0x0,
    Type2DArray = 0x3,
    TypeCube = 0x1,
    Type3D = 0x2,
};

public enum TextureFormat
{
    DXT1 = 0x0,
    DXT3 = 0x1,
    DXT5 = 0x2,
    DXT5A = 0x3,
    DXN = 0x4,
    RGB565 = 0x5,
    RGB888 = 0x6,
    ARGB1555 = 0x7,
    ARGB4444 = 0x8,
    ARGB8888 = 0x9,
    L8 = 0xA,
    L16 = 0xB,
    ABGR16 = 0xC,
    ABGR16F = 0xD,
    ABGR32F = 0xE,
    R16F = 0xF,
    R32F = 0x10,
    NormalDXN = 0x11,
    NormalDXT1 = 0x12,
    NormalDXT5 = 0x13,
    NormalDXT5RGA = 0x14,
    RG8 = 0x15,
    GR16 = 0x16,
    GR16F = 0x17,
    D16 = 0x18,
    D24S8 = 0x19,
    D24FS8 = 0x1A,
    D32F = 0x1B,
    ABGR32 = 0x1C,
    GR32F = 0x1D,
};