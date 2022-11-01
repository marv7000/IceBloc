using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace IceBloc.Frostbite.Mesh;

[StructLayout(LayoutKind.Sequential, Size = 0x94)]
public unsafe struct MeshSubset
{
    public RelocPtr<int> GeometryDeclarations;
    public RelocPtrStr MaterialName;
    public int MaterialIndex;
    public int PrimitiveCount;
    public int StartIndex;
    public int VertexOffset;
    public int VertexCount;
    public byte VertexStride;
    public byte PrimitiveType;
    public byte BonesPerVertex;
    public byte BoneCount;
    public RelocPtr<short> BoneIndices;
    public GeometryDeclarationDesc GeoDecls;
    public fixed float TexCoordRatios[6];

    public int GetGeoDesc(int type)
    {
        for (int i = 0; i < 16; i++)
        {
            var elemType = GeoDecls.Element[i * 4];

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
