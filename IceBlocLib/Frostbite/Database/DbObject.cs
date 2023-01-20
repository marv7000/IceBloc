using System.Numerics;
using System.Text;
using IceBlocLib.Utility;

namespace IceBlocLib.Frostbite.Database;

public class DbObject
{
    public string Name = "";
    public DbObjectType ObjectType;
    public object Data;

    public DbObject(BinaryReader reader)
    {
        var header = reader.ReadByte();
        ObjectType = (DbObjectType)(header & 0x1F);
        var flags = header >> 5;
        if (flags != 0x04)
            Name = reader.ReadNullTerminatedString();

        switch (ObjectType)
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
                }
                break;
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
                }
                break;
            case DbObjectType.Null:
                break;
            case DbObjectType.ObjectId:
                Data = new string(reader.ReadChars(12)); break;
            case DbObjectType.Bool:
                Data = reader.ReadBoolean(); break;
            case DbObjectType.String:
                {
                    var data = reader.ReadChars(reader.ReadLEB128() - 1);
                    Data = new string(data);
                    reader.BaseStream.Position += 1;
                }
                break;
            case DbObjectType.Integer:
                Data = reader.ReadInt32(); break;
            case DbObjectType.Long:
                Data = reader.ReadInt64(); break;
            case DbObjectType.VarInt:
                {
                    var val = reader.ReadLEB128();
                    Data = val >> 1 ^ val & 1;
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
                Data = reader.ReadMatrix4x4(); break;
            case DbObjectType.Blob:
                var dataSize = reader.ReadLEB128();
                Data = Encoding.ASCII.GetString(reader.ReadBytes(dataSize)).Replace("\0", ""); break;
            case DbObjectType.Attachment:
                Data = reader.ReadBytes(20); break;
            case DbObjectType.Timespan:
                Data = reader.ReadDBTimeSpan(); break;
            case DbObjectType.Eoo:
                break;
            default:
                throw new Exception($"Unhandled DB object type {ObjectType} at {reader.BaseStream.Position}.");
        }
    }

    /// <summary>
    /// Checks to see if a <see cref="DbObject"/> is XOR encrypted and decrypts it if it is.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>The decrypted file as a byte array.</returns>
    public static DbObject UnpackDbObject(string filePath, out bool wasCached)
    {
        wasCached = IO.DecryptAndCache(filePath);

        using var r = new BinaryReader(File.OpenRead($"Cache\\{Settings.CurrentGame}\\{Path.GetFileName(filePath)}"));
        // Use decrypted data to create a DbObject structure.
        return new DbObject(r);
    }

    /// <summary>
    /// Gets the <see cref="DbObject"/> of a field with the given name
    /// </summary>
    public DbObject GetField(string name)
    {
        try
        {
            foreach (var element in Data as List<DbObject>)
            {
                if (element.Name == name) return element;
            }
            return null;
        }
        catch
        {
            Console.WriteLine($"Couldn't find the requested field {name} in the DbObject!");
            return null;
        }
    }

    public override string ToString()
    {
        if (ObjectType == DbObjectType.Array)
            return $"<{Name}, {ObjectType}, Size = {(Data as List<DbObject>).Count}>";
        else if (ObjectType == DbObjectType.Object)
            return $"<{Name}, {ObjectType}, Entries = {(Data as List<DbObject>).Count}>";
        else
            return $"<{Name}, {ObjectType}, {Data}>";
    }
}

/// <summary>
/// Only usable for root Db nodes.
/// </summary>
public struct DbMetaData
{
    public string Name;
    public long TotalSize;
    public bool AlwaysEmitSuperBundle;
}

public struct DbRecordId
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

    public override string ToString()
    {
        return "" + Value.TotalSeconds;
    }
}