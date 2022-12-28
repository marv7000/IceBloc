using System.IO;

namespace IceBloc.Frostbite.Animation;

public class DctAnimation : Animation
{
    public DctAnimation(Stream stream, ref GenericData gd)
    {
        using var r = new BinaryReader(stream);
        r.ReadGdDataHeader(out uint hash, out uint type, out uint baseOffset);
    }
}
