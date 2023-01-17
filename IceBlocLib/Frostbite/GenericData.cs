using IceBloc.Frostbite.Animation;
using IceBloc.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IceBloc.Frostbite;

public class GenericData
{
    public Dictionary<uint, GenericDataClass> Classes = new();
    public Dictionary<Memory<byte>, bool> Data = new();

    /// <summary>
    /// Reads a GD bank from a stream.
    /// </summary>
    public GenericData(Stream stream)
    {
        using var r = new BinaryReader(stream);

        // GD Header
        AntPackagingType packageType = (AntPackagingType)r.ReadInt32(true);
        int subDataCount = -1;

        switch (packageType)
        {
            case AntPackagingType.Chunk:
                int partitions = r.ReadInt32(true);
                int reflType = r.ReadInt32(true);
                subDataCount = r.ReadInt32(true);
                int subDataCapacity = r.ReadInt32(true);
                int ptr = r.ReadInt32(true);
                int pad = r.ReadInt32(true);
                break;
            case AntPackagingType.Bundle:
            case AntPackagingType.AnimationSet:
                break;
        }

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

        // Dump the data to disk.
        while (r.BaseStream.Position < r.BaseStream.Length)
        {
            long basePos = r.BaseStream.Position;

            // Could possibly be "GD.DATAb" for big endian, according to PDB. Not really used in BF3.
            string header = Encoding.ASCII.GetString(r.ReadBytes(7));
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

            Data.Add(data, bigEndian);

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
    }

    /// <summary>
    /// Deserializes a GD.DATA block into native classes with defined behaviour. Endianess is guessed from the context.
    /// </summary>
    public object Deserialize(Stream stream)
    {
        using var r = new BinaryReader(stream);

        bool bigEndian = false;

        // If first 4 bytes are 0, we likely have a BE long.
        // TODO: Find more reliable method to determine endianess.
        if (r.ReadInt32() == 0)
        {
            bigEndian = true;
        }
        r.BaseStream.Position = 0;

        return Deserialize(stream, bigEndian);
    }

    /// <summary>
    /// Deserializes a GD.DATA block into native classes with defined behaviour.
    /// </summary>
    public object Deserialize(Stream stream, bool bigEndian)
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
                deserializedData = new FrameAnimation(r.BaseStream, ref gd, bigEndian); break;
            case "DctAnimationAsset":
                deserializedData = new DctAnimation(r.BaseStream, ref gd, bigEndian); break;
            case "RawAnimationAsset":
                deserializedData = new RawAnimation(r.BaseStream, ref gd, bigEndian); break;
            default:
                throw new MissingMethodException($"Tried to invoke undefined behaviour for type \"{gd.Classes[type].Name}\"\nThe type is valid, but no translations for this class have been defined in IceBloc.");
        }

        return deserializedData;
    }

    public Dictionary<string, object> ReadValues(BinaryReader r, uint baseOffset, uint type, bool bigEndian)
    {
        Dictionary<string, object> data = new();

        foreach (var field in Classes[type].Elements)
        {
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
                        long offset = r.ReadInt64();
                        r.BaseStream.Position = offset;
                        fieldData = r.ReadBytes((int)size);
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
                        long offset = r.ReadInt64();
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
                        long offset = r.ReadInt64();
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
                        long offset = r.ReadInt64();
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
                        long offset = r.ReadInt64();
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
                        long offset = r.ReadInt64();
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
                        long offset = r.ReadInt64();
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
                        long offset = r.ReadInt64();
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
                        long offset = r.ReadInt64();
                        r.BaseStream.Position = offset;
                        fieldData = r.ReadDoubleArray((int)size, bigEndian);
                    }
                    break;
                case "DataRef":
                    if (!field.IsArray)
                    {
                        fieldData = r.ReadInt64(bigEndian);
                    }
                    break;
                case "String":
                    if (!field.IsArray)
                    {
                        uint size = r.ReadUInt32(bigEndian);
                        uint capacity = r.ReadUInt32(bigEndian);
                        long offset = r.ReadInt64();
                        r.BaseStream.Position = offset;
                        fieldData = Encoding.ASCII.GetString(r.ReadBytes((int)size)).Replace("\0", "");
                    }
                    break;
                case "Guid":
                    if (!field.IsArray)
                    {
                        fieldData = r.ReadGuid(bigEndian);
                    }
                    break;
            }
            data.Add(field.Name, fieldData);
        }
        return data;
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
    public string Name;
    public int Offset;
    public object Data;
    public bool IsArray;

    public override string ToString()
    {
        string isArray = IsArray ? "[]" : "";
        return $"Field, \"{Name}\", {Type}{isArray}, {Offset}";
    }
}