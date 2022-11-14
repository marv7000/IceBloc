using System.IO;
using IceBloc.InternalFormats;

namespace IceBloc.Export;

/// <summary>
/// Exports <see cref="InternalMesh"/> objects to the .OBJ format.
/// </summary>
public class ModelExporterOBJ : IModelExporter
{
    /// <inheritdoc/>
    public void Export(InternalMesh mesh, string path)
    {
        // Start writing to disk.
        using var w = new StreamWriter(File.OpenWrite(path));

        // Write object name.
        w.WriteLine($"o {mesh.Name}");

        // Loop through each vertex.
        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            // Write the position data.
            w.WriteLine($"v {mesh.Vertices[i].PositionX} {mesh.Vertices[i].PositionY} {mesh.Vertices[i].PositionZ}");
            // Write the normal data.
            w.WriteLine($"vn {mesh.Vertices[i].NormalX} {mesh.Vertices[i].NormalY} {mesh.Vertices[i].NormalZ}");
            // Write the UV data.
            w.WriteLine($"vt {mesh.Vertices[i].TexCoordX} {mesh.Vertices[i].TexCoordY}");
        }
        // Loop through each face index.
        for (int i = 0; i < mesh.Faces.Count; i++)
        {
            // Get the current face (for readability).
            var f = mesh.Faces[i];
            // Write three face indices (Format: pos/norm/uv).
            w.WriteLine($"f {f.A + 1}/{f.A + 1}/{f.A + 1} {f.B + 1}/{f.B + 1}/{f.B + 1} {f.C + 1}/{f.C + 1}/{f.C + 1}");
        }
    }
}