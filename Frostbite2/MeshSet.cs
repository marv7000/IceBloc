using IceBloc.Utility;
using System.IO;

namespace IceBloc.Frostbite2;

public class MeshSet
{
    public MeshSetLayout SetLayout;
    public MeshLayout Layout;
    public MeshSubset[] Subsets;

    public MeshSet(Stream stream)
    {
        using var r = new BinaryReader(stream);

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

        MeshLayout ml = new MeshLayout();
        ml.Type = (MeshType)r.ReadUInt32();
        ml.SubCount = r.ReadInt32();
        ml.SubSets = r.ReadRelocPtr();
        ml.CategorySubsetIndices0 = r.ReadRelocArray();
        ml.CategorySubsetIndices1 = r.ReadRelocArray();
        ml.CategorySubsetIndices2 = r.ReadRelocArray();
        ml.CategorySubsetIndices3 = r.ReadRelocArray();
        ml.Flags = (MeshLayoutFlags)r.ReadUInt32();
        ml.IndexBufferFormat = (IndexBufferFormat)r.ReadInt32();
        ml.IndexDataSize = r.ReadInt32();
        ml.VertexDataSize = r.ReadInt32();
        ml.EdgeDataSize = r.ReadInt32();
        ml.DataChunkID = new System.Guid(r.ReadBytes(16));
        ml.AuxVertexIndexDataOffset = r.ReadInt32();
        ml.EmbeddedEdgeData = r.ReadRelocPtr();
        ml.ShaderDebugName = r.ReadRelocPtr();
        ml.Name = r.ReadRelocPtr();
        ml.ShortName = r.ReadRelocPtr();
        ml.NameHash = r.ReadInt32();
        ml.Data = r.ReadRelocPtr();
        ml.u17 = r.ReadInt32();
        ml.u18 = r.ReadInt64();
        ml.u19 = r.ReadInt64();
        ml.SubsetPartIndices = r.ReadRelocPtr();

        Subsets = new MeshSubset[ml.SubCount];
        for (int i = 0; i < ml.SubCount; i++)
        {
            Subsets[i] = new MeshSubset();
            Subsets[i].GeometryDeclarations = r.ReadRelocPtr(); // int
            Subsets[i].MaterialName = r.ReadRelocPtr(); // string
            Subsets[i].MaterialIndex = r.ReadInt32();
            Subsets[i].PrimitiveCount = r.ReadInt32();
            Subsets[i].StartIndex = r.ReadInt32();
            Subsets[i].VertexOffset = r.ReadInt32();
            Subsets[i].VertexCount = r.ReadInt32();
            Subsets[i].VertexStride = r.ReadByte();
            Subsets[i].PrimitiveType = r.ReadByte();
            Subsets[i].BonesPerVertex = r.ReadByte();
            Subsets[i].BoneCount = r.ReadByte();
            Subsets[i].BoneIndices = r.ReadRelocPtr(); // short
            Subsets[i].GeoDecls = r.ReadGeometryDeclarationDesc();
            for (int j = 0; j < 6; j++)
            {
                Subsets[i].TexCoordRatios[j] = r.ReadSingle();
            }
        }

        // Finalize
        SetLayout = msl;
        Layout = ml;
    }
}
