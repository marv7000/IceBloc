using IceBloc.Utility;
using System.IO;

namespace IceBloc.Frostbite2;

public class MeshSet
{
    public MeshSetLayout SetLayout;
    public MeshLayout[] Layout;
    public MeshSubset[][] Subsets;

    public MeshSet(Stream stream)
    {
        using var r = new BinaryReader(stream);

        // Parse MeshSetLayout.
        MeshSetLayout msl = new MeshSetLayout();
        msl.MeshType = (MeshType)r.ReadUInt32();
        msl.Flags = r.ReadUInt32();
        msl.LodCount = r.ReadInt32();
        msl.TotalSubsetCount = r.ReadInt32();
        msl.BoundBoxMin = r.ReadVector4();
        msl.BoundBoxMax = r.ReadVector4();
        msl.LOD0 = r.ReadRelocPtr();
        msl.LOD1 = r.ReadRelocPtr();
        msl.LOD2 = r.ReadRelocPtr();
        msl.LOD3 = r.ReadRelocPtr();
        msl.LOD4 = r.ReadRelocPtr();
        msl.Name = r.ReadRelocPtr();
        msl.ShortName = r.ReadRelocPtr();
        msl.NameHash = r.ReadInt32();
        r.ReadInt32(); // Pad

        // Parse MeshLayout for each LOD.
        MeshLayout[] ml = new MeshLayout[msl.LodCount];
        for (int i = 0; i < msl.LodCount; i++)
        {
            ml[i] = new();
            ml[i].Type = (MeshType)r.ReadUInt32();
            ml[i].SubCount = r.ReadInt32();
            ml[i].SubSets = r.ReadRelocPtr();
            ml[i].CategorySubsetIndices0 = r.ReadRelocArray();
            ml[i].CategorySubsetIndices1 = r.ReadRelocArray();
            ml[i].CategorySubsetIndices2 = r.ReadRelocArray();
            ml[i].CategorySubsetIndices3 = r.ReadRelocArray();
            ml[i].Flags = (MeshLayoutFlags)r.ReadUInt32();
            ml[i].IndexBufferFormat = (IndexBufferFormat)r.ReadInt32();
            ml[i].IndexDataSize = r.ReadInt32();
            ml[i].VertexDataSize = r.ReadInt32();
            ml[i].EdgeDataSize = r.ReadInt32();
            ml[i].DataChunkID = new System.Guid(r.ReadBytes(16));
            ml[i].AuxVertexIndexDataOffset = r.ReadInt32();
            ml[i].EmbeddedEdgeData = r.ReadRelocPtr();
            ml[i].ShaderDebugName = r.ReadRelocPtr();
            ml[i].Name = r.ReadRelocPtr();
            ml[i].ShortName = r.ReadRelocPtr();
            ml[i].NameHash = r.ReadInt32();
            ml[i].Data = r.ReadRelocPtr();
            ml[i].u17 = r.ReadInt32();
            ml[i].u18 = r.ReadInt64();
            ml[i].u19 = r.ReadInt64();
            ml[i].SubsetPartIndices = r.ReadRelocPtr();
        }

        // Parse each subset for each LOD. This 2D array maps X to the LOD and Y to the MeshSubset.
        Subsets = new MeshSubset[msl.LodCount][];

        for (int i = 0; i < msl.LodCount; i++)
        {
            Subsets[i] = new MeshSubset[ml[i].SubCount];
            for (int j = 0; j < ml[i].SubCount; j++)
            {
                Subsets[i][j] = new MeshSubset();
                Subsets[i][j].GeometryDeclarations = r.ReadRelocPtr(); // int
                Subsets[i][j].MaterialName = r.ReadRelocPtr(); // string
                Subsets[i][j].MaterialIndex = r.ReadInt32();
                Subsets[i][j].PrimitiveCount = r.ReadInt32();
                Subsets[i][j].StartIndex = r.ReadInt32();
                Subsets[i][j].VertexOffset = r.ReadInt32();
                Subsets[i][j].VertexCount = r.ReadInt32();
                Subsets[i][j].VertexStride = r.ReadByte();
                Subsets[i][j].PrimitiveType = r.ReadByte();
                Subsets[i][j].BonesPerVertex = r.ReadByte();
                Subsets[i][j].BoneCount = r.ReadByte();
                Subsets[i][j].BoneIndices = r.ReadRelocPtr(); // short
                Subsets[i][j].GeoDecls = r.ReadGeometryDeclarationDesc();
                for (int k = 0; k < 6; k++)
                {
                    Subsets[i][j].TexCoordRatios[k] = r.ReadSingle();
                }
            }
            r.BaseStream.Position += 8;
        }

        // Finalize
        SetLayout = msl;
        Layout = ml;
    }
}
