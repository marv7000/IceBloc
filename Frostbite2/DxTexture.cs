using IceBloc.Utility;
using System;
using System.IO;
using System.Text;

namespace IceBloc.Frostbite2;

public class DxTexture
{
    public uint[] MipOffsets = new uint[2];
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
    public string TextureGroup;

    public byte[] PixelData;

    /// <summary>
    /// Deserializes a DxTexture asset.
    /// </summary>
    /// <param name="texture"><see cref="DxTexture"/> asset to deserialize.</param>
    public DxTexture(byte[] texture)
    {
        using var stream = new MemoryStream(texture);
        using var reader = new BinaryReader(stream);

        MipOffsets[0] = reader.ReadUInt32();
        MipOffsets[1] = reader.ReadUInt32();
        Ttype = (TextureType)reader.ReadInt32();
        Format = (TextureFormat)reader.ReadUInt16();
        Flags = reader.ReadUInt32();
        Width = reader.ReadUInt16();
        Height = reader.ReadUInt16();
        Depth = reader.ReadUInt16();
        SliceCount = reader.ReadUInt16();
        MipmapCount = reader.ReadByte();
        MipmapBaseIndex = reader.ReadByte();
        StreamingChunkId = new Guid(reader.ReadBytes(16));
        for (int i = 0; i < 15; i++)
            MipmapSizes[i] = reader.ReadUInt32();
        MipmapChainSize = reader.ReadUInt32();
        ResourceNameHash = reader.ReadUInt32();
        TextureGroup = Encoding.ASCII.GetString(reader.ReadBytes(16)).Replace("\0", "");

        PixelData = IO.GetAssetFromGuid(StreamingChunkId);
    }

    public bool GetFlag(TextureHeaderFlags flag)
    {
        return (Flags & (uint)flag) != 0;
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

public enum TextureFormat : ushort
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

public enum TextureHeaderFlags
{
    Streaming = 0x1,
    SrgbGamma = 0x2,
    CpuResource = 0x4,
    OnDemandLoaded = 0x8,
    Mutable = 0x10,
    NoSkipmip = 0x20,
    XenonPackedMipmaps = 0x100,
    Ps3MemoryCell = 0x100,
    Ps3MemoryRsx = 0x200,
};

public enum DdsFormat
{
    UNKNOWN = 0x0,
    R32G32B32A32_TYPELESS = 0x1,
    R32G32B32A32_FLOAT = 0x2,
    R32G32B32A32_UINT = 0x3,
    R32G32B32A32_SINT = 0x4,
    R32G32B32_TYPELESS = 0x5,
    R32G32B32_FLOAT = 0x6,
    R32G32B32_UINT = 0x7,
    R32G32B32_SINT = 0x8,
    R16G16B16A16_TYPELESS = 0x9,
    R16G16B16A16_FLOAT = 0xA,
    R16G16B16A16_UNORM = 0xB,
    R16G16B16A16_UINT = 0xC,
    R16G16B16A16_SNORM = 0xD,
    R16G16B16A16_SINT = 0xE,
    R32G32_TYPELESS = 0xF,
    R32G32_FLOAT = 0x10,
    R32G32_UINT = 0x11,
    R32G32_SINT = 0x12,
    R32G8X24_TYPELESS = 0x13,
    D32_FLOAT_S8X24_UINT = 0x14,
    R32_FLOAT_X8X24_TYPELESS = 0x15,
    X32_TYPELESS_G8X24_UINT = 0x16,
    R10G10B10A2_TYPELESS = 0x17,
    R10G10B10A2_UNORM = 0x18,
    R10G10B10A2_UINT = 0x19,
    R11G11B10_FLOAT = 0x1A,
    R8G8B8A8_TYPELESS = 0x1B,
    R8G8B8A8_UNORM = 0x1C,
    R8G8B8A8_UNORM_SRGB = 0x1D,
    R8G8B8A8_UINT = 0x1E,
    R8G8B8A8_SNORM = 0x1F,
    R8G8B8A8_SINT = 0x20,
    R16G16_TYPELESS = 0x21,
    R16G16_FLOAT = 0x22,
    R16G16_UNORM = 0x23,
    R16G16_UINT = 0x24,
    R16G16_SNORM = 0x25,
    R16G16_SINT = 0x26,
    R32_TYPELESS = 0x27,
    D32_FLOAT = 0x28,
    R32_FLOAT = 0x29,
    R32_UINT = 0x2A,
    R32_SINT = 0x2B,
    R24G8_TYPELESS = 0x2C,
    D24_UNORM_S8_UINT = 0x2D,
    R24_UNORM_X8_TYPELESS = 0x2E,
    X24_TYPELESS_G8_UINT = 0x2F,
    R8G8_TYPELESS = 0x30,
    R8G8_UNORM = 0x31,
    R8G8_UINT = 0x32,
    R8G8_SNORM = 0x33,
    R8G8_SINT = 0x34,
    R16_TYPELESS = 0x35,
    R16_FLOAT = 0x36,
    D16_UNORM = 0x37,
    R16_UNORM = 0x38,
    R16_UINT = 0x39,
    R16_SNORM = 0x3A,
    R16_SINT = 0x3B,
    R8_TYPELESS = 0x3C,
    R8_UNORM = 0x3D,
    R8_UINT = 0x3E,
    R8_SNORM = 0x3F,
    R8_SINT = 0x40,
    A8_UNORM = 0x41,
    R1_UNORM = 0x42,
    R9G9B9E5_SHAREDEXP = 0x43,
    R8G8_B8G8_UNORM = 0x44,
    G8R8_G8B8_UNORM = 0x45,
    BC1_TYPELESS = 0x46,
    BC1_UNORM = 0x47,
    BC1_UNORM_SRGB = 0x48,
    BC2_TYPELESS = 0x49,
    BC2_UNORM = 0x4A,
    BC2_UNORM_SRGB = 0x4B,
    BC3_TYPELESS = 0x4C,
    BC3_UNORM = 0x4D,
    BC3_UNORM_SRGB = 0x4E,
    BC4_TYPELESS = 0x4F,
    BC4_UNORM = 0x50,
    BC4_SNORM = 0x51,
    BC5_TYPELESS = 0x52,
    BC5_UNORM = 0x53,
    BC5_SNORM = 0x54,
    B5G6R5_UNORM = 0x55,
    B5G5R5A1_UNORM = 0x56,
    B8G8R8A8_UNORM = 0x57,
    B8G8R8X8_UNORM = 0x58,
    R10G10B10_XR_BIAS_A2_UNORM = 0x59,
    B8G8R8A8_TYPELESS = 0x5A,
    B8G8R8A8_UNORM_SRGB = 0x5B,
    B8G8R8X8_TYPELESS = 0x5C,
    B8G8R8X8_UNORM_SRGB = 0x5D,
    BC6H_TYPELESS = 0x5E,
    BC6H_UF16 = 0x5F,
    BC6H_SF16 = 0x60,
    BC7_TYPELESS = 0x61,
    BC7_UNORM = 0x62,
    BC7_UNORM_SRGB = 0x63
};

