using System.Numerics;

namespace IceBlocLib.InternalFormats;

public sealed class InternalAnimation
{
    public string Name = "";
    public Frame[] Frames = new Frame[0];

    public struct Frame
    {
        public int FrameIndex;
        public Vector3[] Positions;
        public Quaternion[] Rotations;
    }
}
