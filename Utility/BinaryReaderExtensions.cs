using IceBloc.Frostbite2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Input;

namespace IceBloc.Utility;

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

    public static CatalogEntry ReadCatalogEntry(this BinaryReader reader)
    {
        CatalogEntry entry = new();
        entry.SHA = reader.ReadBytes(20);
        entry.Offset = reader.ReadUInt32();
        entry.DataSize = reader.ReadInt32();
        entry.CasFileIndex = reader.ReadInt32();

        return entry;
    }

    public static Matrix4x4 ReadMatrix4x4(this BinaryReader reader)
    {
        return new Matrix4x4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                             reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                             reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                             reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
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
}
