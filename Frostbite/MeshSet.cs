using IceBloc.InternalFormats;
using IceBloc.Utility;
using System.Collections.Generic;
using System.IO;

namespace IceBloc.Frostbite;

public class MeshSet
{
    public MeshSetLayout SetLayout;
    public MeshLayout[] Layout;
    public MeshSubset[][] Subsets;

    public MeshSet(BinaryReader r)
    {
        MeshSetLayout msl  = r.ReadMeshSetLayout();
        MeshLayout[] ml    = new MeshLayout[msl.LodCount];
        MeshSubset[][] sub = new MeshSubset[msl.LodCount][];

        for (int i = 0; i < msl.LodCount; i++) 
            ml[i] = r.ReadMeshLayout();
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

        // For each LOD.
        for (int i = 0; i < meshSet.Subsets.Length; i++)
        {
            Stream stream;
            if (meshSet.Layout[i].EmbeddedEdgeData.Value == null)
                stream = new MemoryStream(IO.GetChunk(meshSet.Layout[i].DataChunkID));
            else
                stream = new MemoryStream(meshSet.Layout[i].EmbeddedEdgeData.Value);

            using var cr = new BinaryReader(stream);

            // For each MeshSubset.
            for (int j = 0; j < meshSet.Subsets[i].Length; j++)
            {
                InternalMesh mesh = new();
                MeshSubset sub = meshSet.Subsets[i][j];

                mesh.Name = sub.MaterialName.Value + "_LOD" + j;
                mesh.IsSkinned = false; // TODO

                var indexStartOffset = meshSet.Layout[i].VertexDataSize;

                // Start reading vertices.
                cr.BaseStream.Position = sub.VertexOffset;

                for (int k = 0; k < sub.VertexCount; k++)
                {
                    Vertex vert = new();

                    var posElement = sub.GeoDecls.GetByUsage(VertexElementUsage.Pos);
                    var norElement = sub.GeoDecls.GetByUsage(VertexElementUsage.Normal);
                    var uv0Element = sub.GeoDecls.GetByUsage(VertexElementUsage.TexCoord0);

                    var position = posElement.Read(cr, sub.VertexStride);
                    var normals = norElement.Read(cr, sub.VertexStride);
                    var texcoord = uv0Element.Read(cr, sub.VertexStride);

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

                    mesh.Vertices.Add(vert);
                }

                // Read face indices.
                cr.BaseStream.Position = indexStartOffset + (sub.StartIndex * 2);

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

    public static List<InternalMesh> ConvertToInternal(Stream meshSetStream, Stream chunkStream)
    {
        List<InternalMesh> meshList = new();

        // Create readers for our streams.
        using var mr = new BinaryReader(meshSetStream);
        using var cr = new BinaryReader(chunkStream);

        // Load the MeshSet from stream.
        MeshSet meshSet = new(mr);

        // For each LOD.
        for (int i = 0; i < meshSet.Subsets.Length; i++)
        {
            // For each MeshSubset.
            for (int j = 0; j < meshSet.Subsets[i].Length; j++)
            {
                InternalMesh mesh = new();
                MeshSubset sub = meshSet.Subsets[i][j];

                mesh.Name = sub.MaterialName.Value;
                mesh.IsSkinned = false; // TODO

                var indexStartOffset = meshSet.Layout[i].VertexDataSize;

                // Start reading vertices.
                cr.BaseStream.Position = sub.VertexOffset;

                for (int k = 0; k < sub.VertexCount; k++)
                {
                    Vertex vert = new();

                    var posElement = sub.GeoDecls.GetByUsage(VertexElementUsage.Pos);
                    var norElement = sub.GeoDecls.GetByUsage(VertexElementUsage.Normal);
                    var uv0Element = sub.GeoDecls.GetByUsage(VertexElementUsage.TexCoord0);

                    var position = posElement.Read(cr, sub.VertexStride);
                    var normals = norElement.Read(cr, sub.VertexStride);
                    var texcoord = uv0Element.Read(cr, sub.VertexStride);

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
                    
                    mesh.Vertices.Add(vert);
                }

                // Read face indices.
                cr.BaseStream.Position = indexStartOffset + (sub.StartIndex*2);

                for (int k = 0; k < sub.PrimitiveCount; k++)
                {
                    int a = cr.ReadUInt16();
                    int b = cr.ReadUInt16();
                    int c = cr.ReadUInt16();

                    mesh.Faces.Add((a, b, c));
                }

                meshList.Add(mesh);
            }
        }

        return meshList;
    }
}
