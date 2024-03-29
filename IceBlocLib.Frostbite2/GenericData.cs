﻿using IceBlocLib.Frostbite;
using IceBlocLib.Frostbite2.Animations.Base;
using IceBlocLib.Utility;
using System;
using System.Text;

namespace IceBlocLib.Frostbite2;

public class GenericData
{
    public Dictionary<uint, GenericDataClass> Classes = new();

    public List<(Memory<byte> Bytes, bool BigEndian)> Data = new();

    public Dictionary<Guid, int> DataMap = new();

    /// <summary>
    /// Reads a GD bank from a stream.
    /// </summary>
    public GenericData(Stream stream)
    {
        using var r = new BinaryReader(stream);

        // GD Header
        AntPackagingType packageType = (AntPackagingType)r.ReadInt32(true);
        int subDataCount = -1;

        uint dataOffset = r.ReadUInt32(true);
        int reflType = r.ReadInt32(true);
        subDataCount = r.ReadInt32(true);
        int subDataCapacity = r.ReadInt32(true);
        int ptr = r.ReadInt32(true);
        int pad = r.ReadInt32(true);

        r.BaseStream.Position = dataOffset + 4;
        // GD.STRMl block
        string strmBlock = Encoding.ASCII.GetString(r.ReadBytes(7));
        bool strmBigEndian = r.ReadByte() == 98 ? true : false; // "b" for big endian, "l" for little.
        uint strmTotalSize = r.ReadUInt32(strmBigEndian);
        uint strmIndicesOffset = r.ReadUInt32(strmBigEndian);

        // GD.REFLl block
        string reflBlock = Encoding.ASCII.GetString(r.ReadBytes(7));
        bool reflBigEndian = r.ReadByte() == 98 ? true : false;
        uint reflTotalSize = r.ReadUInt32(reflBigEndian);
        uint reflIndicesOffset = r.ReadUInt32(reflBigEndian);

        // All offsets in the REFL block reference this offset as their base.
        long reflStartOffset = r.BaseStream.Position;

        long size = r.ReadInt64(reflBigEndian);
        long[] offsets = r.ReadInt64Array((int)size, reflBigEndian);

        GenericDataLayoutEntry[] gdLayout = new GenericDataLayoutEntry[size];

        // For every class defined in the REFL block.
        for (int i = 0; i < size; i++)
        {
            // Set the stream position to the offset of the current class.
            r.BaseStream.Position = offsets[i] + reflStartOffset;

            // Read the GenericData layout entry.
            gdLayout[i] = r.ReadGDLE(reflBigEndian, out int fieldSize);

            // Convert this info to our intermediate format for easier use.
            var cl = new GenericDataClass();
            cl.Name = gdLayout[i].mName;
            cl.Alignment = (int)gdLayout[i].mAlignment;
            cl.Size = (int)gdLayout[i].mDataSize;

            // Loop through all fields of the class.
            for (int j = 0; j < fieldSize; j++)
            {
                var item = new GenericDataField();

                // Set the field values to our intermediate field.
                // Get the correct name from the string table.
                var curOffset = r.BaseStream.Position;
                r.BaseStream.Position = offsets[i] + reflStartOffset + gdLayout[i].mStringTableOffset + gdLayout[i].mEntries[j].mName;
                item.Name = r.ReadNullTerminatedString();
                r.BaseStream.Position = curOffset;

                item.Offset = (int)gdLayout[i].mEntries[j].mOffset;
                item.IsArray = gdLayout[i].mEntries[j].mFlags == EFlags.Array;

                // Get the type of the field by querying the mLayout offset and getting that type.
                long offset = r.BaseStream.Position;
                if (gdLayout[i].mEntries[j] is not null)
                {
                    // Set the stream position to the GDLE that we want to read.
                    r.BaseStream.Position = gdLayout[i].mEntries[j].mLayout + reflStartOffset;
                    // Read the GDLE.
                    var glde = r.ReadGDLE(reflBigEndian, out int _fieldSize);
                    // Get the field's type name.
                    item.Type = glde.mName;
                    item.TypeHash = glde.mHash;
                    item.IsNative = glde.mNative;
                    item.Size = glde.mDataSize;
                    item.Alignment = glde.mAlignment;
                    // Go back to our original stream position.
                    r.BaseStream.Position = offset;
                    // Add the read element.
                    cl.Elements.Add(item);
                }
            }
            // Finally, add the read class.
            Classes.Add(gdLayout[i].mHash, cl);
        }

        r.Align(4);

        // Type table
        int typeTableSize = r.ReadInt32(reflBigEndian);
        int[] typeTable = r.ReadInt32Array(typeTableSize, reflBigEndian);

        // Set this to true if you want to dump the data that's being read next. (debug)
        bool exportData = false;

        while (r.BaseStream.Position < r.BaseStream.Length)
        {
            long basePos = r.BaseStream.Position;

            string header = Encoding.ASCII.GetString(r.ReadBytes(7));
            if (header == "GD.DATA")
            {
                bool bigEndian = r.ReadByte() == 98 ? true : false;

                int dataBlockSize = r.ReadInt32(bigEndian);
                int dataBlockIndexOffset = r.ReadInt32(bigEndian);

                int dataBlockSizeDifference = dataBlockSize - dataBlockIndexOffset;

                r.ReadBytes(16); // Pad

                uint dataBlockClassType = (uint)r.ReadUInt64(bigEndian);

                // Get the data
                r.BaseStream.Position = basePos + 16;
                Memory<byte> data = r.ReadBytes(dataBlockIndexOffset - 16);
                r.ReadBytes(dataBlockSizeDifference); // Pad the indices.

                // Get the name of the file.
                r.BaseStream.Position = basePos + 16 + 32 + Classes[dataBlockClassType].Size;
                string fileName = r.ReadNullTerminatedString();

                Data.Add((data, bigEndian));

                if (exportData)
                {
                    // Save all bytes except for the header and the indices at the end.
                    string path = $"Output\\{Settings.CurrentGame}\\";
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllBytes(path + $"{fileName}.{Classes[dataBlockClassType].Name}", data.ToArray());
                }
                // Set the position to the end of the block.
                r.BaseStream.Position = basePos + dataBlockSize;
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Deserializes a GD.DATA block into native classes with defined behaviour.
    /// </summary>
    public object Deserialize(Stream stream, int index, bool bigEndian)
    {
        using var r = new BinaryReader(stream);

        r.ReadGdDataHeader(bigEndian, out uint hash, out uint type, out uint baseOffset);
        r.BaseStream.Position = 0;

        object deserializedData = null;

        GenericData gd = this;

        // Add definitions here.
        switch (gd.Classes[type].Name)
        {
            case "FrameAnimationAsset":
                deserializedData = new FrameAnimation(r.BaseStream, index, ref gd, bigEndian); break;
            case "DctAnimationAsset":
                deserializedData = new DctAnimation(r.BaseStream, index, ref gd, bigEndian); break;
            case "RawAnimationAsset":
                deserializedData = new RawAnimation(r.BaseStream, index, ref gd, bigEndian); break;
            case "CurveAnimationAsset":
                deserializedData = new CurveAnimation(r.BaseStream, index, ref gd, bigEndian); break;
            default:
                return new Animation();
                //throw new MissingMethodException($"Tried to invoke undefined behaviour for type \"{gd.Classes[type].Name}\"\nThe type is valid, but no translations for this class have been defined in IceBloc.");
        }

        return deserializedData;
    }

    public Dictionary<string, object> ReadValues(BinaryReader r, int index, uint baseOffset, uint type, bool bigEndian)
    {
        Dictionary<string, object> data = new();

        for (int x = 0; x < Classes[type].Elements.Count; x++)
        {
            GenericDataField field = Classes[type].Elements[x];
            object fieldData = null;

            // Go to the offset of the current field.
            r.BaseStream.Position = baseOffset + field.Offset;
            switch (field.Type)
            {
                case "Bool":
                    if (!field.IsArray)
                    {
                        fieldData = r.ReadBoolean();
                    }
                    break;
                case "UInt8":
                    if (!field.IsArray)
                    {
                        fieldData = r.ReadByte();
                    }
                    else
                    {
                        uint size = r.ReadUInt32(bigEndian);
                        uint capacity = r.ReadUInt32(bigEndian);
                        long offset = r.ReadInt64(bigEndian);
                        r.BaseStream.Position = offset;
                        fieldData = r.ReadUInt8Array((int)size);
                    }
                    break;
                case "Int8":
                    if (!field.IsArray)
                    {
                        fieldData = r.ReadSByte();
                    }
                    else
                    {
                        uint size = r.ReadUInt32(bigEndian);
                        uint capacity = r.ReadUInt32(bigEndian);
                        long offset = r.ReadInt64(bigEndian);
                        r.BaseStream.Position = offset;
                        fieldData = r.ReadInt8Array((int)size);
                    }
                    break;
                case "Int16":
                    if (!field.IsArray)
                    {
                        fieldData = r.ReadInt16(bigEndian);
                    }
                    else
                    {
                        uint size = r.ReadUInt32(bigEndian);
                        uint capacity = r.ReadUInt32(bigEndian);
                        long offset = r.ReadInt64(bigEndian);
                        r.BaseStream.Position = offset;
                        fieldData = r.ReadInt16Array((int)size, bigEndian);
                    }
                    break;
                case "Int32":
                    if (!field.IsArray)
                    {
                        fieldData = r.ReadInt32(bigEndian);
                    }
                    else
                    {
                        uint size = r.ReadUInt32(bigEndian);
                        uint capacity = r.ReadUInt32(bigEndian);
                        long offset = r.ReadInt64(bigEndian);
                        r.BaseStream.Position = offset;
                        fieldData = r.ReadInt32Array((int)size, bigEndian);
                    }
                    break;
                case "Int64":
                    if (!field.IsArray)
                    {
                        fieldData = r.ReadInt64(bigEndian);
                    }
                    else
                    {
                        uint size = r.ReadUInt32(bigEndian);
                        uint capacity = r.ReadUInt32(bigEndian);
                        long offset = r.ReadInt64(bigEndian);
                        r.BaseStream.Position = offset;
                        fieldData = r.ReadInt64Array((int)size, bigEndian);
                    }
                    break;
                case "UInt16":
                    if (!field.IsArray)
                    {
                        fieldData = r.ReadUInt16(bigEndian);
                    }
                    else
                    {
                        uint size = r.ReadUInt32(bigEndian);
                        uint capacity = r.ReadUInt32(bigEndian);
                        long offset = r.ReadInt64(bigEndian);
                        r.BaseStream.Position = offset;
                        fieldData = r.ReadUInt16Array((int)size, bigEndian);
                    }
                    break;
                case "UInt32":
                    if (!field.IsArray)
                    {
                        fieldData = r.ReadInt32(bigEndian);
                    }
                    else
                    {
                        uint size = r.ReadUInt32(bigEndian);
                        uint capacity = r.ReadUInt32(bigEndian);
                        long offset = r.ReadInt64(bigEndian);
                        r.BaseStream.Position = offset;
                        fieldData = r.ReadUInt32Array((int)size, bigEndian);
                    }
                    break;
                case "UInt64":
                    if (!field.IsArray)
                    {
                        fieldData = r.ReadInt64(bigEndian);
                    }
                    else
                    {
                        uint size = r.ReadUInt32(bigEndian);
                        uint capacity = r.ReadUInt32(bigEndian);
                        long offset = r.ReadInt64(bigEndian);
                        r.BaseStream.Position = offset;
                        fieldData = r.ReadUInt64Array((int)size, bigEndian);
                    }
                    break;
                case "Float":
                    if (!field.IsArray)
                    {
                        fieldData = r.ReadSingle(bigEndian);
                    }
                    else
                    {
                        uint size = r.ReadUInt32(bigEndian);
                        uint capacity = r.ReadUInt32(bigEndian);
                        long offset = r.ReadInt64(bigEndian);
                        r.BaseStream.Position = offset;
                        fieldData = r.ReadSingleArray((int)size, bigEndian);
                    }
                    break;
                case "Double":
                    if (!field.IsArray)
                    {
                        fieldData = r.ReadDouble(bigEndian);
                    }
                    else
                    {
                        uint size = r.ReadUInt32(bigEndian);
                        uint capacity = r.ReadUInt32(bigEndian);
                        long offset = r.ReadInt64(bigEndian);
                        r.BaseStream.Position = offset;
                        fieldData = r.ReadDoubleArray((int)size, bigEndian);
                    }
                    break;
                case "DataRef":
                    if (!field.IsArray)
                    {
                        fieldData = r.ReadInt64(bigEndian);
                    }
                    else
                    {
                        uint size = r.ReadUInt32(bigEndian);
                        uint capacity = r.ReadUInt32(bigEndian);
                        long offset = r.ReadInt64(bigEndian);
                        r.BaseStream.Position = offset;
                        fieldData = r.ReadGuidArray((int)size, bigEndian);
                    }
                    break;
                case "String":
                    if (!field.IsArray)
                    {
                        uint size = r.ReadUInt32(bigEndian);
                        uint capacity = r.ReadUInt32(bigEndian);
                        long offset = r.ReadInt64(bigEndian);
                        r.BaseStream.Position = offset;
                        fieldData = Encoding.ASCII.GetString(r.ReadBytes((int)size)).Replace("\0", "");
                    }
                    break;
                case "Guid":
                    if (!field.IsArray)
                    {
                        fieldData = r.ReadGuid(bigEndian);
                    }
                    else
                    {
                        uint size = r.ReadUInt32(bigEndian);
                        uint capacity = r.ReadUInt32(bigEndian);
                        long offset = r.ReadInt64(bigEndian);
                        r.BaseStream.Position = offset;
                        fieldData = r.ReadGuidArray((int)size, bigEndian);
                    }
                    break;
                default:
                    if (!field.IsArray)
                    {
                        fieldData = ReadValues(r, index, (uint)(field.Offset + baseOffset), field.TypeHash, bigEndian);
                    }
                    else
                    {
                        uint size = r.ReadUInt32(bigEndian);
                        uint capacity = r.ReadUInt32(bigEndian);
                        long offset = r.ReadInt64(bigEndian);
                        fieldData = new Dictionary<string, object>[size];
                        for (uint i = 0; i < size; i++)
                        {
                            (fieldData as Dictionary<string, object>[])[i] = 
                                ReadValues(
                                    r, index,
                                    (uint)(offset + GetAlignedSize(Classes[type].Elements[x].Size, (uint)Classes[type].Elements[x].Alignment) * i), 
                                    field.TypeHash, 
                                    bigEndian);
                        }
                    }
                    break;
            }
            data.Add(field.Name, fieldData);
        }

        data.TryGetValue("__guid", out object guid);
        if (guid != null)
            DataMap.TryAdd((Guid)guid, index);
        return data;
    }

    public Dictionary<string, object> this[Guid guid]
    {
        get 
        {
            for (int i = 0; i < Data.Count; i++)
            {
                using var s = new MemoryStream(Data[i].Bytes.ToArray());
                using var r = new BinaryReader(s);
                r.ReadGdDataHeader(Data[i].BigEndian, out uint base_hash, out uint base_type, out uint base_baseOffset);
                var values = ReadValues(r, i, base_baseOffset, base_type, Data[i].BigEndian);

                if ((Guid)values["__guid"] == guid)
                {
                    return values;
                }
            }
            return null; 
        }
    }
    public Dictionary<string, object> this[string name]
    {
        get 
        {
            for (int i = 0; i < Data.Count; i++)
            {
                using var s = new MemoryStream(Data[i].Bytes.ToArray());
                using var r = new BinaryReader(s);
                r.ReadGdDataHeader(Data[i].BigEndian, out uint base_hash, out uint base_type, out uint base_baseOffset);
                var values = ReadValues(r, i, base_baseOffset, base_type, Data[i].BigEndian);

                if ((string)values["__name"] == name)
                {
                    return values;
                }
            }
            return null; 
        }
    }

    public uint GetDataType(Guid guid)
    {
        for (int i = 0; i < Data.Count; i++)
        {
            using var s = new MemoryStream(Data[i].Bytes.ToArray());
            using var r = new BinaryReader(s);
            r.ReadGdDataHeader(Data[i].BigEndian, out uint base_hash, out uint base_type, out uint base_baseOffset);
            var values = ReadValues(r, i, base_baseOffset, base_type, Data[i].BigEndian);

            if ((Guid)values["__guid"] == guid)
            {
                return base_type;
            }
        }
        return 0;
    }

    /// <summary>
    /// Gets the first field of <paramref name="name"/> with value <paramref name="guid"/>
    /// </summary>
    public Dictionary<string, object> this[string name, Guid guid]
    {
        get 
        {
            for (int i = 0; i < Data.Count; i++)
            {
                using var s = new MemoryStream(Data[i].Bytes.ToArray());
                using var r = new BinaryReader(s);
                r.ReadGdDataHeader(Data[i].BigEndian, out uint base_hash, out uint base_type, out uint base_baseOffset);
                var values = ReadValues(r, i, base_baseOffset, base_type, Data[i].BigEndian);

                for (int x = 0; x < values.Count; x++)
                {
                    values.TryGetValue(name, out var g);
                    if (g != null)
                    {
                        if ((Guid)g == guid)
                        {
                            return values;
                        }
                    }
                }
            }
            return null; 
        }
    }

    private static uint GetAlignedSize(uint size, uint alignBy)
    {
        if (size % alignBy != 0)
            size += alignBy - (size % alignBy);
        return size;
    }

    public static void DumpAssetBank(string path)
    {
        GenericData gd = new GenericData(File.OpenRead(path));

        string exportPath = IO.EnsurePath(path, "_dump.txt");
        using var w = new StreamWriter(exportPath, false);

        w.WriteLine($"// Generated by IceBloc\n");

        w.WriteLine("\n// CLASSES\n");
        foreach (var v in gd.Classes)
        {
            w.WriteLine($"[Hash = {v.Key}, Size = {v.Value.Size}, Alignment = {v.Value.Alignment}]");
            w.WriteLine($"Class {v.Value.Name}");
            foreach (var a in v.Value.Elements)
            {
                w.WriteLine($"    [Offset = {a.Offset}]");
                if (a.IsArray)
                    w.WriteLine($"    {a.Type}[] {a.Name}");
                else
                    w.WriteLine($"    {a.Type} {a.Name}");
            }
            w.WriteLine("");
        }

        w.WriteLine("\n// DATA\n");
        for (int i = 0; i < gd.Data.Count; i++)
        {
            (Memory<byte> Bytes, bool BigEndian) v = gd.Data[i];
            using var s = new MemoryStream(v.Bytes.ToArray());
            using var r = new BinaryReader(s);
            r.ReadGdDataHeader(v.BigEndian, out uint hash, out uint type, out uint baseOffset);
            var data = gd.ReadValues(r, i, baseOffset, type, v.BigEndian);

            uint base_type = 0;

            Dictionary<string, object> baseData = new();
            if ((long)data["__base"] != 0)
            {
                r.BaseStream.Position = (long)data["__base"];
                r.ReadGdDataHeader(v.BigEndian, out uint base_hash, out base_type, out uint base_baseOffset);
                baseData = gd.ReadValues(r, i, (uint)((long)data["__base"] + base_baseOffset), base_type, v.BigEndian);
            }

            w.WriteLine($"{gd.Classes[type].Name}, Type = {type}, Base = {base_type}, {(v.BigEndian ? "BE" : "LE")}:");
            WriteMembers(w, 0, data, gd, type);
            foreach (var d in baseData)
            {
                w.Write($"    (Base) {(d.Value is null ? "<Null>" : d.Value.GetType().Name)} {d.Key}");
                if (d.Value is Array)
                {
                    w.Write($"[{(d.Value as Array).Length}] = ");
                    foreach (object val in (d.Value as Array))
                    {
                        w.Write($"{val}, ");
                    }
                    w.WriteLine("");
                }
                else
                {
                    w.WriteLine(" = " + (d.Value is null ? "<Null>" : d.Value.ToString()));
                }
            }
            w.WriteLine("");
        }

        Console.WriteLine("Saved to " + exportPath);
    }

    public static void WriteMembers(StreamWriter w, int level, Dictionary<string, object> data, GenericData gd, uint type)
    {
        // Add tabs for each layer of recursion.
        string tab = "    ";
        for (int i = 0; i < level; i++)
        {
            tab += "    ";
        }
        level++;

        // Loop through every field.
        for (int x = 0; x < data.Count; x++)
        {
            var d = data.ElementAt(x);

            // Type name.
            string tName = d.Value is null ? "<Null>" : gd.Classes[type].Elements[x].Type;

            if (tName == "LayoutEntry")
            {
                int a = 0;
            }

            // If the field is an array of a non-native type, go one level deeper for every member.
            if (d.Value is Dictionary<string, object>[] dicts)
            {
                w.WriteLine($"{tab}{tName}[{dicts.Length}] {d.Key}");
                for (int i = 0; i < dicts.Length; i++)
                {
                    w.WriteLine($"{tab}    {tName} {d.Key}[{i}]");
                    WriteMembers(w, level + 1, dicts[i], gd, gd.Classes[type].Elements[x].TypeHash);
                }
            }
            // If the field is a non-native type, go one level deeper for every member of that type.
            else if (d.Value is Dictionary<string, object> dict)
            {
                w.WriteLine($"{tab}{tName} {d.Key}");
                WriteMembers(w, level, dict, gd, gd.Classes[type].Elements[x].TypeHash);
            }
            else
            {
                w.Write($"{tab}{tName} {d.Key}");
                if (d.Value is Array a)
                {
                    w.Write($"[{a.Length}] = ");
                    for (int i = 0; i < a.Length; i++)
                    {
                        // If we're writing the last element of the array, don't write a comma at the end.
                        if (i == a.Length- 1)
                            w.Write($"{a.GetValue(i)}");
                        else
                            w.Write($"{a.GetValue(i)}, ");
                    }
                    if ((d.Value as Array).Length == 0)
                    {
                        w.Write($"<Empty>");
                    }
                    w.WriteLine("");
                }
                else
                {
                    w.WriteLine(" = " + (d.Value is null ? "<Null>" : d.Value.ToString()));
                }
            }
        }
    }

    public class GenericDataLayoutEntry
    {
        public int mMinSlot;
        public int mMaxSlot;
        public uint mDataSize;
        public uint mAlignment;
        public uint mStringTableOffset;
        public uint mStringTableLength;
        public bool mReordered;
        public bool mNative;
        public uint mHash;
        public GenericDataEntry[] mEntries;
        public string mName;
        public string[] mFieldNames;

        public override string ToString()
        {
            return $"Class, \"{mName}\"";
        }
    }

    public class GenericDataEntry
    {
        public uint mLayoutHash;
        public uint mElementSize;
        public uint mOffset;
        public uint mName;
        public ushort mCount;
        public EFlags mFlags;
        public ushort mElementAlign;
        public short mRLE;
        public long mLayout; // Layout = Field Type Offset, relative to the start of the REFL section.
    }
}

/// <summary>
/// Defines a dynamic "GD" class, with each class field stored in <see cref="Elements"/>.
/// </summary>
public class GenericDataClass
{
    public string Name;
    public int Alignment;
    public int Size;
    public List<GenericDataField> Elements = new();

    public GenericDataClass() { }

    public override string ToString()
    {
        return $"Class, \"{Name}\", Align {Alignment}, Size {Size}";
    }
}

public struct GenericDataField
{
    public string Type;
    public uint TypeHash;
    public string Name;
    public int Offset;
    public object Data;
    public bool IsArray;
    public bool IsNative;
    public uint Size;
    public uint Alignment;

    public override string ToString()
    {
        string isArray = IsArray ? "[]" : "";
        return $"Field, \"{Name}\", {Type}{isArray}, {Offset}";
    }
}

public static class GenericDataExtensions
{
    public static GenericData.GenericDataLayoutEntry ReadGDLE(this BinaryReader r, bool bigEndian, out int fieldSize)
    {
        var gdle = new GenericData.GenericDataLayoutEntry();

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
        gdle.mEntries = new GenericData.GenericDataEntry[fieldSize];
        for (int j = 0; j < fieldSize; j++)
        {
        LABEL_1:
            GenericData.GenericDataEntry entry = new();
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

    
}