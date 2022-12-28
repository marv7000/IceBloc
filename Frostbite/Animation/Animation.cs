﻿using System.IO;

namespace IceBloc.Frostbite.Animation;

public class Animation
{
    public int CodecType;
    public int AnimId;
    public float TrimOffset;
    public ushort EndFrame;
    public bool Additive;

    public Animation()
    {

    }

    public Animation(Stream stream, ref GenericData gd)
    {
        using var r = new BinaryReader(stream);
        r.ReadGdDataHeader(out uint base_hash, out uint base_type, out uint base_baseOffset);

        var baseData = gd.ReadValues(r, base_baseOffset, base_type, false);

        CodecType = (int)baseData["CodecType"];
        AnimId = (int)baseData["AnimId"];
        TrimOffset = (float)baseData["TrimOffset"];
        EndFrame = (ushort)baseData["EndFrame"];
        Additive = (bool)baseData["Additive"];
    }
}