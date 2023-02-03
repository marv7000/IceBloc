using IceBlocLib.Frostbite2.Animations.DCT;
using IceBlocLib.Frostbite2.Animations.Misc;
using IceBlocLib.InternalFormats;
using System.Numerics;
using static IceBlocLib.InternalFormats.InternalAnimation;
using IceBlocLib.Frostbite;
using IceBlocLib.Utility;
using System.Reflection.Metadata.Ecma335;

namespace IceBlocLib.Frostbite2.Animations.Base;

public class DctAnimation : Animation
{
    public ushort[] KeyTimes = new ushort[0];
    public byte[] Data = new byte[0];
    public ushort NumKeys;
    public ushort NumVec3;
    public ushort NumFloat;
    public int DataSize;
    public bool Cycle;

    public ushort NumQuats;
    public ushort NumFloatVec;
    public ushort QuantizeMultBlock;
    public byte QuantizeMultSubblock;
    public byte CatchAllBitCount;
    public byte[] DofTableDescBytes;
    public short[] DeltaBaseX;
    public short[] DeltaBaseY;
    public short[] DeltaBaseZ;
    public short[] DeltaBaseW;
    public ushort[] BitsPerSubblock;

    private uint TotalSubBlocks = 0;
    private DofTable[] Tables = new DofTable[0];
    private List<Vector4> DecompressedData = new();
    private uint NextBitOffset = 0;

    // TODO Remove when done with testing.
    private uint a = 0;

    public DctAnimation(Stream stream, int index, ref GenericData gd, bool bigEndian)
    {
        using var r = new BinaryReader(stream);
        r.ReadGdDataHeader(bigEndian, out uint hash, out uint type, out uint baseOffset);

        var data = gd.ReadValues(r, index, baseOffset, type, false);

        Name = (string)data["__name"];
        ID = (Guid)data["__guid"];
        KeyTimes = data["KeyTimes"] as ushort[];
        Data = data["Data"] as byte[];
        NumKeys = (ushort)data["NumKeys"];
        NumVec3 = (ushort)data["NumVec3"];
        NumFloat = (ushort)data["NumFloat"];
        DataSize = (int)data["DataSize"];
        Cycle = (bool)data["Cycle"];

        NumQuats = (ushort)data["NumQuats"];
        NumFloatVec = (ushort)data["NumFloatVec"];
        QuantizeMultBlock = (ushort)data["QuantizeMultBlock"];
        QuantizeMultSubblock = (byte)data["QuantizeMultSubblock"];
        CatchAllBitCount = (byte)data["CatchAllBitCount"];

        DofTableDescBytes = data["DofTableDescBytes"] as byte[];

        DeltaBaseX = data["DeltaBaseX"] as short[];
        DeltaBaseY = data["DeltaBaseY"] as short[];
        DeltaBaseZ = data["DeltaBaseZ"] as short[];
        DeltaBaseW = data["DeltaBaseW"] as short[];
        BitsPerSubblock = data["BitsPerSubblock"] as ushort[];

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

        // Decompress the animation.
        Decompress();
    }

    public InternalAnimation ConvertToInternal()
    {
        InternalAnimation ret = new();

        List<string> posChannels = new();
        List<string> rotChannels = new();

        // Get all names.
        for (int i = 0; i < Channels.Length; i++)
        {
            if (Channels[i].EndsWith(".q"))
                rotChannels.Add(Channels[i].Replace(".q", ""));
            else if (Channels[i].EndsWith(".t"))
                posChannels.Add(Channels[i].Replace(".t", ""));
        }

        // Assign values to Channels.
        for (int i = 0; i < KeyTimes.Length; i++)
        {
            Frame frame = new Frame();

            for (int channelIdx = 0; channelIdx < rotChannels.Count; channelIdx++)
            {
                int pos = (int)(i * GetNumTableEntriesPerFrame() + channelIdx);
                Vector4 element = DecompressedData[pos];
                frame.Rotations.Add(Quaternion.Normalize(new Quaternion(element.X, element.Y, element.Z, element.W)));
            }
            for (int channelIdx = 0; channelIdx < posChannels.Count; channelIdx++)
            {
                int pos = (int)(i * GetNumTableEntriesPerFrame() + NumQuats + channelIdx);
                Vector4 element = DecompressedData[pos];
                frame.Positions.Add(new Vector3(element.X, element.Y, element.Z));
            }
            ret.Frames.Add(frame);
        }

        for (int i = 0; i < KeyTimes.Length - KeyTimes.Length % 8; i++)
        {
            Frame f = ret.Frames[i];
            f.FrameIndex = KeyTimes[i];
            ret.Frames[i] = f;
        }

        ret.Name = Name;
        ret.PositionChannels = posChannels;
        ret.RotationChannels = rotChannels;
        ret.Additive = Additive;
        return ret;
    }

    #region DCT Decompression

    public static float[] Dct3Coeff = new float[64] {
             0.250000f,  0.490393f,  0.461940f,  0.415735f,  0.353553f,  0.277785f,  0.191342f,  0.097545f,
             0.250000f,  0.415735f,  0.191342f, -0.097545f, -0.353553f, -0.490393f, -0.461940f, -0.277785f,
             0.250000f,  0.277785f, -0.191342f, -0.490393f, -0.353553f,  0.097545f,  0.461940f,  0.415735f,
             0.250000f,  0.097545f, -0.461940f, -0.277785f,  0.353553f,  0.415735f, -0.191342f, -0.490393f,
             0.250000f, -0.097545f, -0.461940f,  0.277785f,  0.353553f, -0.415735f, -0.191342f,  0.490393f,
             0.250000f, -0.277785f, -0.191342f,  0.490393f, -0.353553f, -0.097545f,  0.461940f, -0.415735f,
             0.250000f, -0.415735f,  0.191342f,  0.097545f, -0.353553f,  0.490393f, -0.461940f,  0.277785f,
             0.250000f, -0.490393f,  0.461940f, -0.415735f,  0.353554f, -0.277785f,  0.191342f, -0.097545f,
        };

    public void Decompress()
    {
        BuildDofTables();

        for (ushort i = 0; i < NumKeys / 8 + NumKeys % 8; i++)
        {
            NextBitOffset = Decompress_NextColumn(NextBitOffset);
        }
    }
    private void BuildDofTables()
    {
        uint dofCount = GetNumTableEntriesPerFrame();
        Tables = new DofTable[dofCount];

        for (int i = 0; i < dofCount; i++)
        {
            var subBlockCount = (byte)((DofTableDescBytes[i] >> 4) & 0xF);

            var table = new DofTable(subBlockCount);
            table.DeltaBase = new short[4]
            {
                DeltaBaseX[i],
                DeltaBaseY[i],
                DeltaBaseZ[i],
                DeltaBaseW[i],
            };

            table.BitsPerSubBlock = new DofTable.BitsPerComponent[table.SubBlockCount];

            for (var j = 0; j < table.SubBlockCount; j++)
            {
                table.BitsPerSubBlock[j] = new DofTable.BitsPerComponent(BitsPerSubblock[TotalSubBlocks + j]);
            }

            Tables[i] = table;
            TotalSubBlocks += subBlockCount;
        }
    }
    public uint Decompress_NextColumn(uint bitOffset)
    {
        var bitstream = new BitReader(new MemoryStream(Data), true);
        bitstream.Position = bitOffset;
        uint offset = bitOffset;

        var output = new MemoryStream(DataSize * 16);

        CombineUnquantizeDCT3_N8((float)QuantizeMultBlock, (float)QuantizeMultSubblock * 0.1f, out var dctTable);

        for (uint x = 0; x < NumQuats; x++)
        {
            var dofTable = Tables[x];

            MemoryStream quatStream = new();
            UnpackV4Block(ref bitstream, offset, dofTable, ref quatStream);
            var vec = GetDeltaFromStream(ref quatStream);
            
            vec[0][0] += dofTable.DeltaBase[0];
            vec[0][1] += dofTable.DeltaBase[1];
            vec[0][2] += dofTable.DeltaBase[2];
            vec[0][3] += dofTable.DeltaBase[3];

            List<short> shorts = new();

            for (int i = 0; i < 8; i++)
                shorts.AddRange(vec[i].AsShortList());

            Unquantize_TransformDct3_N8(shorts, ref output, in dctTable);
            output.Position += 16 * 8; // sizeof(__m128) * blockSize
        }

        for (uint x = 0; x < NumVec3; x++)
        {
            var dofTable = Tables[NumQuats + x];

            MemoryStream vecStream = new();
            UnpackV4Block(ref bitstream, bitOffset, dofTable, ref vecStream);
            var vec = GetDeltaFromStream(ref vecStream);
            vec[0][0] += dofTable.DeltaBase[0];
            vec[0][1] += dofTable.DeltaBase[1];
            vec[0][2] += dofTable.DeltaBase[2];
            vec[0][3] += dofTable.DeltaBase[3];

            List<short> shorts = new();

            for (int i = 0; i < 8; i++)
                shorts.AddRange(vec[i].AsShortList());

            Unquantize_TransformDct3_N8(shorts, ref output, in dctTable);
            output.Position += 16 * 8;
        }

        for (uint x = 0; x < NumFloatVec; x++)
        {
            var dofTable = Tables[NumQuats + NumVec3 + x];

            MemoryStream floatStream = new();
            UnpackV4Block(ref bitstream, bitOffset, dofTable, ref floatStream);
            var vec = GetDeltaFromStream(ref floatStream);
            vec[0][0] += dofTable.DeltaBase[0];
            vec[0][1] += dofTable.DeltaBase[1];
            vec[0][2] += dofTable.DeltaBase[2];
            vec[0][3] += dofTable.DeltaBase[3];

            List<short> shorts = new();

            for (int i = 0; i < 8; i++)
                shorts.AddRange(vec[i].AsShortList());

            Unquantize_TransformDct3_N8(shorts, ref output, in dctTable);
            output.Position += 16 * 8;
        }

        // Write everything to DecompressedData.
        var r = new BinaryReader(output);
        output.Position = 0;

        // 8 Frames per block.
        List<Vector4> temp = new();
        for (int i = 0; i < GetNumTableEntriesPerFrame() * 8; i++)
        {
            // Read Vec.
            var x = r.ReadSingle();
            var y = r.ReadSingle();
            var z = r.ReadSingle();
            var w = r.ReadSingle();

            temp.Add(new Vector4(x, y, z, w));
        }
        // Reorder.
        for (int i = 0; i < 8; i++)
        {
            for (int k = 0; k < GetNumTableEntriesPerFrame(); k++)
            {
                DecompressedData.Add(temp[i + (k * 8)]);
            }
        }

        File.WriteAllBytes($@"D:\test\{Name}_Block_{a++}", output.ToArray());

        return (uint)bitstream.Position;
    }
    public void UnpackV4Block(ref BitReader r, uint whichBlock, DofTable dofTable, ref MemoryStream data)
    {
        List<short> block = new();

        byte[] componentBits = new byte[4];

        ushort subBlock = 0;

        if (0 == whichBlock)
        {
            block.Add(0);
            block.Add(0);
            block.Add(0);
            block.Add(0);

            subBlock = 1;
        }

        for (; subBlock < dofTable.SubBlockCount; subBlock++)
        {
            componentBits[0] = (byte)dofTable.BitsPerSubBlock[subBlock].SafeBitsX(CatchAllBitCount);
            componentBits[1] = (byte)dofTable.BitsPerSubBlock[subBlock].SafeBitsY(CatchAllBitCount);
            componentBits[2] = (byte)dofTable.BitsPerSubBlock[subBlock].SafeBitsZ(CatchAllBitCount);
            componentBits[3] = (byte)dofTable.BitsPerSubBlock[subBlock].SafeBitsW(CatchAllBitCount);

            block.Add(ExtractComponent(ref r, componentBits[0]));
            block.Add(ExtractComponent(ref r, componentBits[1]));
            block.Add(ExtractComponent(ref r, componentBits[2]));
            block.Add(ExtractComponent(ref r, componentBits[3]));
        }

        for (; subBlock < 8; subBlock++)
        {
            block.Add(0);
            block.Add(0);
            block.Add(0);
            block.Add(0);
        }

        // Convert short array to MemoryStream.
        data.Position = 0;
        using var w = new BinaryWriter(data, System.Text.Encoding.ASCII, true);
        foreach (var sh in block)
        {
            w.Write(sh);
        }
        data.Position = 0;
    }
    #endregion

    #region DCT Helpers

    /// <summary>
    /// Convert 32 shorts from the stream to a Vector4i.
    /// </summary>
    private Vector4i[] GetDeltaFromStream(ref MemoryStream stream)
    {
        using var r = new BinaryReader(stream, System.Text.Encoding.ASCII, true);

        Vector4i[] ret = new Vector4i[8];

        stream.Position = 0;

        var x = r.ReadInt16();
        var y = r.ReadInt16();
        var z = r.ReadInt16();
        var w = r.ReadInt16();
        ret[0] = new Vector4i(x, y, z, w);

        for (int i = 1; i < 8; i++)
        {
            x = r.ReadInt16();
            y = r.ReadInt16();
            z = r.ReadInt16();
            w = r.ReadInt16();
            ret[i] = new Vector4i(x, y, z, w);
        }

        stream.Position = 0;

        return ret;
    }

    public void CombineUnquantizeDCT3_N8(float BlockMultiplier, float SubblockMultiplier, out Vector4[,] unquantizeDCT3Combined)
    {
        unquantizeDCT3Combined = new Vector4[8, 8];

        CombineUnquantized_Dct3_N8(0, SubblockMultiplier, BlockMultiplier, ref unquantizeDCT3Combined);
        CombineUnquantized_Dct3_N8(1, SubblockMultiplier, BlockMultiplier, ref unquantizeDCT3Combined);
        CombineUnquantized_Dct3_N8(2, SubblockMultiplier, BlockMultiplier, ref unquantizeDCT3Combined);
        CombineUnquantized_Dct3_N8(3, SubblockMultiplier, BlockMultiplier, ref unquantizeDCT3Combined);
        CombineUnquantized_Dct3_N8(4, SubblockMultiplier, BlockMultiplier, ref unquantizeDCT3Combined);
        CombineUnquantized_Dct3_N8(5, SubblockMultiplier, BlockMultiplier, ref unquantizeDCT3Combined);
        CombineUnquantized_Dct3_N8(6, SubblockMultiplier, BlockMultiplier, ref unquantizeDCT3Combined);
        CombineUnquantized_Dct3_N8(7, SubblockMultiplier, BlockMultiplier, ref unquantizeDCT3Combined);
    }

    public void Unquantize_TransformDct3_N8(List<short> Src, ref MemoryStream Dest, in Vector4[,] unquantizeDCT3Combined)
    {
        Vector4[] srcVec = new Vector4[8]
        {
            new Vector4(Src[ 0], Src[ 1], Src[ 2], Src[ 3]),
            new Vector4(Src[ 4], Src[ 5], Src[ 6], Src[ 7]),
            new Vector4(Src[ 8], Src[ 9], Src[10], Src[11]),
            new Vector4(Src[12], Src[13], Src[14], Src[15]),
            new Vector4(Src[16], Src[17], Src[18], Src[19]),
            new Vector4(Src[20], Src[21], Src[22], Src[23]),
            new Vector4(Src[24], Src[25], Src[26], Src[27]),
            new Vector4(Src[28], Src[29], Src[30], Src[31]),
        };
        
        Unquantize_TransformDct3_N8(srcVec, ref Dest, in unquantizeDCT3Combined);
    }

    public void Unquantize_TransformDct3_N8(Vector4[] Src, ref MemoryStream Dest, in Vector4[,] dctCombined)
    {
        using var w = new BinaryWriter(Dest, System.Text.Encoding.ASCII, true);

        var pos = Dest.Position;

        for (int i = 0; i < 8; i++)
        {
            Vector4 vec = Unquantize_Dct3_N8(Src, in dctCombined, i);
            w.Write(vec);
        }

        Dest.Position = pos;
    }

    private Vector4 Unquantize_Dct3_N8(Vector4[] Src, in Vector4[,] dct3Combined, int k)
    {
        Vector4 var = Src[0] * dct3Combined[k,0];
        var += Src[1] * dct3Combined[k,1];
        var += Src[2] * dct3Combined[k,2];
        var += Src[3] * dct3Combined[k,3];
        var += Src[4] * dct3Combined[k,4];
        var += Src[5] * dct3Combined[k,5];
        var += Src[6] * dct3Combined[k,6];
        var += Src[7] * dct3Combined[k,7];

        return var;
    }

    private void CombineUnquantized_Dct3_N8(int k, float SubblockMultiplier, float BlockMultiplier, ref Vector4[,] unquantizeDCT3Combined)
    {
        float quantize = (1.0f + SubblockMultiplier * (float)k) / BlockMultiplier;

        for (int i = 0; i < 8; i++)
        {
            unquantizeDCT3Combined[i, k] = quantize * new Vector4(Dct3Coeff[(i << 3) + k]);
        }
    }

    private uint SignExtend(uint src, byte srcBitCount)
    {
        return ((src) << (32 - (srcBitCount))) >> (32 - (srcBitCount));
    }
    private short ExtractComponent(ref BitReader r, byte component)
    {
        if (component != 0)
        {
            return (short)SignExtend((uint)r.ReadBits(component), component);
        }
        else
        {
            return 0;
        }
    }
    private bool HasNoCatchAllExceptions()
    {
        return CatchAllBitCount <= 0xF;
    }
    private uint GetNumTableEntriesPerFrame()
    {
        return (uint)(NumQuats + NumVec3 + NumFloatVec);
    }
    private uint GetBitLength_Column0Subblock0()
    {
        if (HasNoCatchAllExceptions())
            return GetBitLength_Column0Subblock0_NoExceptions();
        else
            return GetBitLength_Column0Subblock0_WithExceptions();
    }
    private uint GetColumnBitLength()
    {
        if (HasNoCatchAllExceptions())
            return GetColumnBitLength_NoExceptions();
        else
            return GetColumnBitLength_WithExceptions();
    }
    private uint GetBitLength_Column0Subblock0_NoExceptions()
    {
        uint Column0BitCount = 0;
        uint NumTableEntries = GetNumTableEntriesPerFrame();

        for (ushort Entry = 0; Entry<NumTableEntries; Entry++)
        {
            DofTable dofTable = Tables[Entry];
            if (Tables[Entry].SubBlockCount != 0)
            {
                Column0BitCount += (uint)(dofTable.BitsPerSubBlock[0].BitsX + dofTable.BitsPerSubBlock[0].BitsY + dofTable.BitsPerSubBlock[0].BitsZ + dofTable.BitsPerSubBlock[0].BitsW);
            }
        }

        return Column0BitCount;
    }
    private uint GetBitLength_Column0Subblock0_WithExceptions()
    {
        uint Column0BitCount = 0;
        uint NumTableEntries = GetNumTableEntriesPerFrame();

        for (ushort Entry = 0; Entry < NumTableEntries; Entry++)
        {
            DofTable dofTable = Tables[Entry];
            if (Tables[Entry].SubBlockCount != 0)
            {
                Column0BitCount += (uint) (
                        dofTable.BitsPerSubBlock[0].SafeBitsX(CatchAllBitCount) +
                        dofTable.BitsPerSubBlock[0].SafeBitsY(CatchAllBitCount) +
                        dofTable.BitsPerSubBlock[0].SafeBitsZ(CatchAllBitCount) +
                        dofTable.BitsPerSubBlock[0].SafeBitsW(CatchAllBitCount)
                    );
            }
        }

        return Column0BitCount;
    }
    private uint GetColumnBitLength_NoExceptions()
    {
        uint Column0BitCount = 0;
        uint NumTableEntries = GetNumTableEntriesPerFrame();

        for (ushort Entry = 0; Entry < NumTableEntries; Entry++)
        {
            DofTable dofTable = Tables[Entry];
            for (ushort subBlock = 0; subBlock < Tables[Entry].SubBlockCount; subBlock++)
            {
                Column0BitCount += (uint)(dofTable.BitsPerSubBlock[subBlock].BitsX + dofTable.BitsPerSubBlock[subBlock].BitsY + dofTable.BitsPerSubBlock[subBlock].BitsZ + dofTable.BitsPerSubBlock[subBlock].BitsW);
            }
        }

        return Column0BitCount;
    }
    private uint GetColumnBitLength_WithExceptions()
    {
        uint Column0BitCount = 0;
        uint NumTableEntries = GetNumTableEntriesPerFrame();

        for (ushort Entry = 0; Entry < NumTableEntries; Entry++)
        {
            DofTable dofTable = Tables[Entry];
            for (ushort subBlock = 0; subBlock < Tables[Entry].SubBlockCount; subBlock++)
            {
                Column0BitCount += (uint)(
                        dofTable.BitsPerSubBlock[subBlock].SafeBitsX(CatchAllBitCount) +
                        dofTable.BitsPerSubBlock[subBlock].SafeBitsY(CatchAllBitCount) +
                        dofTable.BitsPerSubBlock[subBlock].SafeBitsZ(CatchAllBitCount) +
                        dofTable.BitsPerSubBlock[subBlock].SafeBitsW(CatchAllBitCount)
                    );
            }
        }

        return Column0BitCount;
    }
    #endregion
    
}