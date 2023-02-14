using IceBlocLib.InternalFormats;

namespace IceBlocLib.Export;

public class SkeletonExporterATF : ISkeletonExporter
{
    public void Export(InternalSkeleton skel, string path)
    {
        // Start writing to disk.
        using var w = new StreamWriter(File.OpenWrite(path + ".atf"));

        // Meta data.
        w.WriteLine("// Created by IceBloc");
        w.WriteLine("TYPE,SKELETON");

        // Write object name.
        w.WriteLine($"NAME,{skel.Name}");

        w.WriteLine("BONES");
        // Loop through each vertex.
        for (int i = 0; i < skel.BoneTransforms.Count; i++)
        {
            w.Write($"BONE,{i},{skel.BoneParents[i]},{skel.BoneNames[i]},");
            w.Write($"{skel.BoneTransforms[i].Position.X},{skel.BoneTransforms[i].Position.Y},{skel.BoneTransforms[i].Position.Z},");
            w.Write($"{skel.BoneTransforms[i].Rotation.X},{skel.BoneTransforms[i].Rotation.Y},{skel.BoneTransforms[i].Rotation.Z},{skel.BoneTransforms[i].Rotation.W},");
            w.Write($"{skel.LocalTransforms[i].Position.X},{skel.LocalTransforms[i].Position.Y},{skel.LocalTransforms[i].Position.Z},");
            w.Write($"{skel.LocalTransforms[i].Rotation.X},{skel.LocalTransforms[i].Rotation.Y},{skel.LocalTransforms[i].Rotation.Z},{skel.LocalTransforms[i].Rotation.W}\n");
        }
        w.WriteLine("END");
    }
}
