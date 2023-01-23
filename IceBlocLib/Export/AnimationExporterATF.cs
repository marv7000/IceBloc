using IceBlocLib.InternalFormats;

namespace IceBlocLib.Export;

public class AnimationExporterATF : IAnimationExporter
{
    public void Export(InternalAnimation animation, string path)
    {
        // Start writing to disk.
        using var w = new StreamWriter(File.OpenWrite(path + "_" + animation.Name + ".atf"));

        // Meta data.
        w.WriteLine("// Created by IceBloc");
        w.WriteLine("TYPE,ANIMATION");
        w.WriteLine($"NAME,{animation.Name}");
        w.WriteLine("// Format: Type,Channel,FrameIndex,BoneIndex,Value");
        for (int i = 0; i < animation.Frames.Length; i++)
        {
            for (int k = 0; k < animation.Frames[i].Positions.Length; k++)
            {
                w.Write($"KEY,POSX,{animation.Frames[i].FrameIndex},{k},{animation.Frames[i].Positions[k].X}\n");
                w.Write($"KEY,POSY,{animation.Frames[i].FrameIndex},{k},{animation.Frames[i].Positions[k].Y}\n");
                w.Write($"KEY,POSZ,{animation.Frames[i].FrameIndex},{k},{animation.Frames[i].Positions[k].Z}\n");
            }
            for (int k = 0; k < animation.Frames[i].Rotations.Length; k++)
            {
                w.Write($"KEY,ROTX,{animation.Frames[i].FrameIndex},{k},{animation.Frames[i].Rotations[k].X}\n");
                w.Write($"KEY,ROTY,{animation.Frames[i].FrameIndex},{k},{animation.Frames[i].Rotations[k].Y}\n");
                w.Write($"KEY,ROTZ,{animation.Frames[i].FrameIndex},{k},{animation.Frames[i].Rotations[k].Z}\n");
                w.Write($"KEY,ROTW,{animation.Frames[i].FrameIndex},{k},{animation.Frames[i].Rotations[k].W}\n");
            }
        }
    }

    public void Export(InternalAnimation animation, InternalSkeleton skeleton, string path)
    {
        throw new NotImplementedException();
    }
}
