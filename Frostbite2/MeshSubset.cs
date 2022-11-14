using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace IceBloc.Frostbite2;

[StructLayout(LayoutKind.Sequential, Size = 0x94)]
public unsafe struct MeshSubset
{
    public RelocPtr GeometryDeclarations = new(); // int
    public RelocPtr MaterialName = new(); // string
    public int MaterialIndex = 0;
    public int PrimitiveCount = 0;
    public int StartIndex = 0;
    public int VertexOffset = 0;
    public int VertexCount = 0;
    public byte VertexStride = 0;
    public byte PrimitiveType = 0;
    public byte BonesPerVertex = 0;
    public byte BoneCount = 0;
    public RelocPtr BoneIndices = new(); // short
    public GeometryDeclarationDesc GeoDecls = new();
    public float[] TexCoordRatios = new float[6];

    public MeshSubset()
    {
    }

    public int GetGeoDesc(VertexElementFormat type)
    {
        for (int i = 0; i < 16; i++)
        {
            var elemType = GeoDecls.Elements[i].Format;

            if (elemType == type)
            {
                return i;
            }
        }

        return -1;
    }

    public Vector4 Read(ReadOnlySpan<byte> buffer, int index)
    {
        return GeoDecls.Read(buffer, index);
    }
}
