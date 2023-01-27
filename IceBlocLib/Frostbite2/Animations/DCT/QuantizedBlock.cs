using IceBlocLib.Utility;
using System.Runtime.InteropServices;

namespace IceBlocLib.Frostbite2.Animations.DCT;

public class QuantizedBlock
{
    Vector4i[] mSamples = new Vector4i[8];

    public void HighPassFilter(ushort MinAllowedFreq)
    {
        for (ushort i = 0; i < 8; i++)
        {
            for (ushort j = 0; j < 4; j++)
            {
                if (MathF.Abs(mSamples[i][j]) < MinAllowedFreq)
                {
                    mSamples[i][j] = 0;
                }
            }
        }
    }

    public uint FindLastNonZeroSample() 
    {
        short result;
        for (short i = 8 - 1; i >= 0; i--)
        {
            result = (short)(mSamples[i][0] + mSamples[i][1] + mSamples[i][2] + mSamples[i][3]);
            if (result != 0)
            {
                return (uint)i;
            }
        }
        return 8;
    }

    void ComputeMinBits_ForEachComponentValue(byte[] MinBits) 
    {
        int k = 0;
        for (ushort i = 0; i < 8; i++)
        {
            for (ushort j = 0; j < 4; j++)
            {
                MinBits[k++] = ComputeMinBitsForSignedValue(mSamples[i][j]);
            }
        }
    }

    void FindMaxAbs_ByComponent(Vector4i MaxAbs_ByComponent) 
    {
        MaxAbs_ByComponent.Set(0, 0, 0, 0);

        Vector4i vector;
        for (ushort i = 0; i < 8; i++)
        {
            vector = mSamples[i];
            MaxAbs_ByComponent[0] = Math.Max(MaxAbs_ByComponent[0], Math.Abs(vector[0]));
            MaxAbs_ByComponent[1] = Math.Max(MaxAbs_ByComponent[1], Math.Abs(vector[1]));
            MaxAbs_ByComponent[2] = Math.Max(MaxAbs_ByComponent[2], Math.Abs(vector[2]));
            MaxAbs_ByComponent[3] = Math.Max(MaxAbs_ByComponent[3], Math.Abs(vector[3]));
        }
    }

    public unsafe void ComputeBitCount_ByComponent(byte* BitCount_ByComponent)
    {
        Vector4i vec = new();

        FindMaxAbs_ByComponent(vec);

        for (ushort i = 0; i < 4; i++)
        {
            BitCount_ByComponent[i] = ComputeMinBitsForSignedValue(vec[i]);
        }
    }

    public static byte GetBitCount_WithCatchAll(byte RawBitCount, byte CatchAllResult)
    {
        return RawBitCount < 15 ? RawBitCount : CatchAllResult;
    }

    public unsafe static byte ComputeMinBitsForSignedValue(short Value)
    {
        return (byte)((Math.Floor(Math.Log2(Value))) + 1);
    }
}

public struct BlockBitTable
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    static byte[][] sBlockBitPattern = new byte[16][];

    public unsafe byte ClassifyBlock(byte* MaxBits_BySubblock)
    {
        byte x = 0;

        for (int i = 8 - 1; x < 16 && i >= 0; i--)
        {
            while (x < 16 && (MaxBits_BySubblock[i] > sBlockBitPattern[x][i]))
            {
                ++x;
            }
        }
        return x < 16 ? x : (byte)(16 - 1);
    }
}