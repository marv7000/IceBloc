using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace IceBloc.Frostbite2;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct GeometryDeclarationDesc
{
    public fixed byte Element[16 * 4];
    public fixed short Streams[4];
    public byte ElementCount;
    public byte StreamCount;
    public byte Padding0;
    public byte Padding1;

    public Vector4 Read(ReadOnlySpan<byte> buffer, int index)
    {
        var type = Element[index * 4 + 1];
        var offset = Element[index * 4 + 2];
        var data = buffer[offset..];

        var result = Vector4.Zero;

        switch (type)
        {
            case 7:
                {
                    var casted = MemoryMarshal.Cast<byte, Half>(data);
                    return new((float)casted[0], (float)casted[1], (float)casted[2], 0.0f);
                }
            case 8:
                {
                    var casted = MemoryMarshal.Cast<byte, Half>(data);
                    return new((float)casted[0], (float)casted[1], (float)casted[2], (float)casted[3]);
                }
            case 12:
                {
                    var casted = MemoryMarshal.Cast<byte, byte>(data);
                    return new(casted[0], casted[1], casted[2], casted[3]);
                }
            case 13:
                {
                    var casted = MemoryMarshal.Cast<byte, byte>(data);
                    return new(casted[0] / 255.0f, casted[1] / 255.0f, casted[2] / 255.0f, casted[3] / 255.0f);
                }
        }

        return result;
    }
}
