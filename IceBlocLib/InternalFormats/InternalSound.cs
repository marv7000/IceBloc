namespace IceBlocLib.InternalFormats;

public class InternalSound
{
    public uint LoopStart;
    public uint LoopEnd;
    public ushort SampleRate;
    public int ChannelCount;
    public double Length;
    public short[] Data;

    public InternalSound()
    {
    }
}
