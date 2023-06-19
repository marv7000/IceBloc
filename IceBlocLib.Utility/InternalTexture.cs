using System;

namespace IceBlocLib.InternalFormats;

public sealed class InternalTexture
{
    public int Width;
    public int Height;
    public int Depth;
    public InternalTextureFormat Format;
    public int MipmapCount;
    public byte[] Data;
}

public enum InternalTextureFormat
{
    UNKNOWN = -1,
    GREY,
    RGB0,
    RGBA,
    DXT1,
    DXT1Normal,
    DXT3,
    DXT5,
    DXN
}