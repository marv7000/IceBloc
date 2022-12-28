using IceBloc.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media.Animation;

namespace IceBloc.Frostbite;

public class GenericData
{
    public Dictionary<uint, GenericDataClass> Classes = new();

    /// <summary>
    /// Creates an empty GD entity.
    /// </summary>
    public GenericData()
    {

    }

    /// <summary>
    /// Creates a GD entity from a given set of classes.
    /// </summary>
    public GenericData(Dictionary<uint, GenericDataClass> classes)
    { 
        Classes = classes; 
    }

    /// <summary>
    /// Reads a GD bank from a stream.
    /// </summary>
    public GenericData(Stream stream)
    {
        using var r = new BinaryReader(stream);

        // GD Header
        int version = r.ReadInt32(true);
        int partitions= r.ReadInt32(true);
        int reflType = r.ReadInt32(true);
        int subDataCount = r.ReadInt32(true);
        int subDataCapacity = r.ReadInt32(true);
        int ptr = r.ReadInt32(true);
        int pad = r.ReadInt32(true);

        // GD.STRMl block
        string strmBlock = Encoding.ASCII.GetString(r.ReadBytes(8));
        uint strmTotalSize = r.ReadUInt32();
        uint strmIndicesOffset = r.ReadUInt32();

        // GD.REFLl block
        string reflBlock = Encoding.ASCII.GetString(r.ReadBytes(8));
        uint reflTotalSize = r.ReadUInt32();
        uint reflIndicesOffset = r.ReadUInt32();

        // All offsets in the REFL block reference this offset as their base.
        long reflStartOffset = r.BaseStream.Position;

        long size = r.ReadInt64();
        long[] offsets = r.ReadInt64Array((int)size, false);

        GenericDataLayoutEntry[] gdLayout = new GenericDataLayoutEntry[size];

        // For every class defined in the REFL block.
        for (int i = 0; i < size; i++)
        {
            // Set the stream position to the offset of the current class.
            r.BaseStream.Position = offsets[i] + reflStartOffset;

            // Read the GenericData layout entry.
            gdLayout[i] = r.ReadGDLE(out int fieldSize);

            // Convert this info to our intermediate format for easier use.
            var cl = new GenericDataClass();
            cl.Name = gdLayout[i].mName;
            cl.Alignment = (int)gdLayout[i].mAlignment;
            cl.Size = (int)gdLayout[i].mDataSize;

            // Loop through all fields of the class.
            for (int j = 0; j < fieldSize; j++)
            {
                var item = new GenericDataElement();

                // Set the field values to our intermediate field.
                item.Name = gdLayout[i].mFieldNames[j];
                item.Offset = (int)gdLayout[i].mEntries[j].mOffset;
                item.IsArray = gdLayout[i].mEntries[j].mFlags == EFlags.Array;

                // Get the type of the field by querying the mLayout offset and getting that type.
                long offset = r.BaseStream.Position;
                if (gdLayout[i].mEntries[j] is not null)
                {
                    // Set the stream position to the GDLE that we want to read.
                    r.BaseStream.Position = gdLayout[i].mEntries[j].mLayout + reflStartOffset;
                    // Read the GDLE.
                    var glde = r.ReadGDLE(out int _fieldSize);
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
        int typeTableSize = r.ReadInt32();
        int[] typeTable = r.ReadInt32Array(typeTableSize, false);

        // Set this to true if you want to dump the data that's being read next. (debug)
        bool exportData = false;

        // Dump the data to disk.
        if (exportData)
        {
            for (int i = 0; i < subDataCount; i++)
            {
                long basePos = r.BaseStream.Position;

                // Could possibly be "GD.DATAb" for big endian, according to PDB.
                bool bigEndian = false;
                string header = Encoding.ASCII.GetString(r.ReadBytes(8));
                if (header == "GD.DATAl")
                    bigEndian = false;
                else if (header == "GD.DATAb")
                    bigEndian = true;
                else
                    throw new InvalidDataException($"Expected format \"GD.DATAl\" or \"GD.DATAb\", but got \"{header}\"");

                int dataBlockSize = r.ReadInt32(bigEndian);
                int dataBlockIndexOffset = r.ReadInt32(bigEndian);

                int dataBlockSizeDifference = dataBlockSize - dataBlockIndexOffset;

                r.ReadBytes(16); // Pad

                uint dataBlockClassType = r.ReadUInt32(bigEndian);

                // Get the data
                r.BaseStream.Position = basePos + 16;
                byte[] data = r.ReadBytes(dataBlockIndexOffset - 16);
                r.ReadBytes(dataBlockSizeDifference); // Pad the indices.

                // Get the name of the file.
                r.BaseStream.Position = basePos + 16 + 32 + Classes[dataBlockClassType].Size;
                string fileName = r.ReadNullTerminatedString();

                // Save all bytes except for the header and the indices at the end.
                string path = $"Output\\{Settings.CurrentGame}\\";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path + $"{fileName}.{Classes[dataBlockClassType].Name}", data);

                // Set the position to the end of the block.
                r.BaseStream.Position = basePos + dataBlockSize;
            }
        }
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
                        fieldData = Encoding.ASCII.GetString(r.ReadBytes((int)size - 1)); // -1 to ignore \0 at the end
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
    public List<GenericDataElement> Elements = new();

    public GenericDataClass() { }

    public override string ToString()
    {
        return $"Class, \"{Name}\", Align {Alignment}, Size {Size}";
    }
}

public struct GenericDataElement
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