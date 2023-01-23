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

        // Loop through each vertex.
        for (int i = 0; i < skel.BoneTransforms.Count; i++)
        {
            // Type,Id,ParentId,Name,PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ,RotationW,ScaleX,ScaleY,ScaleZ
            w.Write($"BONE,{i},{skel.BoneParents[i]},{skel.BoneNames[i]},");
            w.Write($"{skel.BoneTransforms[i].Position.X},{skel.BoneTransforms[i].Position.Y},{skel.BoneTransforms[i].Position.Z},");
            w.Write($"{skel.BoneTransforms[i].Rotation.X},{skel.BoneTransforms[i].Rotation.Y},{skel.BoneTransforms[i].Rotation.Z},{skel.BoneTransforms[i].Rotation.W},");
            w.Write($"{skel.BoneTransforms[i].Scale.X},{skel.BoneTransforms[i].Scale.Y},{skel.BoneTransforms[i].Scale.Z}\n");
        }
    }
}
