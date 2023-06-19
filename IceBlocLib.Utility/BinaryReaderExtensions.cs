using System.Buffers.Binary;
using System.Numerics;

namespace IceBlocLib.Frostbite;

public static class BinaryReaderExtensions
{
    /// <summary>
    /// Reads a 7-bit encoded LEB128 integer.
    /// </summary>
    /// <returns>The decoded integer.</returns>
    public static int ReadLEB128(this BinaryReader reader)
    {
        int result = 0; int shift = 0;
        while (true)
        {
            byte b = reader.ReadBytes(1)[0];
            result |= (b & 0x7f) << shift;
            if (b >> 7 == 0)
                return result;
            shift += 7;
        }
    }
    public static Matrix4x4 ReadMatrix4x4(this BinaryReader reader)
    {
        return new Matrix4x4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                             reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                             reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                             reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
    public static Vector4 ReadVector4(this BinaryReader reader)
    {
        return new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
    public static TimeSpan ReadDBTimeSpan(this BinaryReader reader)
    {
        var val = (ulong)reader.ReadLEB128();
        ulong lower = val & 0x00000000FFFFFFFF;
        var upper = (val & 0xFFFFFFFF00000000) >> 32;
        var flag = lower & 1;
        var span = lower >> 1 ^ flag | (upper >> 1 ^ flag) << 32;
        return new TimeSpan((long)span);
    }

    /// <summary>
    /// Reads all bytes of the underlying stream until it has reached its end.
    /// </summary>
    /// <returns>All bytes of the <see cref="Stream"/> from the current position until the end.</returns>
    public static byte[] ReadUntilStreamEnd(this BinaryReader reader)
    {
        long length = reader.BaseStream.Length - reader.BaseStream.Position;
        return reader.ReadBytes((int)length);
    }

    /// <summary>
    /// Reads an undefined length of chars until it enounters a nullbyte.
    /// </summary>
    /// <returns><see cref="string"/> containing the read data.</returns>
    public static string ReadNullTerminatedString(this BinaryReader reader)
    {
        List<char> chars = new();
        while (true)
        {
            var value = reader.ReadChar();
            if (value != 0x00)
                chars.Add(value);
            else
                return new string(chars.ToArray());
        }
    }
    
    public static object ReadByType<T>(this BinaryReader r)
    {
        switch (typeof(T).Name)
        {
            case "Byte":
                return (T)(object)r.ReadByte();
            case "Int16":
                return (T)(object)r.ReadInt16();
            case "UInt16":
                return (T)(object)r.ReadUInt16();
            case "Int32":
                return (T)(object)r.ReadInt32();
            case "UInt32":
                return (T)(object)r.ReadUInt32();
            case "Int64":
                return (T)(object)r.ReadInt64();
            case "IntU64":
                return (T)(object)r.ReadUInt64();
            case "String":
                return (T)(object)r.ReadNullTerminatedString();
            case "Byte[]":
                return (T)(object)r.ReadUntilStreamEnd();
            default:
                return default(T);
        }
    }

    /// <summary>
    /// Advances the stream to the next position that is a multiple of the given parameter.
    /// </summary>
    public static void Align(this BinaryReader r, int alignBy)
    {
        if (r.BaseStream.Position % alignBy != 0)
        {
            r.BaseStream.Position += alignBy - (r.BaseStream.Position % alignBy);
        }
    }

    #region Primitive Extensions
    public static Guid ReadGuid(this BinaryReader r, bool bigEndian)
    {
        if (bigEndian)
            return new Guid(r.ReadBytes(16).Reverse().ToArray());
        else
            return new Guid(r.ReadBytes(16));
    }

    public static int ReadInt32(this BinaryReader r, bool bigEndian)
    {
        if (bigEndian)
            return BinaryPrimitives.ReverseEndianness(r.ReadInt32());
        else
            return r.ReadInt32();
    }
    public static uint ReadUInt32(this BinaryReader r, bool bigEndian)
    {
        if (bigEndian)
            return BinaryPrimitives.ReverseEndianness(r.ReadUInt32());
        else
            return r.ReadUInt32();
    }
    public static short ReadInt16(this BinaryReader r, bool bigEndian)
    {
        if (bigEndian)
            return BinaryPrimitives.ReverseEndianness(r.ReadInt16());
        else
            return r.ReadInt16();
    }
    public static ushort ReadUInt16(this BinaryReader r, bool bigEndian)
    {
        if (bigEndian)
            return BinaryPrimitives.ReverseEndianness(r.ReadUInt16());
        else
            return r.ReadUInt16();
    }
    public static long ReadInt64(this BinaryReader r, bool bigEndian)
    {
        if (bigEndian)
            return BinaryPrimitives.ReverseEndianness(r.ReadInt64());
        else
            return r.ReadInt64();
    }
    public static ulong ReadUInt64(this BinaryReader r, bool bigEndian)
    {
        if (bigEndian)
            return BinaryPrimitives.ReverseEndianness(r.ReadUInt64());
        else
            return r.ReadUInt64();
    }
    public static float ReadSingle(this BinaryReader r, bool bigEndian)
    {
        if (bigEndian)
            return BitConverter.ToSingle(r.ReadBytes(4).Reverse().ToArray());
        else
            return r.ReadSingle();
    }
    public static double ReadDouble(this BinaryReader r, bool bigEndian)
    {
        if (bigEndian)
            return BitConverter.ToDouble(r.ReadBytes(8).Reverse().ToArray());
        else
            return r.ReadDouble();
    }

    public static sbyte[] ReadInt8Array(this BinaryReader r, int count)
    {
        sbyte[] array = new sbyte[count];

        for (int i = 0; i < count; i++)
        {
            array[i] = r.ReadSByte();
        }

        return array;
    }
    
    public static byte[] ReadUInt8Array(this BinaryReader r, int count)
    {
        byte[] array = new byte[count];

        for (int i = 0; i < count; i++)
        {
            array[i] = r.ReadByte();
        }

        return array;
    }

    public static short[] ReadInt16Array(this BinaryReader r, int count, bool bigEndian)
    {
        short[] array = new short[count];

        for (int i = 0; i < count; i++)
        {
            array[i] = r.ReadInt16(bigEndian);
        }

        return array;
    }
    public static int[] ReadInt32Array(this BinaryReader r, int count, bool bigEndian)
    {
        int[] array = new int[count];

        for (int i = 0; i < count; i++)
        {
            array[i] = r.ReadInt32(bigEndian);
        }

        return array;
    }
    public static long[] ReadInt64Array(this BinaryReader r, int count, bool bigEndian)
    {
        long[] array = new long[count];

        for (int i = 0; i < count; i++)
        {
            array[i] = r.ReadInt64(bigEndian);
        }

        return array;
    }
    public static ushort[] ReadUInt16Array(this BinaryReader r, int count, bool bigEndian)
    {
        ushort[] array = new ushort[count];

        for (int i = 0; i < count; i++)
        {
            array[i] = r.ReadUInt16(bigEndian);
        }

        return array;
    }
    public static uint[] ReadUInt32Array(this BinaryReader r, int count, bool bigEndian)
    {
        uint[] array = new uint[count];

        for (int i = 0; i < count; i++)
        {
            array[i] = r.ReadUInt32(bigEndian);
        }

        return array;
    }
    public static ulong[] ReadUInt64Array(this BinaryReader r, int count, bool bigEndian)
    {
        ulong[] array = new ulong[count];

        for (int i = 0; i < count; i++)
        {
            array[i] = r.ReadUInt64(bigEndian);
        }

        return array;
    }
    public static float[] ReadSingleArray(this BinaryReader r, int count, bool bigEndian)
    {
        float[] array = new float[count];

        for (int i = 0; i < count; i++)
        {
            array[i] = r.ReadSingle(bigEndian);
        }

        return array;
    }
    public static double[] ReadDoubleArray(this BinaryReader r, int count, bool bigEndian)
    {
        double[] array = new double[count];

        for (int i = 0; i < count; i++)
        {
            array[i] = r.ReadDouble(bigEndian);
        }

        return array;
    }
    public static Guid[] ReadGuidArray(this BinaryReader r, int count, bool bigEndian)
    {
        Guid[] array = new Guid[count];

        for (int i = 0; i < count; i++)
        {
            array[i] = r.ReadGuid(bigEndian);
        }

        return array;
    }

    public static void Write(this BinaryWriter w, Vector4 vector)
    {
        w.Write(vector.X);
        w.Write(vector.Y);
        w.Write(vector.Z);
        w.Write(vector.W);
    }

    #endregion
}
