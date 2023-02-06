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
        using var w = new StreamWriter(File.Open(path + "\\" + animation.Name + ".smd", FileMode.Create));

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

            for (int x = 0; x < animation.Frames.Count; x++)
            {
                if (animation.Frames.Count >= 2 && x >= 1)
                {
                    for (int y = 0; y < animation.Frames[x].FrameIndex - animation.Frames[x - 1].FrameIndex; y++)
                    {
                        w.WriteLine($"time {animation.Frames[x - 1].FrameIndex + y + 1}");
                    }
                }

                for (int i = 0; i < skeleton.BoneNames.Count; i++)
                {
                    var rotationIndex = animation.RotationChannels.IndexOf(skeleton.BoneNames[i]);
                    var positionIndex = animation.PositionChannels.IndexOf(skeleton.BoneNames[i]);

                    Matrix4x4 mat = Matrix4x4.Identity; 
                    var mParent = Matrix4x4.Identity;
                    if (rotationIndex > 0) mParent = skeleton.BoneTransforms[skeleton.BoneParents[i]].Matrix;
                    var mBone = skeleton.BoneTransforms[i].Matrix;
                    var lBone = skeleton.LocalTransforms[i].Matrix;

                    Matrix4x4.Invert(mParent * mBone, out var invertMat);

                    mat *= lBone;
                    mat *= invertMat;

                    if (rotationIndex != -1)
                    {
                        var aRot = Matrix4x4.CreateFromQuaternion(animation.Frames[x].Rotations[rotationIndex]);
                        mat *= aRot;
                    }
                    if (positionIndex != -1)
                    {
                        var aPos = Matrix4x4.CreateTranslation(animation.Frames[x].Positions[positionIndex]);
                        mat *= aPos;
                    }

                    var t = new Transform(mat);
                    var pos = t.Position;
                    var rot = t.EulerAngles;

                    w.WriteLine($"{i} {pos.X} {pos.Y} {pos.Z} {rot.X.DegRad()} {rot.Y.DegRad()} {rot.Z.DegRad()}");
                }
            }
            w.WriteLine("end");
        }
    }
}
