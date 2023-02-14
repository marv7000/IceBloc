using IceBlocLib.Frostbite2013.Animations.Base;
using System.Numerics;

namespace IceBlocLib.Frostbite2013.Animations.DCT;

public static class DctAnimationExtensions
{
    public static float[,] c_DctCoeffs = new float[8, 8] {
        { 0.250000f, 0.490393f, 0.461940f, 0.415735f, 0.353553f, 0.277785f, 0.191342f, 0.097545f,  },
        { 0.250000f, 0.415735f, 0.191342f, -0.097545f, -0.353553f, -0.490393f, -0.461940f, -0.277785f,  },
        { 0.250000f, 0.277785f, -0.191342f, -0.490393f, -0.353553f, 0.097545f, 0.461940f, 0.415735f,  },
        { 0.250000f, 0.097545f, -0.461940f, -0.277785f, 0.353553f, 0.415735f, -0.191342f, -0.490393f,  },
        { 0.250000f, -0.097545f, -0.461940f, 0.277785f, 0.353553f, -0.415735f, -0.191342f, 0.490393f,  },
        { 0.250000f, -0.277785f, -0.191342f, 0.490393f, -0.353553f, -0.097545f, 0.461940f, -0.415735f,  },
        { 0.250000f, -0.415735f, 0.191342f, 0.097545f, -0.353553f, 0.490393f, -0.461940f, 0.277785f,  },
        { 0.250000f, -0.490393f, 0.461940f, -0.415735f, 0.353554f, -0.277785f, 0.191342f, -0.097545f,  },
    };

    public static float[] GenerateCoeffs(this DctAnimation dct, ushort p_Frame)
    {
        float[] s_Coeffs = new float[8];

        var s_CoeffIdx = p_Frame % 8;

        for (var i = 0; i < 8; i++)
        {
            var s_Coeff = c_DctCoeffs[s_CoeffIdx, i];
            var s_Multiplier = ((float)dct.QuantizeMultSubblock * 0.1f * (float)i + 1.0f) / (float)dct.QuantizeMultBlock;
            var s_Value = s_Coeff * s_Multiplier;

            s_Coeffs[i] = s_Value;
        }
        return s_Coeffs;
    }

    public static Vector4 UnpackVec(this DctAnimation dct, List<short> p_Values, ushort p_Frame)
    {
        var s_Result = new Vector4(0.0f);

        var s_Coefs = dct.GenerateCoeffs(p_Frame);

        for (var i = 0; i < 8; i++)
        {
            var s_Vec = new Vector4(p_Values[i * 4 + 0], p_Values[i * 4 + 1], p_Values[i * 4 + 2], p_Values[i * 4 + 3]);

            s_Result += Vector4.Multiply(s_Vec, s_Coefs[i]);
        }

        return s_Result;
    }

    public static List<Vector4> Decompress(this DctAnimation dct)
    {
        var s_DofCount = dct.NumVec3 + dct.NumQuats + dct.NumFloatVec;

        var s_DofTable = new DofTable[s_DofCount];

        var s_SubBlockTotal = 0;
        for (var i = 0; i < s_DofCount; i++)
        {
            // 4 bits is unused.
            var s_SubBlocksCount = (byte)(dct.DofTableDescBytes[i] >> 4 & 0xF);

            var s_DofData = new DofTable(s_SubBlocksCount);
            s_DofData.DeltaBase = new short[4]
            {
                dct.DeltaBaseX[i],
                dct.DeltaBaseY[i],
                dct.DeltaBaseZ[i],
                dct.DeltaBaseW[i],
            };

            s_DofData.BitsPerSubBlock = new DofTable.BitsPerComponent[s_DofData.SubBlockCount];
            for (var j = 0; j < s_DofData.SubBlockCount; j++)
                s_DofData.BitsPerSubBlock[j] = new DofTable.BitsPerComponent(dct.BitsPerSubblock[s_SubBlockTotal + j]);

            s_DofTable[i] = s_DofData;

            s_SubBlockTotal += s_SubBlocksCount;

        }

        var s_BitReader = new BitReader(new MemoryStream(dct.Data), 64, Endianness.BigEndian);


        List<List<short>> s_Blocks = new();
        for (var s_BlockFrame = 0; s_BlockFrame < (dct.NumKeys + 7) / 8; s_BlockFrame++)
        {
            for (var s_DofIdx = 0; s_DofIdx < s_DofTable.Length; s_DofIdx++)
            {
                List<short> s_Block = new();

                var s_SubBlock = s_DofTable[s_DofIdx];

                var s_Components = s_SubBlock.BitsPerSubBlock;

                if (s_BlockFrame == 0)
                {
                    s_Block.Add(0);
                    s_Block.Add(0);
                    s_Block.Add(0);
                    s_Block.Add(0);

                    s_Components = s_Components.Skip(1).ToArray();
                }

                foreach (var s_Component in s_Components)
                {
                    var s_X = s_BitReader.ReadIntHigh(s_Component.SafeBitsX(dct.CatchAllBitCount));
                    var s_Y = s_BitReader.ReadIntHigh(s_Component.SafeBitsY(dct.CatchAllBitCount));
                    var s_Z = s_BitReader.ReadIntHigh(s_Component.SafeBitsZ(dct.CatchAllBitCount));
                    var s_W = s_BitReader.ReadIntHigh(s_Component.SafeBitsW(dct.CatchAllBitCount));

                    s_Block.Add((short)s_X);
                    s_Block.Add((short)s_Y);
                    s_Block.Add((short)s_Z);
                    s_Block.Add((short)s_W);
                }

                if (s_Components.Length < 8)
                {
                    for (var i = 0; i < 8 - s_Components.Length; i++)
                    {
                        s_Block.Add(0);
                        s_Block.Add(0);
                        s_Block.Add(0);
                        s_Block.Add(0);
                    }
                }

                s_Block[0] += s_SubBlock.DeltaBase[0];
                s_Block[1] += s_SubBlock.DeltaBase[1];
                s_Block[2] += s_SubBlock.DeltaBase[2];
                s_Block[3] += s_SubBlock.DeltaBase[3];

                s_Blocks.Add(s_Block);
            }
        }

        List<Vector4> dat = new();

        for (var s_Frame = 0; s_Frame < dct.NumKeys; s_Frame++)
        {
            var s_BlockIdx = s_Frame / 8;

            for (var s_DofIdx = 0; s_DofIdx < s_DofTable.Length; s_DofIdx++)
            {
                var s_DataIdx = s_BlockIdx * s_DofTable.Length + s_DofIdx;

                if (s_DataIdx >= s_Blocks.Count)
                    break;

                var s_Block = s_Blocks.ElementAt(s_DataIdx);

                dat.Add(dct.UnpackVec(s_Block, (ushort)s_Frame));
            }
        }

        return dat;
    }
}
