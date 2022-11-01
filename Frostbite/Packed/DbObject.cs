using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using IceBloc.Utility;

namespace IceBloc.Frostbite.Packed;

public class DbObject
{
    public string Name { get; set; }

    public DbObjectType ObjectType;

    public object? Data { get; set; }

    public DbObject(BinaryReader reader)
    {
        var header = reader.ReadByte();
        ObjectType = (DbObjectType)(header & 0x1F);
        var flags = header >> 5;
        if (flags == 0x04)
            Name = null;
        else
            Name = reader.ReadNullTerminatedString();

        switch(ObjectType)
        {
            case DbObjectType.Array:
                {
                    var listLength = reader.ReadLEB128();
                    var list = new List<DbObject>();
                    var endPos = reader.BaseStream.Position + listLength;
                    while (reader.BaseStream.Position < endPos - 1)
                        list.Add(new DbObject(reader));
                    if (reader.ReadByte() != 0x00)
                        throw new InvalidDataException("Array does not end with 0x00 byte. Position: " + reader.BaseStream.Position);
                    Data = list;
                } break;
            case DbObjectType.Object:
                {
                    var list = new List<DbObject>();
                    var entrySize = reader.ReadLEB128();
                    var endPos = reader.BaseStream.Position + entrySize;
                    while (reader.BaseStream.Position < endPos - 1)
                    {
                        var dbo = new DbObject(reader);
                        list.Add(dbo);
                    }
                    if (reader.ReadByte() != 0x00)
                        throw new InvalidDataException("Entry does not end with 0x00 byte. Position: " + reader.BaseStream.Position);
                    Data = list;
                } break;
            case DbObjectType.Null:
                break;
            case DbObjectType.ObjectId:
                Data = new string(reader.ReadChars(12)); // Hash
                break;
            case DbObjectType.Bool:
                Data = reader.ReadBoolean();
                break;
            case DbObjectType.String:
                {
                    var data = reader.ReadChars(reader.ReadLEB128() - 1);
                    Data = new string(data);
                    reader.BaseStream.Position += 1;
                } break;
            case DbObjectType.Integer:
                Data = reader.ReadInt32(); break;
            case DbObjectType.Long:
                Data = reader.ReadInt64(); break;
            case DbObjectType.VarInt:
                {
                    var val = reader.ReadLEB128();
                    Data = (val >> 1) ^ (val & 1);
                }
                break;
            case DbObjectType.Float:
                Data = reader.ReadSingle(); break;
            case DbObjectType.Double:
                Data = reader.ReadDouble(); break;
            case DbObjectType.Timestamp:
                Data = reader.ReadUInt64(); break;
            case DbObjectType.RecordId:
                Data = new DbRecordId(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16()); break;
            case DbObjectType.Guid:
                Data = new Guid(reader.ReadBytes(16)); break;
            case DbObjectType.SHA1:
                Data = reader.ReadBytes(20); break;
            case DbObjectType.Vector4:
                Data = new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()); break;
            case DbObjectType.Matrix44:
                Data = new Matrix4x4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                                    reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                                    reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                                    reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()); break;
            case DbObjectType.Blob:
                Data = reader.ReadLEB128(); break;
            case DbObjectType.Attachment:
                Data = reader.ReadBytes(20); break;
            case DbObjectType.Timespan:
                Data = new DbTimespan(reader); break;
            default:
                break;
        }
            throw new Exception($"Unhandled DB object type {ObjectType} at {reader.BaseStream.Position}.");
    }

    /// <summary>
    /// Checks to see if a <see cref="DbObject"/> is XOR encrypted and decrypts it if it is.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>The decrypted file as a byte array.</returns>
    public static DbObject UnpackDbObject(string filePath)
    {
        byte[] data;

        // Decrypt file if neccessary.
        using (var stream = File.OpenRead(filePath))
        {
            using var reader = new BinaryReader(stream);

            var magic = reader.ReadBytes(4);
            if (magic.SequenceEqual(new byte[] { 0x00, 0xD1, 0xCE, 0x00 }))
            {
                reader.BaseStream.Position = 296; // Skip the signature.
                var key = new byte[260];
                for (int i = 0; i < 260; i++)
                {
                    key[i] = (byte)(reader.ReadByte() ^ 0x7B);
                }
                var encryptedData = reader.ReadUntilStreamEnd();
                data = new byte[encryptedData.Length];
                for (int i = 0; i < encryptedData.Length; i++)
                {
                    data[i] = (byte)(key[i % 257] ^ encryptedData[i]);
                }
            }
            else if (magic.SequenceEqual(new byte[] { 0x00, 0xD1, 0xCE, 0x01 }) || magic.SequenceEqual(new byte[] { 0x00, 0xD1, 0xCE, 0x03 }))
            {
                reader.BaseStream.Position = 556; // skip signature + skip empty key
                data = reader.ReadUntilStreamEnd();
            }
            else
            {
                reader.BaseStream.Position = 0;
                data = reader.ReadUntilStreamEnd();
            }
        }

        // Use decrypted data to create a DbObject structure.
        using (var stream = new MemoryStream(data))
        {
            using var reader = new BinaryReader(stream);
            return new DbObject(reader);
        }
    }

    /// <summary>
    /// Gets the <see cref="DbObject"/> of a field with the given name
    /// </summary>
    public DbObject GetField(string name)
    {
        try
        {
            foreach(var element in Data as List<DbObject>)
            {
                if(element.Name == name)
                {
                    return element;
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public override string ToString()
    {
        return $"{Name},{Data}";
    }
}

/// <summary>
/// Only usable for root Db nodes.
/// </summary>
public class DbMetaData
{
    public string Name { get; set; }
    public long TotalSize { get; set; }
    public bool AlwaysEmitSuperBundle { get; set; }
}

public class DbRecordId
{
    public ushort ExtentId;
    public ushort PageId;
    public ushort SlotId;

    public DbRecordId(ushort extentId, ushort pageId, ushort slotId)
    {
        ExtentId = extentId;
        PageId = pageId;
        SlotId = slotId;
    }
}

public class DbTimespan
{
    public TimeSpan Value;

    public DbTimespan(BinaryReader reader)
    {
        var val = (ulong)reader.ReadLEB128();
        ulong lower = (val & 0x00000000FFFFFFFF);
        var upper = (val & 0xFFFFFFFF00000000) >> 32;
        var flag = lower & 1;
        var span = ((lower >> 1) ^ flag) | (((upper >> 1) ^ flag) << 32);
        Value = new TimeSpan((long)span);
    }

    public override string ToString()
    {
        return "" + Value.TotalSeconds;
    }
}