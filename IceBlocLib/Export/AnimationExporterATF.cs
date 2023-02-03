using IceBlocLib.InternalFormats;
using System.Numerics;

namespace IceBlocLib.Export;

public class AnimationExporterATF : IAnimationExporter
{
    public void Export(InternalAnimation animation, string path)
    {
        // Start writing to disk.
        Directory.CreateDirectory(path);
        using var w = new StreamWriter(File.Open(path + "\\" + animation.Name + ".atf", FileMode.Create));

        // Meta data.
        w.WriteLine("// Created by IceBloc");
        w.WriteLine("TYPE,ANIMATION");
        w.WriteLine($"NAME,{animation.Name}");
        w.WriteLine($"ADDITIVE,{(animation.Additive == true ? "TRUE" : "FALSE")}");
        for (int i = 0; i < animation.Frames.Count; i++)
        {
            for (int k = 0; k < animation.Frames[i].Rotations.Count; k++)
            {
                Quaternion rotChannel = animation.Frames[i].Rotations[k];
                string channelName = animation.RotationChannels[k];
                w.Write($"KEY,ROT,{animation.Frames[i].FrameIndex},{channelName},{rotChannel.X},{rotChannel.Y},{rotChannel.Z},{rotChannel.W}\n");
            }
            for (int k = 0; k < animation.Frames[i].Positions.Count; k++)
            {
                Vector3 posChannel = animation.Frames[i].Positions[k];
                string channelName = animation.PositionChannels[k];
                w.Write($"KEY,POS,{animation.Frames[i].FrameIndex},{channelName},{posChannel.X},{posChannel.Y},{posChannel.Z}\n");
            }
        }
    }

    public void Export(InternalAnimation animation, InternalSkeleton skeleton, string path)
    {
        throw new NotImplementedException();
    }
}
