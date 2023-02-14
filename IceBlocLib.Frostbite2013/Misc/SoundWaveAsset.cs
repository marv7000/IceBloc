using IceBlocLib.Frostbite;
using IceBlocLib.InternalFormats;

namespace IceBlocLib.Frostbite2013.Misc;

public class SoundWaveAsset
{
    public static List<InternalSound> ConvertToInternal(in Dbx dbx)
    {
        // Initialize our result and load the sound chunk from EBX.
        List<InternalSound> sounds = new();

        // Get the amount of Chunks in the EBX.
        int chunkCount = (dbx.Prim["$"]["Chunks"].Value as Complex).Fields.Count;

        // For each chunk in the EBX, append the ChunkId to the list of IDs.
        List<Guid> chunkGuid = new List<Guid>();
        for (int i = 0; i < chunkCount; i++)
            chunkGuid.Add((Guid)dbx.Prim["$"]["Chunks"][i]["ChunkId"].Value);

        var variations = dbx.Prim["Variations"];
        var variationCount = (variations.Value as Complex).Fields.Count;

        // For every sound variation in the EBX.
        for (int x = 0; x < variationCount; x++)
        {
            // Initialize variables
            var samples = new List<short>();
            var internalSound = new InternalSound();
            var start = 0f;

            var variation = variations[x].Link(in dbx);
            var chunkIndex = ((int)(sbyte)variation["ChunkIndex"].Value);

            using var stream = new MemoryStream(IO.GetChunk(chunkGuid[chunkIndex]));
            using var r = new BinaryReader(stream);


            // Get the first and last loop segment index
            var segments = (variation["Segments"].Value as Complex).Fields;
            var firstLoopSegmentIndex = ((uint)(sbyte)variation["FirstLoopSegmentIndex"].Value);
            var lastLoopSegmentIndex = ((uint)(sbyte)variation["LastLoopSegmentIndex"].Value);

            // Patch together all segments.
            for (int i = 0; i <= lastLoopSegmentIndex - firstLoopSegmentIndex; i++)
            {
                var segment = segments[(int)firstLoopSegmentIndex + i];
                var segmentLength = (float)segments[(int)firstLoopSegmentIndex + i]["SegmentLength"].Value;
                var samplesOffset = (uint)segments[(int)firstLoopSegmentIndex + i]["SamplesOffset"].Value;

                r.BaseStream.Position = samplesOffset;
                if (r.ReadUInt16() != 0x48) 
                    return new List<InternalSound>();

                short unk1 = r.ReadInt16(true);
                SndPlayerCodec playerCodec = (SndPlayerCodec)(r.ReadByte() & 0x0F);

                // Channel count calculation according to https://wiki.multimedia.cx/index.php/EA_SAGE_Audio_Files
                int channelCount = r.ReadByte() / 4 + 1;
                ushort sampleRate = r.ReadUInt16(true);
                uint totalSamples = r.ReadUInt32(true) & 0x0FFFFFFF;

                if (i == firstLoopSegmentIndex && segments.Count > 1)
                {
                    start = samples.Count / channelCount / (float)sampleRate;
                    internalSound.LoopStart = (uint)samples.Count;
                }

                r.BaseStream.Position = samplesOffset;
                byte[] buffer = r.ReadUntilStreamEnd();

                switch (playerCodec)
                {
                    case SndPlayerCodec.SIGN16BIG_INT:
                        {
                            short[] data = DecodeSign16Big(buffer);
                            samples.AddRange(data);
                            totalSamples = (uint)data.Length;
                            break;
                        }
                    case SndPlayerCodec.XAS1_INT:
                        {
                            short[] data = DecodeXas1(buffer);
                            samples.AddRange(data);
                            totalSamples = (uint)data.Length;
                            break;
                        }
                    case SndPlayerCodec.EALAYER31_INT:
                    case SndPlayerCodec.EALAYER32PCM_INT:
                        throw new NotSupportedException("SoundWaveAsset: Unsupported type EALayer3!");
                }

                if (i == lastLoopSegmentIndex && segments.Count > 1)
                {
                    float loopDuration = ((samples.Count / channelCount) / (float)sampleRate) - start;
                    internalSound.SampleCount = (uint)samples.Count;
                }

                internalSound.SampleRate = sampleRate;
                internalSound.ChannelCount = channelCount;

                if (segmentLength == 0.0f)
                {
                    segmentLength = (samples.Count / channelCount) / (float)sampleRate;
                }

                internalSound.Length = (samples.Count / internalSound.ChannelCount) / (double)internalSound.SampleRate;
                internalSound.Data = samples.ToArray();
            }

            sounds.Add(internalSound);
        }
        
        return sounds;
    }

    public static short[] DecodeXas1(byte[] soundBuffer)
    {
        using var r = new BinaryReader(new MemoryStream(soundBuffer));

        ushort blockType = r.ReadUInt16();
        ushort blockSize = r.ReadUInt16(true);
        byte compressionType = r.ReadByte();

        int channelCount = (r.ReadByte() >> 2) + 1;
        ushort sampleRate = r.ReadUInt16(true);
        int totalSamples = r.ReadInt32(true) & 0x00FFFFFF;

        List<short>[] channels = new List<short>[channelCount];
        for (int i = 0; i < channelCount; i++)
            channels[i] = new List<short>();

        while (r.BaseStream.Position <= r.BaseStream.Length)
        {
            blockType = r.ReadUInt16();
            blockSize = r.ReadUInt16(true);

            if (blockType == 0x45)
                break;

            uint samples = r.ReadUInt32(true);

            byte[] buffer;
            short[] buf = new short[32];
            int[] numA = new int[] { 0, 240, 460, 392 };
            int[] numB = new int[] { 0, 0, -208, -220 };

            for (int i = 0; i < (blockSize / 76 / channelCount); i++)
            {
                for (int j = 0; j < channelCount; j++)
                {
                    buffer = r.ReadBytes(76);

                    for (int k = 0; k < 4; k++)
                    {
                        buf[0] = (short)(buffer[k * 4 + 0] & 0xF0 | buffer[k * 4 + 1] << 8);
                        buf[1] = (short)(buffer[k * 4 + 2] & 0xF0 | buffer[k * 4 + 3] << 8);

                        int v4 = buffer[k * 4] & 0x0F;
                        int v10 = buffer[k * 4 + 2] & 0x0F;
                        int v5 = 2;

                        while (v5 < 32)
                        {
                            int v11 = (buffer[12 + k + v5 * 2] & 240) >> 4;
                            if (v11 > 7)
                                v11 -= 16;

                            int v12 = buf[v5 - 1] * numA[v4] + buf[v5 - 2] * numB[v4];

                            buf[v5] = (short)(v12 + (v11 << 20 - v10) + 128 >> 8);
                            if (buf[v5] > short.MaxValue)
                                buf[v5] = short.MaxValue;
                            else if (buf[v5] < short.MinValue)
                                buf[v5] = short.MinValue;

                            int v13 = (int)buffer[12 + k + v5 * 2] & 15;
                            if (v13 > 7)
                                v13 -= 16;

                            int v14 = buf[v5] * numA[v4] + buf[v5 - 1] * numB[v4];

                            buf[v5 + 1] = (short)(v14 + (v13 << 20 - v10) + 128 >> 8);
                            if (buf[v5 + 1] > short.MaxValue)
                                buf[v5 + 1] = short.MaxValue;
                            else if (buf[v5 + 1] < short.MinValue)
                                buf[v5 + 1] = short.MinValue;

                            v5 += 2;
                        }

                        channels[j].AddRange(buf);
                    }

                    uint sampleSize = (samples < 128) ? samples : 128;
                    samples -= sampleSize;
                }
            }
        }

        short[] outBuffer = new short[channels[0].Count * channelCount];
        for (int i = 0; i < channels[0].Count; i++)
        {
            for (int j = 0; j < channelCount; j++)
            {
                outBuffer[(i * channelCount) + j] = channels[j][i];
            }
        }

        return outBuffer;
    }

    public static short[] DecodeSign16Big(byte[] soundBuffer)
    {
        using var reader = new BinaryReader(new MemoryStream(soundBuffer));
        ushort blockType = reader.ReadUInt16();
        ushort blockSize = reader.ReadUInt16(true);
        byte compressionType = reader.ReadByte();

        int channelCount = (reader.ReadByte() >> 2) + 1;
        ushort sampleRate = reader.ReadUInt16(true);
        int totalSamples = reader.ReadInt32(true) & 0x00FFFFFF;

        List<short>[] channels = new List<short>[channelCount];
        for (int i = 0; i < channelCount; i++)
            channels[i] = new List<short>();

        while (reader.BaseStream.Position <= reader.BaseStream.Length)
        {
            blockType = reader.ReadUInt16();
            blockSize = reader.ReadUInt16(true);

            if (blockType == 0x45)
                break;

            uint samples = reader.ReadUInt32(true);

            for (int i = 0; i < samples; i++)
            {
                for (int j = 0; j < channelCount; j++)
                    channels[j].Add(reader.ReadInt16(true));
            }
        }

        short[] outBuffer = new short[channels[0].Count * channelCount];
        for (int i = 0; i < channels[0].Count; i++)
        {
            for (int j = 0; j < channelCount; j++)
            {
                outBuffer[(i * channelCount) + j] = channels[j][i];
            }
        }

        return outBuffer;
    }
}