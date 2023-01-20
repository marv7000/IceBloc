using IceBlocLib.Frostbite;
using IceBlocLib.InternalFormats;
using System.Text;
namespace IceBlocLib.Frostbite2.Textures;

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