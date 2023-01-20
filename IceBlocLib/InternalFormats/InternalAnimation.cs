using System.Numerics;

namespace IceBlocLib.InternalFormats;

public sealed class InternalAnimation
{
    public Frame[] Frames;

    public struct Frame
    {
        public int FrameIndex;
        public Vector3[] Positions;
        public Quaternion[] Rotations;
    }
}
