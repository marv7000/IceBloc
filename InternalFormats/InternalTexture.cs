using System;

namespace IceBloc.InternalFormats;

public sealed class InternalTexture
{
    public string Name = "";
    public int Width;
    public int Height;
    public int Depth;
    public InternalTextureFormat Format;
    public int MipmapCount;
    public byte[] Data;
}

public enum InternalTextureFormat
{
    Grey,
    RGB,
    RGBA,
    DXT1,
    DXT3,
    DXT5
}