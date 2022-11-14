using System;
using System.Runtime.InteropServices;

namespace IceBloc.Frostbite2;

[StructLayout(LayoutKind.Sequential, Size = 0xB0)]
public struct MeshLayout
{
    public MeshType Type;
    public int SubCount;
    public RelocPtr SubSets; // MeshSubset
    public RelocArray CategorySubsetIndices0; // byte
    public RelocArray CategorySubsetIndices1; // byte
    public RelocArray CategorySubsetIndices2; // byte
    public RelocArray CategorySubsetIndices3; // byte
    public MeshLayoutFlags Flags;
    public IndexBufferFormat IndexBufferFormat;
    public int IndexDataSize;
    public int VertexDataSize;
    public int EdgeDataSize;
    public Guid DataChunkID;
    public int AuxVertexIndexDataOffset;
    public RelocPtr EmbeddedEdgeData;
    public RelocPtr ShaderDebugName;
    public RelocPtr Name;
    public RelocPtr ShortName;
    public int NameHash;
    public RelocPtr Data;
    public int u17;
    public long u18;
    public long u19;
    public RelocPtr SubsetPartIndices;
}

public enum IndexBufferFormat : int
{
    IndexBufferFormat_16Bit = 0x0,
    IndexBufferFormat_32Bit = 0x1,
};

public enum MeshLayoutFlags : int
{
    IsBaseLod = 0x1,
    StreamingEnable = 0x40,
    StreamInstancingEnable = 0x10,
    VertexAnimationEnable = 0x80,
    IsDataAvailable = 0x20000000,
};
