using IceBlocLib.InternalFormats;
using System.Numerics;

namespace IceBlocLib.Export;

public class AnimationExporterATF : IAnimationExporter
{
    public void Export(InternalAnimation animation, string path)
    {
        // Start writing to disk.
        Directory.CreateDirectory(path);
        using var w = new StreamWriter(File.OpenWrite(path + "\\" + animation.Name + ".atf"));

        // Meta data.
        w.WriteLine("// Created by IceBloc");
        w.WriteLine("TYPE,ANIMATION");
        w.WriteLine($"NAME,{animation.Name}");
        w.WriteLine("// Format: Type,Channel,FrameIndex,BoneName,Value");
        for (int i = 0; i < animation.Frames.Count; i++)
        {
            for (int k = 0; k < animation.Frames[i].Positions.Count; k++)
            {
                Vector3 posChannel = animation.Frames[i].Positions[k];
                string channelName = animation.PositionChannels[k];
                w.Write($"KEY,POSX,{animation.Frames[i].FrameIndex},{channelName},{posChannel.X}\n");
                w.Write($"KEY,POSY,{animation.Frames[i].FrameIndex},{channelName},{posChannel.Y}\n");
                w.Write($"KEY,POSZ,{animation.Frames[i].FrameIndex},{channelName},{posChannel.Z}\n");
            }
            for (int k = 0; k < animation.Frames[i].Rotations.Count; k++)
            {
                Quaternion rotChannel = animation.Frames[i].Rotations[k];
                string channelName = animation.RotationChannels[k];
                w.Write($"KEY,ROTX,{animation.Frames[i].FrameIndex},{channelName},{rotChannel.X}\n");
                w.Write($"KEY,ROTY,{animation.Frames[i].FrameIndex},{channelName},{rotChannel.Y}\n");
                w.Write($"KEY,ROTZ,{animation.Frames[i].FrameIndex},{channelName},{rotChannel.Z}\n");
                w.Write($"KEY,ROTW,{animation.Frames[i].FrameIndex},{channelName},{rotChannel.W}\n");
            }
        }
    }

    public void Export(InternalAnimation animation, InternalSkeleton skeleton, string path)
    {
        throw new NotImplementedException();
    }
}
