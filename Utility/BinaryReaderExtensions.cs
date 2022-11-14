using IceBloc.Frostbite2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

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

    public static T ReadStruct<T>(this BinaryReader reader)
    {
        var byteLength = Marshal.SizeOf(typeof(T));
        var bytes = reader.ReadBytes(byteLength);
        var pinned = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        T stt = (T)Marshal.PtrToStructure(pinned.AddrOfPinnedObject(), typeof(T));
        pinned.Free();
        return stt;
    }

    public static void WriteStruct<T>(this BinaryWriter writer, T t)
    {
        var sizeOfT = Marshal.SizeOf(typeof(T));
        var ptr = Marshal.AllocHGlobal(sizeOfT);
        Marshal.StructureToPtr(t, ptr, false);
        var bytes = new byte[sizeOfT];
        Marshal.Copy(ptr, bytes, 0, bytes.Length);
        Marshal.FreeHGlobal(ptr);
        writer.Write(bytes);
    }

    public static RelocPtr ReadRelocPtr(this BinaryReader reader)
    {
        RelocPtr ptr;
        ptr.Ptr = reader.ReadInt32();
        ptr.Pad = reader.ReadInt32(); // Should always be 0, but just read it for consistency.
        return ptr;
    }

    public static RelocArray ReadRelocArray(this BinaryReader reader)
    {
        RelocArray arr;
        arr.Size = reader.ReadUInt32();
        arr.BaseAddress = reader.ReadRelocPtr();
        return arr;
    }

    public static GeometryDeclarationDesc ReadGeometryDeclarationDesc(this BinaryReader reader)
    {
        GeometryDeclarationDesc desc = new();
        for (int i = 0; i < 16; i++)
        {
            desc.Elements[i].Usage = (VertexElementUsage)reader.ReadByte(); 
            desc.Elements[i].Format = (VertexElementFormat)reader.ReadByte(); 
            desc.Elements[i].Offset = reader.ReadByte(); 
            desc.Elements[i].StreamIndex = reader.ReadByte(); 
        }
        for (int i = 0; i < 4; i++)
        {
            desc.Streams[i].Stride = reader.ReadByte();
            desc.Streams[i].Classification = (VertexElementClassification)reader.ReadByte();
        }
        desc.ElementCount = reader.ReadByte();
        desc.StreamCount = reader.ReadByte();
        reader.ReadBytes(2);

        return desc;
    }
}
