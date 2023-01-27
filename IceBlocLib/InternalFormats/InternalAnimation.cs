using System.Numerics;

namespace IceBlocLib.InternalFormats;

public sealed class InternalAnimation
{
    public string Name = "";
    public List<Frame> Frames = new();
    public List<string> RotationChannels = new();
    public List<string> PositionChannels = new();

    public struct Frame
    {
        public int FrameIndex = 0;
        public List<Vector3> Positions = new();
        public List<Quaternion> Rotations = new();

        public Frame(int frameIndex, List<Vector3> positions, List<Quaternion> rotations)
        {
            FrameIndex = frameIndex;
            Positions = positions;
            Rotations = rotations;
        }
    }
}
