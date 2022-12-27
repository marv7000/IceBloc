using System.IO;
using IceBloc.InternalFormats;

namespace IceBloc.Export;

/// <summary>
/// Exports <see cref="InternalMesh"/> objects to the .ATF format.
/// </summary>
public class ModelExporterATF : IModelExporter
{
    /// <inheritdoc/>
    public void Export(InternalMesh mesh, string path)
    {
        // Start writing to disk.
        using var w = new StreamWriter(File.OpenWrite(path + ".atf"));

        // Meta data.
        w.WriteLine("META,Exporter,IceBloc");
        w.WriteLine("TYPE,MESH_RIGID");

        // Write object name.
        w.WriteLine($"NAME,{mesh.Name}");

        // Loop through each vertex.
        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            // Write the position data.
            w.Write($"VERTEX,RIGID,{i},{mesh.Vertices[i].PositionX},{mesh.Vertices[i].PositionY},{mesh.Vertices[i].PositionZ},");
            // Write the normal data.
            w.Write($"{mesh.Vertices[i].NormalX},{mesh.Vertices[i].NormalY},{mesh.Vertices[i].NormalZ},");
            // Write the UV data.
            w.Write($"{mesh.Vertices[i].TexCoordX},{mesh.Vertices[i].TexCoordY}\n");
        }
        // Loop through each face index.
        for (int i = 0; i < mesh.Faces.Count; i++)
        {
            // Get the current face (for readability).
            var (A, B, C) = mesh.Faces[i];
            // Write three face indices (Format: pos/norm/uv).
            w.WriteLine($"FACE,{A + 1},{B + 1},{C + 1}");
        }
    }

    public void Export(InternalMesh mesh, InternalSkeleton skeleton, string path)
    {
        // Start writing to disk.
        using var w = new StreamWriter(File.OpenWrite(path + ".atf"));

        // Meta data.
        w.WriteLine("META,Exporter,IceBloc");
        w.WriteLine("TYPE,MESH_SKINNED");

        // Write object name.
        w.WriteLine($"NAME,{mesh.Name}");

        // Write a reference to our skeleton bind data.
        w.WriteLine($"FEATURE,EXTERNAL_BIND_DATA");

        // Loop through each vertex.
        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            // Write the position data.
            w.Write($"VERTEX,SKINNED,{i},{mesh.Vertices[i].PositionX},{mesh.Vertices[i].PositionY},{mesh.Vertices[i].PositionZ},");
            // Write the normal data.
            w.Write($"{mesh.Vertices[i].NormalX},{mesh.Vertices[i].NormalY},{mesh.Vertices[i].NormalZ},");
            // Write the UV data.
            w.Write($"{mesh.Vertices[i].TexCoordX},{mesh.Vertices[i].TexCoordY},");
            // Write the bone ID data.
            w.Write($"{mesh.Vertices[i].BoneIndexA},{mesh.Vertices[i].BoneIndexB},{mesh.Vertices[i].BoneIndexC},{mesh.Vertices[i].BoneIndexD},");
            // Write the bone weight data.
            w.Write($"{mesh.Vertices[i].TexCoordX},{mesh.Vertices[i].TexCoordY}\n");
        }
        // Loop through each face index.
        for (int i = 0; i < mesh.Faces.Count; i++)
        {
            // Get the current face (for readability).
            var f = mesh.Faces[i];
            // Write three face indices
            w.WriteLine($"FACE,{f.A + 1},{f.B + 1},{f.C + 1}");
        }

        // Write external skeleton bindings.
        using var sw = new StreamWriter(File.OpenWrite(path + "_bind.atf"));

        sw.WriteLine("META,Exporter,IceBloc");
        sw.WriteLine("TYPE,BIND_DATA");

    }
}