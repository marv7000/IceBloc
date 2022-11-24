﻿using System;

namespace IceBloc.InternalFormats;

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
    DXT3,
    DXT5,
    DXN
}