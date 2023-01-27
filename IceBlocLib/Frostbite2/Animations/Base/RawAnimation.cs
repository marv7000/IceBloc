﻿using IceBlocLib.InternalFormats;
using System;
using System.Numerics;

namespace IceBlocLib.Frostbite2.Animations.Base;

public class RawAnimation : Animation
{
    public int NumKeys;
    public int FloatCount;
    public int Vec3Count;
    public int QuatCount;
    public ushort[] KeyTimes;
    public float[] Data;
    public bool Cycle;

    public RawAnimation(Stream stream, int index, ref GenericData gd, bool bigEndian)
    {
        using var r = new BinaryReader(stream);
        r.ReadGdDataHeader(bigEndian, out uint hash, out uint type, out uint baseOffset);

        var data = gd.ReadValues(r, index, baseOffset, type, false);

        Name = (string)data["__name"];
        ID = (Guid)data["__guid"];
        NumKeys = (int)data["NumKeys"];
        FloatCount = (int)data["FloatCount"];
        Vec3Count = (int)data["Vec3Count"];
        QuatCount = (int)data["QuatCount"];
        KeyTimes = data["KeyTimes"] as ushort[];
        Data = data["Data"] as float[];
        Cycle = (bool)data["Cycle"];

        // Read the Base class (Animation).
        r.BaseStream.Position = (long)data["__base"];
        r.ReadGdDataHeader(bigEndian, out uint base_hash, out uint base_type, out uint base_baseOffset);

        var baseData = gd.ReadValues(r, index, (uint)((long)data["__base"] + base_baseOffset), base_type, false);

        CodecType = (int)baseData["CodecType"];
        AnimId = (int)baseData["AnimId"];
        TrimOffset = (float)baseData["TrimOffset"];
        EndFrame = (ushort)baseData["EndFrame"];
        Additive = (bool)baseData["Additive"];
        ChannelToDofAsset = (Guid)baseData["ChannelToDofAsset"];
        Channels = GetChannels(ChannelToDofAsset);
    }

    public InternalAnimation ConvertToInternal()
    {
        InternalAnimation ret = new InternalAnimation();
        ret.Name = Name;

        return ret;
    }
}
