using IceBlocLib.Frostbite;
using System.Runtime.InteropServices;

namespace IceBlocLib.Frostbite2.Meshes;

[StructLayout(LayoutKind.Sequential, Size = 0x94)]
public struct MeshSubset
{
    public RelocPtr GeometryDeclarations = new(); // int
    public RelocPtr MaterialName = new(); // string
    public int MaterialIndex = 0;
    public int PrimitiveCount = 0;
    public int StartIndex = 0;
    public int VertexOffset = 0;
    public int VertexCount = 0;
    public byte VertexStride = 0;
    public PrimitiveType PrimitiveType = 0;
    public byte BonesPerVertex = 0;
    public byte BoneCount = 0;
    public RelocPtr BoneIndices = new(); // short
    public GeometryDeclarationDesc GeoDecls = new();
    public float[] TexCoordRatios = new float[6];

    public MeshSubset() { }
}

public enum PrimitiveType : byte
{
    PointList = 0x0,
    LineList = 0x1,
    LineStrip = 0x2,
    TriangleList = 0x3,
    TriangleStrip = 0x5,
    QuadList = 0x7,
    XenonRectList = 0x8,
};