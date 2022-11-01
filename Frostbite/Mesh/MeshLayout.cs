using System;
using System.Runtime.InteropServices;

namespace IceBloc.Frostbite.Mesh;

[StructLayout(LayoutKind.Sequential, Size = 0xB0)]
public struct MeshLayout
{
    public int Type;
    public int SubCount;
    public RelocPtr<MeshSubset> SubSets;
    public RelocArray<byte> CategorySubsetIndices0;
    public RelocArray<byte> CategorySubsetIndices1;
    public RelocArray<byte> CategorySubsetIndices2;
    public RelocArray<byte> CategorySubsetIndices3;
    public int Flags;
    public int IndexBufferFormat;
    public int IndexDataSize;
    public int VertexDataSize;
    public int EdgeDataSize;
    public Guid DataChunkID;
}
