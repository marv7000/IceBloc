using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using IceBloc.Frostbite.Database;
using IceBloc.Frostbite.Meshes;
using IceBloc.Frostbite.Textures;
using static IceBloc.Frostbite.GenericData;

namespace IceBloc.Frostbite;

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
    public static RelocPtr<T> ReadRelocPtr<T>(this BinaryReader reader)
    {
        RelocPtr<T> ptr;
        ptr.Ptr = reader.ReadInt32();
        ptr.Pad = reader.ReadInt32(); // Should always be 0, but just read it for consistency.
        // Save our current stream position so we can find the value at the Ptr offset.
        long currentStreamPos = reader.BaseStream.Position;

        ptr.Value = default;
        if (ptr.Ptr != 0)
        {
            reader.BaseStream.Position = ptr.Ptr;
            ptr.Value = (T)reader.ReadByType<T>();
        }

        // Return back to our old position.
        reader.BaseStream.Position = currentStreamPos;
        return ptr;
    }
    public static RelocArray<T> ReadRelocArray<T>(this BinaryReader reader)
    {
        RelocArray<T> arr;
        arr.Size = reader.ReadUInt32();
        arr.BaseAddress = reader.ReadRelocPtr<T>();

        if (arr.BaseAddress.Ptr != 0)
        {
            long currentStreamPos = reader.BaseStream.Position;

            reader.BaseStream.Position = arr.BaseAddress.Ptr;
            List<T> valueList = new();
            for (int i = 0; i < arr.Size; i++)
            {
                valueList.Add((T)reader.ReadByType<T>());
            }
            // Return back to our old position.
            reader.BaseStream.Position = currentStreamPos;
        }
        return arr;
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
            case "MeshLayout":
                return (T)(object)r.ReadMeshLayout();
            case "MeshSubset":
                return (T)(object)r.ReadMeshSubset();
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
        r.BaseStream.Position += alignBy - (r.BaseStream.Position % alignBy);
    }

    #region Mesh Extensions

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
    public static MeshSetLayout ReadMeshSetLayout(this BinaryReader r)
    {
        MeshSetLayout msl = new();
        msl.MeshType = (MeshType)r.ReadUInt32();
        msl.Flags = r.ReadUInt32();
        msl.LodCount = r.ReadInt32();
        msl.TotalSubsetCount = r.ReadInt32();
        msl.BoundBoxMin = r.ReadVector4();
        msl.BoundBoxMax = r.ReadVector4();
        for (int i = 0; i < 5; i++)
        {
            msl.LOD[i] = r.ReadRelocPtr<MeshLayout>();
        }
        msl.Name = r.ReadRelocPtr<string>();
        msl.ShortName = r.ReadRelocPtr<string>();
        msl.NameHash = r.ReadInt32();
        r.ReadInt32(); // Pad

        return msl;
    }
    public static MeshLayout ReadMeshLayout(this BinaryReader r)
    {
        MeshLayout ml = new();

        ml.Type = (MeshType)r.ReadUInt32();
        ml.SubCount = r.ReadInt32();
        ml.SubSets = r.ReadRelocPtr<MeshSubset>();
        for (int j = 0; j < 4; j++)
        {
            ml.CategorySubsetIndices[j] = r.ReadRelocArray<byte>();
        }
        ml.Flags = (MeshLayoutFlags)r.ReadUInt32();
        ml.IndexBufferFormat = (IndexBufferFormat)r.ReadInt32();
        ml.IndexDataSize = r.ReadInt32();
        ml.VertexDataSize = r.ReadInt32();
        ml.EdgeDataSize = r.ReadInt32();
        ml.DataChunkID = new Guid(r.ReadBytes(16));
        ml.AuxVertexIndexDataOffset = r.ReadInt32();
        ml.EmbeddedEdgeData = r.ReadRelocPtr<byte[]>();
        ml.ShaderDebugName = r.ReadRelocPtr<string>();
        ml.Name = r.ReadRelocPtr<string>();
        ml.ShortName = r.ReadRelocPtr<string>();
        ml.NameHash = r.ReadInt32();
        ml.Data = r.ReadRelocPtr<int>();
        ml.u17 = r.ReadInt32();
        ml.u18 = r.ReadInt64();
        ml.u19 = r.ReadInt64();
        ml.SubsetPartIndices = r.ReadRelocPtr<short>();

        return ml;
    }
    public static MeshSubset ReadMeshSubset(this BinaryReader r)
    {
        MeshSubset subset = new();

        subset.GeometryDeclarations = r.ReadRelocPtr<int>();
        subset.MaterialName = r.ReadRelocPtr<string>();
        subset.MaterialIndex = r.ReadInt32();
        subset.PrimitiveCount = r.ReadInt32();
        subset.StartIndex = r.ReadInt32();
        subset.VertexOffset = r.ReadInt32();
        subset.VertexCount = r.ReadInt32();
        subset.VertexStride = r.ReadByte();
        subset.PrimitiveType = (PrimitiveType)r.ReadByte();
        subset.BonesPerVertex = r.ReadByte();
        subset.BoneCount = r.ReadByte();
        subset.BoneIndices = r.ReadRelocPtr<short>();
        subset.GeoDecls = r.ReadGeometryDeclarationDesc();
        for (int i = 0; i < 6; i++)
        {
            subset.TexCoordRatios[i] = r.ReadSingle();
        }

        return subset;
    }
    public static DxTexture ReadDxTexture(this BinaryReader rr)
    {
        var tex = new DxTexture();

        tex.Version = rr.ReadUInt32();
        tex.TexType = (TextureType)rr.ReadInt32();
        tex.TexFormat = (TextureFormat)rr.ReadUInt32();
        tex.Flags = rr.ReadUInt32();
        tex.Width = rr.ReadUInt16();
        tex.Height = rr.ReadUInt16();
        tex.Depth = rr.ReadUInt16();
        tex.SliceCount = rr.ReadUInt16();
        rr.ReadUInt16();
        tex.MipmapCount = rr.ReadByte();
        tex.MipmapBaseIndex = rr.ReadByte();
        tex.StreamingChunkId = new Guid(rr.ReadBytes(16));
        // Mipmaps.
        for (int i = 0; i < 15; i++) tex.MipmapSizes[i] = rr.ReadUInt32();
        tex.MipmapChainSize = rr.ReadUInt32();
        tex.ResourceNameHash = rr.ReadUInt32();
        // A TextureGroup is always 16 chars long, we will reinterpret as string for ease of use.
        tex.TextureGroup = Encoding.ASCII.GetString(rr.ReadBytes(16)).Replace("\0", "");

        return tex;
    }

    #endregion

    #region Primitive Extensions
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

    #endregion

    #region EBX Extensions

    public static Guid ReadGuid(this BinaryReader r, bool bigEndian)
    {
        if (bigEndian)
            return new Guid(r.ReadBytes(16).Reverse().ToArray());
        else
            return new Guid(r.ReadBytes(16));
    }
    public static Complex ReadComplex(this BinaryReader r, in Dbx dbx, int complexIndex)
    {
        var complexDesc = dbx.ComplexDescriptors[complexIndex];
        var cmplx = new Complex(complexDesc);

        var startPos = r.BaseStream.Position;
        cmplx.Fields = new();

        for (int i = complexDesc.FieldStartIndex; i < complexDesc.FieldStartIndex + complexDesc.NumField; i++)
        {
            r.BaseStream.Position = startPos + dbx.FieldDescriptors[i].Offset;
            cmplx.Fields.Add(r.ReadField(dbx, i));
        }

        r.BaseStream.Position = startPos + complexDesc.Size;
        return cmplx;
    }
    public static Field ReadField(this BinaryReader r, in Dbx dbx, int fieldIndex)
    {
        var field = new Field(
            new FieldDescriptor(
                new int[] { 
                    dbx.FieldDescriptors[fieldIndex].Name, 
                    dbx.FieldDescriptors[fieldIndex].Type,
                    dbx.FieldDescriptors[fieldIndex].Ref,
                    dbx.FieldDescriptors[fieldIndex].Offset,
                    dbx.FieldDescriptors[fieldIndex].SecondaryOffset
                }));
        var typ = field.Desc.GetFieldType();

        switch(typ)
        {
            case FieldType.Void:
                field.Value = r.ReadComplex(dbx, field.Desc.Ref); break;
            case FieldType.ValueType:
                field.Value = r.ReadComplex(dbx, field.Desc.Ref); break;
            case FieldType.Class:
                field.Value = r.ReadUInt32(dbx.BigEndian); break;
            case FieldType.Array:
                {
                    // Array
                    var arrayRptr = dbx.ArrayRepeaters[r.ReadUInt32(dbx.BigEndian)];
                    var arrayCmplxDesc = dbx.ComplexDescriptors[field.Desc.Ref];

                    r.BaseStream.Position = dbx.ArraySectionStart + arrayRptr.Offset;
                    var arrayCmplx = new Complex(arrayCmplxDesc);
                    for (int i = 0; i < arrayRptr.Repetitions; i++)
                    {
                        arrayCmplx.Fields.Add(r.ReadField(dbx, arrayCmplxDesc.FieldStartIndex));
                    }
                    field.Value = arrayCmplx; break;
                }
            case FieldType.CString:
                {
                    var startPos = r.BaseStream.Position;
                    var stringOffset = r.ReadInt32(dbx.BigEndian);
                    if (stringOffset == -1)
                        field.Value = "<NullString>";
                    else
                    {
                        r.BaseStream.Position = dbx.Header.AbsStringOffset + stringOffset;
                        field.Value = r.ReadNullTerminatedString();
                        r.BaseStream.Position = startPos + 4;

                        if (dbx.IsPrimaryInstance && field.Desc.Name == Ebx.GetHashCode("Name") && dbx.TrueFileName == "")
                            dbx.TrueFileName = (string)field.Value;
                    }
                    break;
                }
            case FieldType.Enum:
                {
                    int compareValue = r.ReadInt32(dbx.BigEndian);
                    var enumComplex = dbx.ComplexDescriptors[field.Desc.Ref];
                    if (!dbx.Enumerations.TryGetValue(field.Desc.Ref, out var value))
                    {
                        var enumeration = new Enumeration();
                        enumeration.Type = field.Desc.Ref;
                        enumeration.Values = new();
                        for (int i = enumComplex.FieldStartIndex; i < enumComplex.FieldStartIndex + enumComplex.NumField; i++)
                            enumeration.Values[dbx.FieldDescriptors[i].Offset] = Ebx.StringTable[dbx.FieldDescriptors[i].Name];

                        dbx.Enumerations[field.Desc.Ref] = enumeration;
                    }

                    if (value is not null)
                    {
                        if (!value.Values.TryGetValue(compareValue, out var compare))
                            field.Value = compareValue.ToString();
                        else
                            field.Value = dbx.Enumerations[field.Desc.Ref].Values[compareValue];
                    }
                    else
                        field.Value = dbx.Enumerations[field.Desc.Ref].Values[compareValue];
                    break;
                }
            case FieldType.FileRef:
                {
                    var startPos = r.BaseStream.Position;
                    var stringOffset = r.ReadInt32(dbx.BigEndian);
                    if (stringOffset == -1)
                        field.Value = "<NullRef>";
                    else
                    {
                        r.BaseStream.Position = dbx.Header.AbsStringOffset + stringOffset;
                        field.Value = r.ReadNullTerminatedString();
                        r.BaseStream.Position = startPos + 4;
                    }
    
                    if (dbx.IsPrimaryInstance && field.Desc.Name == Ebx.GetHashCode("Name") && dbx.TrueFileName == "")
                        dbx.TrueFileName = field.Value as string;
                    break;
                }
            case FieldType.Boolean:
                field.Value = r.ReadByte() == 1 ? true : false; break;
            case FieldType.Int8:
                field.Value = r.ReadSByte(); break;
            case FieldType.UInt8:
                field.Value = r.ReadByte(); break;
            case FieldType.Int16:
                field.Value = r.ReadInt16(dbx.BigEndian); break;
            case FieldType.UInt16:
                field.Value = r.ReadUInt16(dbx.BigEndian); break;
            case FieldType.Int32:
                field.Value = r.ReadInt32(dbx.BigEndian); break;
            case FieldType.UInt32:
                field.Value = r.ReadUInt32(dbx.BigEndian); break;
            case FieldType.Int64:
                field.Value = r.ReadInt64(dbx.BigEndian); break;
            case FieldType.UInt64:
                field.Value = r.ReadUInt64(dbx.BigEndian); break;
            case FieldType.Float32:
                field.Value = r.ReadSingle(dbx.BigEndian); break;
            case FieldType.Float64:
                field.Value = r.ReadDouble(dbx.BigEndian); break;
            case FieldType.GUID:
                field.Value = r.ReadGuid(dbx.BigEndian); break;
            case FieldType.SHA1:
                field.Value = r.ReadBytes(20); break;
            default:
                throw new Exception("Unknown field type " + typ);
        }
        return field;
    }
    public static void WriteField(this StreamWriter f, Field field, int lvl, string text)
    {
        // Indent
        for (int i = 0; i < lvl; i++)
        {
            f.Write("\t");
        }
        f.WriteLine(Ebx.StringTable[field.Desc.Name] + text);
    }
    public static void WriteInstance(this StreamWriter f, Complex cmplx, string text)
    {
        f.WriteLine(Ebx.StringTable[cmplx.Desc.Name] + " " + text);
    }

    #endregion


    #region GenericData
    public static GenericDataLayoutEntry ReadGDLE(this BinaryReader r, bool bigEndian, out int fieldSize)
    {
        var gdle = new GenericDataLayoutEntry();

        gdle.mMinSlot = r.ReadInt32(bigEndian);
        gdle.mMaxSlot = r.ReadInt32(bigEndian);

        fieldSize = (gdle.mMinSlot * -1) + gdle.mMaxSlot + 1;

        gdle.mDataSize = r.ReadUInt32(bigEndian);
        gdle.mAlignment = r.ReadUInt32(bigEndian);
        gdle.mStringTableOffset = r.ReadUInt32(bigEndian);
        gdle.mStringTableLength = r.ReadUInt32(bigEndian);
        gdle.mReordered = r.ReadBoolean();
        gdle.mNative = r.ReadBoolean();
        r.ReadBytes(2); // Pad
        gdle.mHash = r.ReadUInt32(bigEndian);

        // Fill data entries.
        gdle.mEntries = new GenericDataEntry[fieldSize];
        for (int j = 0; j < fieldSize; j++)
        {
        LABEL_1:
            GenericDataEntry entry = new();
            entry.mLayoutHash = r.ReadUInt32(bigEndian);
            entry.mElementSize = r.ReadUInt32(bigEndian);
            entry.mOffset = r.ReadUInt32(bigEndian);
            entry.mName = r.ReadUInt32(bigEndian);
            entry.mCount = r.ReadUInt16(bigEndian);
            entry.mFlags = (EFlags)r.ReadUInt16(bigEndian);
            entry.mElementAlign = r.ReadUInt16(bigEndian);
            entry.mRLE = r.ReadInt16(bigEndian);
            entry.mLayout = r.ReadInt64(bigEndian);

            // Sometimes there seems to be a missing entry?
            // In that case we let everything finish, but then we fix up the alignments.
            if (entry.mElementSize != 0 && entry.mCount != 0)
            {
                gdle.mEntries[j] = entry;
            }
            else
            {
                fieldSize -= 1;
                goto LABEL_1;
            }
        }

        r.ReadBytes(1); // Pad
        gdle.mName = r.ReadNullTerminatedString();

        // Fill field names.
        gdle.mFieldNames = new string[fieldSize];
        for (int j = 0; j < fieldSize; j++)
        {
            gdle.mFieldNames[j] = r.ReadNullTerminatedString();
        }

        return gdle;
    }
    public static void ReadGdDataHeader(this BinaryReader r, bool bigEndian, out uint hash, out uint type, out uint offset)
    {
        hash = (uint)r.ReadUInt64(bigEndian);
        r.ReadBytes(8);
        type = (uint)r.ReadUInt64(bigEndian);
        r.ReadBytes(4);
        offset = r.ReadUInt16(bigEndian);
        r.ReadBytes(2);
    }
    #endregion
}
