using System.IO;
using IceBlocLib.InternalFormats;
using IceBlocLib.Utility;

namespace IceBlocLib.Export;

/// <summary>
/// Exports <see cref="InternalMesh"/> objects to the .SMD format.
/// </summary>
public class ModelExporterSMD : IModelExporter
{
    /// <inheritdoc/>
    public void Export(InternalMesh mesh, string path)
    {
        Export(mesh, null, path);
    }

    public void Export(InternalMesh mesh, InternalSkeleton skeleton, string path)
    {
        // Start writing to disk.
        using var w = new StreamWriter(File.OpenWrite(path + ".smd"));

        // Write version header.
        w.WriteLine("version 1");

        // Export any nodes.
        if (skeleton != null)
        {
            w.WriteLine("nodes");

            for (int i = 0; i < skeleton.BoneTransforms.Count; i++)
            {
                w.WriteLine($"{i} \"{skeleton.BoneNames[i]}\" {skeleton.BoneParents[i]}");
            }

            w.WriteLine("end");

            w.WriteLine("skeleton\ntime 0");

            for (int i = 0; i < skeleton.BoneTransforms.Count; i++)
            {
                var pos = skeleton.LocalTransforms[i].Position;
                var rot = Transform.ToEulerAngles(skeleton.LocalTransforms[i].Rotation);
                w.WriteLine($"{i} {pos.X} {pos.Y} {pos.Z} {rot.X} {rot.Y} {rot.Z}");
            }

            w.WriteLine("end");
        }
        else
        {
            w.WriteLine("nodes");

            w.WriteLine($"0 \"Root\" -1");

            w.WriteLine("end");
            w.WriteLine("skeleton\ntime 0");

            w.WriteLine("0 0 0 0 0 0 0");

            w.WriteLine("end");
        }

        w.WriteLine("triangles");
        // Loop through each vertex.
        for (int i = 0; i < mesh.Faces.Count; i++)
        {
            w.WriteLine(mesh.Name);
            // Get the current face.
            var f = new int[] { mesh.Faces[i].A, mesh.Faces[i].B, mesh.Faces[i].C };

            for (int j = 0; j < 3; j++)
            {
                w.Write($"{mesh.Vertices[f[j]].BoneIndexA} {mesh.Vertices[f[j]].PositionX} {mesh.Vertices[f[j]].PositionY} {mesh.Vertices[f[j]].PositionZ} ");
                w.Write($"{mesh.Vertices[f[j]].NormalX} {mesh.Vertices[f[j]].NormalY} {mesh.Vertices[f[j]].NormalZ} ");
                w.Write($"{mesh.Vertices[f[j]].TexCoordX} {mesh.Vertices[f[j]].TexCoordY} ");
                w.Write($"4 {mesh.Vertices[f[j]].BoneIndexA} {mesh.Vertices[f[j]].BoneWeightA} "); // 4 Weights
                w.Write($"{mesh.Vertices[f[j]].BoneIndexB} {mesh.Vertices[f[j]].BoneWeightB} ");
                w.Write($"{mesh.Vertices[f[j]].BoneIndexC} {mesh.Vertices[f[j]].BoneWeightC} ");
                w.Write($"{mesh.Vertices[f[j]].BoneIndexD} {mesh.Vertices[f[j]].BoneWeightD}\n");
            }
        }
        w.WriteLine("end");
    }
}