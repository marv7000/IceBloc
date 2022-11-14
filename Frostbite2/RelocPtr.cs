using System.Runtime.InteropServices;

namespace IceBloc.Frostbite2;

/// <summary>
/// A <see cref="RelocPtr"/> is a simple pointer that stores a reference with additional padding;
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 0x08)]
public unsafe struct RelocPtr
{
    public int Ptr = 0;
    public int Pad = 0;

    public RelocPtr()
    {
    }
}

/// <summary>
/// An array of <see cref="RelocPtr"/>s, prefixed by an array size.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 0x0C)]
public unsafe struct RelocArray
{
    public uint Size;
    public RelocPtr BaseAddress;
}