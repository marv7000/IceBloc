using IceBlocLib.Frostbite;
using System;
using System.Runtime.InteropServices;

namespace IceBlocLib.Frostbite2013.Meshes;

public struct MeshLayout
{
    public MeshType Type = 0;
    public int SubCount = 0;
    public RelocPtr SubSets = new(); // MeshSubset
    public RelocArray[] CategorySubsetIndices = new RelocArray[4]; // byte
    public MeshLayoutFlags Flags = 0;
    public IndexBufferFormat IndexBufferFormat = 0;
    public int IndexDataSize = 0;
    public int VertexDataSize = 0;
    public int EdgeDataSize = 0;
    public Guid DataChunkID = new();
    public int AuxVertexIndexDataOffset = 0;
    public RelocPtr EmbeddedEdgeData = new();
    public RelocPtr ShaderDebugName = new();
    public RelocPtr Name = new();
    public RelocPtr ShortName = new();
    public int NameHash = 0;
    public RelocPtr Data = new();
    public int u17 = 0;
    public long u18 = 0;
    public long u19 = 0;
    public RelocPtr SubsetPartIndices = new();

    public MeshLayout()
    {

    }
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