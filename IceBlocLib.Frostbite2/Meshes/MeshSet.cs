using IceBlocLib.InternalFormats;
using IceBlocLib.Frostbite;
using System.Numerics;
using IceBlocLib.Utility.Export;

namespace IceBlocLib.Frostbite2.Meshes;

public class MeshSet
{
    public MeshSetLayout SetLayout;
    public MeshLayout[] Layout;
    public MeshSubset[][] Subsets;

    public MeshSet(BinaryReader r)
    {
        MeshSetLayout msl = r.ReadMeshSetLayout();
        MeshLayout[] ml = new MeshLayout[msl.LodCount];
        MeshSubset[][] sub = new MeshSubset[msl.LodCount][];

        for (int i = 0; i < msl.LodCount; i++)
        {
            r.BaseStream.Position = msl.LOD[i].Ptr;
            ml[i] = r.ReadMeshLayout();
        }

        for (int i = 0; i < msl.LodCount; i++)
        {
            r.BaseStream.Position = ml[i].SubSets.Ptr;
            sub[i] = new MeshSubset[ml[i].SubCount];
            for (int j = 0; j < ml[i].SubCount; j++)
                sub[i][j] = r.ReadMeshSubset();
        }

        // Finalize
        SetLayout = msl;
        Layout = ml;
        Subsets = sub;
        r.BaseStream.Position = 0;
    }

    public static List<InternalMesh> ConvertToInternal(Stream meshSetStream)
    {
        List<InternalMesh> meshList = new();

        // Create readers for our streams.
        using var mr = new BinaryReader(meshSetStream);

        // Load the MeshSet from stream.
        MeshSet meshSet = new(mr);

        byte[] chunk = new byte[0];
        // For each LOD.
        for (int i = 0; i < meshSet.Subsets.Length; i++)
        {
            Stream stream;
            if (meshSet.Layout[i].EmbeddedEdgeData.Value == null)
            {
                chunk = IO.GetChunk(meshSet.Layout[i].DataChunkID);
                stream = new MemoryStream(chunk);
            }
            else
                stream = new MemoryStream(meshSet.Layout[i].EmbeddedEdgeData.Value as byte[]);

            using var cr = new BinaryReader(stream);

            // For each MeshSubset.
            for (int j = 0; j < meshSet.Subsets[i].Length; j++)
            {
                InternalMesh mesh = new();
                MeshSubset sub = meshSet.Subsets[i][j];

                mesh.Name = (string) sub.MaterialName.Value;

                var indexStartOffset = meshSet.Layout[i].VertexDataSize;

                // Start reading vertices.
                cr.BaseStream.Position = sub.VertexOffset;

                bool isSkinned = sub.BoneCount > 0;

                for (int k = 0; k < sub.VertexCount; k++)
                {
                    Vertex vert = new();

                    var posElement = sub.GeoDecls.GetByUsage(VertexElementUsage.Pos);
                    var norElement = sub.GeoDecls.GetByUsage(VertexElementUsage.Normal);
                    var uv0Element = sub.GeoDecls.GetByUsage(VertexElementUsage.TexCoord0);

                    var boneIndexElement = sub.GeoDecls.GetByUsage(VertexElementUsage.BoneIndices);
                    var boneWeightElement = sub.GeoDecls.GetByUsage(VertexElementUsage.BoneWeights);

                    Vector4 position = new();
                    Vector4 normals = new();
                    Vector4 texcoord = new();
                    Vector4 boneIndex = new(-1.0f);
                    Vector4 boneWeight = new(1.0f);
                    position = posElement.Read(cr, sub.VertexStride);
                    normals = norElement.Read(cr, sub.VertexStride);
                    texcoord = uv0Element.Read(cr, sub.VertexStride);
                    
                    if (isSkinned)
                    {
                        boneIndex = boneIndexElement.Read(cr, sub.VertexStride);
                        boneWeight = boneWeightElement.Read(cr, sub.VertexStride);
                    }
                    // We're done reading the current vertex, move up the stream.
                    cr.BaseStream.Position += sub.VertexStride;

                    vert.PositionX = position.X;
                    vert.PositionY = position.Y;
                    vert.PositionZ = position.Z;
                    vert.NormalX = normals.X;
                    vert.NormalY = normals.Y;
                    vert.NormalZ = normals.Z;
                    vert.TexCoordX = texcoord.X;
                    vert.TexCoordY = 1.0f - texcoord.Y;

                    var bIdx = sub.BoneIndices.Value as List<object>;
                    if (isSkinned)
                    {
                        vert.BoneIndexA = (int)(short)bIdx[(int)boneIndex.X];
                        vert.BoneIndexB = (int)(short)bIdx[(int)boneIndex.Y];
                        vert.BoneIndexC = (int)(short)bIdx[(int)boneIndex.Z];
                        vert.BoneIndexD = (int)(short)bIdx[(int)boneIndex.W];
                        vert.BoneWeightA = boneWeight.X;
                        vert.BoneWeightB = boneWeight.Y;
                        vert.BoneWeightC = boneWeight.Z;
                        vert.BoneWeightD = boneWeight.W;
                    }
                    else
                    {
                        vert.BoneIndexA = 0;
                        vert.BoneIndexB = 0;
                        vert.BoneIndexC = 0;
                        vert.BoneIndexD = 0;
                        vert.BoneWeightA = 0.0f;
                        vert.BoneWeightB = 0.0f;
                        vert.BoneWeightC = 0.0f;
                        vert.BoneWeightD = 1.0f;
                    }

                    mesh.Vertices.Add(vert);
                }

                // Read face indices.
                cr.BaseStream.Position = indexStartOffset + sub.StartIndex * 2;

                for (int k = 0; k < sub.PrimitiveCount; k++)
                {
                    int a = cr.ReadUInt16();
                    int b = cr.ReadUInt16();
                    int c = cr.ReadUInt16();

                    mesh.Faces.Add((a, b, c));
                }

                meshList.Add(mesh);
            }
            stream.Close();
        }

        return meshList;
    }
}

public static class MeshSetExtensions
{
    public static GeometryDeclarationDesc ReadGeometryDeclarationDesc(this BinaryReader reader)
    {
        GeometryDeclarationDesc desc = new();
        for (int i = 0; i < 16; i++)
        {
            desc.Elements[i].Usage = (VertexElementUsage)reader.ReadByte();
            desc.Elements[i].Format = (VertexElementFormat)reader.ReadByte();
            desc.Elements[i].Offset = reader.ReadByte();
            desc.Elements[i].StreamIndex = reader.ReadByte();
        }
        for (int i = 0; i < 4; i++)
        {
            desc.Streams[i].Stride = reader.ReadByte();
            desc.Streams[i].Classification = (VertexElementClassification)reader.ReadByte();
        }
        desc.ElementCount = reader.ReadByte();
        desc.StreamCount = reader.ReadByte();
        reader.ReadBytes(2);

        return desc;
    }

    public static MeshSetLayout ReadMeshSetLayout(this BinaryReader r)
    {
        MeshSetLayout msl = new();
        msl.MeshType = (MeshType)r.ReadUInt32();
        msl.Flags = r.ReadUInt32();
        msl.LodCount = r.ReadInt32();
        msl.TotalSubsetCount = r.ReadInt32();
        msl.BoundBoxMin = r.ReadVector4();
        msl.BoundBoxMax = r.ReadVector4();
        for (int i = 0; i < 5; i++)
        {
            msl.LOD[i] = r.ReadRelocPtr<MeshLayout>();
        }
        msl.Name = r.ReadRelocPtr<string>();
        msl.ShortName = r.ReadRelocPtr<string>();
        msl.NameHash = r.ReadInt32();
        r.ReadInt32(); // Pad

        return msl;
    }
    public static MeshLayout ReadMeshLayout(this BinaryReader r)
    {
        MeshLayout ml = new();

        ml.Type = (MeshType)r.ReadUInt32();
        ml.SubCount = r.ReadInt32();
        ml.SubSets = r.ReadRelocPtr<MeshSubset>();
        for (int j = 0; j < 4; j++)
        {
            ml.CategorySubsetIndices[j] = r.ReadRelocArray<byte>();
        }
        ml.Flags = (MeshLayoutFlags)r.ReadUInt32();
        ml.IndexBufferFormat = (IndexBufferFormat)r.ReadInt32();
        ml.IndexDataSize = r.ReadInt32();
        ml.VertexDataSize = r.ReadInt32();
        ml.EdgeDataSize = r.ReadInt32();
        ml.DataChunkID = new Guid(r.ReadBytes(16));
        ml.AuxVertexIndexDataOffset = r.ReadInt32();
        ml.EmbeddedEdgeData = r.ReadRelocPtr<byte[]>();
        ml.ShaderDebugName = r.ReadRelocPtr<string>();
        ml.Name = r.ReadRelocPtr<string>();
        ml.ShortName = r.ReadRelocPtr<string>();
        ml.NameHash = r.ReadInt32();
        ml.Data = r.ReadRelocPtr<int>();
        ml.u17 = r.ReadInt32();
        ml.u18 = r.ReadInt64();
        ml.u19 = r.ReadInt64();
        ml.SubsetPartIndices = r.ReadRelocPtr<short>();

        return ml;
    }
    public static MeshSubset ReadMeshSubset(this BinaryReader r)
    {
        MeshSubset subset = new();

        subset.GeometryDeclarations = r.ReadRelocPtr<int>();
        subset.MaterialName = r.ReadRelocPtr<string>();
        subset.MaterialIndex = r.ReadInt32();
        subset.PrimitiveCount = r.ReadInt32();
        subset.StartIndex = r.ReadInt32();
        subset.VertexOffset = r.ReadInt32();
        subset.VertexCount = r.ReadInt32();
        subset.VertexStride = r.ReadByte();
        subset.PrimitiveType = (PrimitiveType)r.ReadByte();
        subset.BonesPerVertex = r.ReadByte();
        subset.BoneCount = r.ReadByte();
        subset.BoneIndices = r.ReadRelocPtr<short>(subset.BoneCount);
        subset.GeoDecls = r.ReadGeometryDeclarationDesc();
        for (int i = 0; i < 6; i++)
        {
            subset.TexCoordRatios[i] = r.ReadSingle();
        }

        return subset;
    }
}