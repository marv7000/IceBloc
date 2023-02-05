using IceBlocLib.InternalFormats;
using IceBlocLib.Utility;
using System.Numerics;

namespace IceBlocLib.Export;

public class AnimationExporterSMD : IAnimationExporter
{
    public void Export(InternalAnimation animation, InternalSkeleton skeleton, string path)
    {
        // Start writing to disk.
        Directory.CreateDirectory(path);
        using var w = new StreamWriter(File.OpenWrite(path + "\\" + animation.Name + ".smd"));

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

            w.WriteLine("skeleton");

            for (int x = 0; x < animation.Frames.Count; x++)
            {
                w.WriteLine($"time {x}");
                for (int i = 0; i < skeleton.BoneNames.Count; i++)
                {
                    var rotationIndex = animation.RotationChannels.IndexOf(skeleton.BoneNames[i]);
                    var positionIndex = animation.PositionChannels.IndexOf(skeleton.BoneNames[i]);

                    Vector3 rot = Vector3.Zero;
                    Vector3 pos = Vector3.Zero;

                    if (rotationIndex != -1)
                    {
                        var rotOffset = Transform.ToEulerAngles(skeleton.GetLocalTransform(i).Rotation);
                        rot = rotOffset + Transform.ToEulerAngles(animation.Frames[x].Rotations[rotationIndex]);
                    }
                    if (positionIndex != -1)
                    {
                        pos = 
                            skeleton.GetLocalTransform(i).Position + 
                            animation.Frames[x].Positions[positionIndex];
                    }

                    w.WriteLine($"{i} {pos.X} {pos.Y} {pos.Z} {rot.X.DegRad()} {rot.Y.DegRad()} {rot.Z.DegRad()}");
                }
            }
            w.WriteLine("end");
        }
    }

    
}
