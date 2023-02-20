using IceBlocLib.InternalFormats;
using System.Text;

namespace IceBlocLib.Utility.Export;

public class SoundExporterWAV : ISoundExporter
{
    public void Export(InternalSound sound, string path)
    {
        using var s = File.Open(path + ".wav", FileMode.Create);
        using var w = new BinaryWriter(s);

        // Convert to byte array.
        byte[] data = new byte[sound.Data.Length * 2];
        Buffer.BlockCopy(sound.Data, 0, data, 0, data.Length);

        w.Write(Encoding.ASCII.GetBytes("RIFF"));
        w.Write(0);
        w.Write(Encoding.ASCII.GetBytes("WAVE"));
        w.Write(Encoding.ASCII.GetBytes("fmt "));
        w.Write(16);
        w.Write((short)1);
        w.Write((short)sound.ChannelCount);
        w.Write((int)sound.SampleRate);
        w.Write(sound.SampleRate * 16 * sound.ChannelCount / 8);
        w.Write((short)(16 * sound.ChannelCount / 8));
        w.Write((short)16);
        w.Write(Encoding.ASCII.GetBytes("data"));
        w.Write(data.Length);
        w.Write(data);
        w.BaseStream.Position = 4;
        w.Write((uint)(w.BaseStream.Length - 8));
    }
}
