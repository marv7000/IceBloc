namespace IceBlocLib.InternalFormats;

public class InternalSound
{
    public uint LoopStart;
    public uint SampleCount;
    public ushort SampleRate;
    public int ChannelCount;
    public double Length;
    public short[] Data;

    public InternalSound()
    {
    }
}
