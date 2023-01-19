using System.IO;
using System.Text;

namespace IceBloc.Frostbite;

public static class Ebx
{
    public static Dictionary<Guid, string> GuidTable = new();
    public static List<string> ParsedEbx = new();
    public static Dictionary<int, string> StringTable = new();
    public static string Seperator = "::";
    public static bool WriteArrayIndexers = true;

    public static void AddEbxGuid(string path)
    {
        if (ParsedEbx.Contains(path))
            return;

        // Add EBX GUID and name to the database.
        var dbx = new Dbx(path);
        GuidTable[dbx.FileGuid] = dbx.TrueFileName;
        ParsedEbx.Add(path);
    }

    public static int GetHashCode(string keyword)
    {
        // 32bit FNV-1 hash with FNV_offset_basis = 5381 and FNV_prime = 33
        var hash = 5381;
        foreach (var chr in Encoding.ASCII.GetBytes(keyword))
        {
            hash = (hash * 33) ^ chr;
        }
        return hash;
    }
}
public class EbxHeader
{
    public uint AbsStringOffset;
    public uint LenStringToEOF;
    public uint NumGUID;
    public uint Null;
    public uint NumInstanceRepeater;
    public uint NumComplex;
    public uint NumField;
    public uint LenName;
    public uint LenString;
    public uint NumArrayRepeater;
    public uint LenPayload;

    public EbxHeader(uint[] varList)
    {
        AbsStringOffset = varList[0];  //// absolute offset for string section start
        LenStringToEOF = varList[1];  //// length from string section start to EOF
        NumGUID = varList[2];  //// number of external GUIDs
        Null = varList[3];  //// 00000000
        NumInstanceRepeater = varList[4];
        NumComplex = varList[5];  //// number of complex entries
        NumField = varList[6];  //// number of field entries
        LenName = varList[7];  //// length of name section including padding
        LenString = varList[8];  //// length of string section including padding
        NumArrayRepeater = varList[9];
        LenPayload = varList[10]; //// length of normal payload section; the start of the array payload section is absStringOffset+lenString+lenPayload
    }
}

public class FieldDescriptor
{
    // Without this, C# will generate identical hashes for array objects,
    // which makes it so that changes on one member apply to every single one.
    public int Random = new Random().Next();

    public int Name;
    public int Type;
    public int Ref;
    public int Offset;
    public int SecondaryOffset;

    public FieldDescriptor(int[] varList)
    {
        Name = varList[0];
        Type = varList[1];
        Ref = varList[2];
        Offset = varList[3];
        SecondaryOffset = varList[4];
    }

    public FieldType GetFieldType()
    {
        return (FieldType)((Type >> 4) & 0x1F);
    }
}

public class ComplexDescriptor
{
    public int Name;
    public int FieldStartIndex;
    public int NumField;
    public int Alignment;
    public FieldType Type;
    public int Size;
    public int SecondarySize;

    public ComplexDescriptor(int[] varList)
    {
        Name = varList[0];
        FieldStartIndex = varList[1]; //the index of the first field belonging to the complex
        NumField = varList[2]; //the total number of fields belonging to the complex
        Alignment = varList[3];
        Type = (FieldType)varList[4];
        Size = varList[5]; //total length of the complex in the payload section
        SecondarySize = varList[6]; //seems deprecated
    }

    public ComplexDescriptor(int name, int fieldStartIndex, int numField, int alignment, FieldType type, int size, int secondarySize)
    {
        Name = name;
        FieldStartIndex = fieldStartIndex;
        NumField = numField;
        Alignment = alignment;
        Type = type;
        Size = size;
        SecondarySize = secondarySize;
    }
}

public class InstanceRepeater
{
    public int Null;
    public int Repetitions;
    public int ComplexIndex;

    public InstanceRepeater(int[] varList)
    {

        Null = varList[0]; //called "internalCount", seems to be always null
        Repetitions = varList[1]; //number of instance repetitions
        ComplexIndex = varList[2]; //index of complex used as the instance
}
}

public class ArrayRepeater
{
    public int Offset;
    public int Repetitions;
    public int ComplexIndex;

    public ArrayRepeater(int[] varList)
    {
        Offset = varList[0]; //offset in array payload section
        Repetitions = varList[1]; //number of array repetitions
        ComplexIndex = varList[2]; //not necessary for extraction
    }
}

public class Enumeration
{
    public int Type;
    public Dictionary<int,string> Values = new();
}

public class Complex
{
    public ComplexDescriptor Desc;
    public List<Field> Fields;
    public Guid Guid;

    public Complex(ComplexDescriptor desc)
    {
        Desc = desc;
        Fields = new();
    }

    public object Get(string name)
    {

        foreach (var field in Fields)
        {
            if (field.Desc.Name == Ebx.GetHashCode(name) && field.Desc.GetFieldType() == FieldType.Array)
            {
                return (field.Value as Complex).Fields;
            }
            else
                return field.Value;
        }

        //Go up the inheritance chain.
        foreach (var field in Fields)
        {
            if (field.Desc.GetFieldType() == FieldType.Void)
                return (field.Value as Complex).Get(name);
        }

        return null;
    }
}

public class Field
{
    public FieldDescriptor Desc;
    public object Value;

    public Field(FieldDescriptor desc)
    {
        Desc = desc;
    }

    public Complex Link(Dbx dbx)
    {
        if (Desc.GetFieldType() != FieldType.Class)
            throw new Exception("Invalid link, wrong field type\nField name: " + Desc.Name + "\nField type: " + Desc.GetFieldType() + "\nFile name: " + dbx.TrueFileName);

        if ((int)Value >> 31 == 1)
        {
            if (dbx.EbxRoot == "")
                throw new Exception("Ebx root path is not specified!");

            (Guid A, Guid B) extguid = dbx.ExternalGuids[(int)Value & 0x7fffffff];

            var extDbx = new Dbx(Path.Join(dbx.EbxRoot, Ebx.GuidTable[extguid.A] + ".ebx").ToLower());
            foreach (var instance in extDbx.Instances)
            {
                if (instance.Key == extguid.B)
                    return instance.Value;
            }
            throw new Exception("Nullguid link.\nFilename: " + dbx.TrueFileName);
        }
        else if ((int)Value != 0)
        {
            foreach(var instance in dbx.Instances)
                if (instance.Key == dbx.InternalGuids[(int)Value - 1])
                    return instance.Value;
        }
        else
            throw new Exception("Nullguid link.\nFilename: " + dbx.TrueFileName);

        throw new Exception("Invalid link, could not find target.");
    }
}

public class Dbx
{
    public string TrueFileName = "";
    public string EbxRoot = "";
    public bool BigEndian = false;
    public EbxHeader Header;
    public uint ArraySectionStart = 0;
    public Guid FileGuid;
    public Guid PrimaryInstanceGuid;
    public List<(Guid, Guid)> ExternalGuids = new();
    public List<Guid> InternalGuids = new();
    public FieldDescriptor[] FieldDescriptors;
    public ComplexDescriptor[] ComplexDescriptors;
    public InstanceRepeater[] InstanceRepeaters;
    public ArrayRepeater[] ArrayRepeaters;
    public Dictionary<Guid, Complex> Instances = new();
    public bool IsPrimaryInstance = false;
    public Complex Prim;
    public Dictionary<int, Enumeration> Enumerations = new();

    public Dbx() { }
    public Dbx(string path) : this(File.OpenRead(path)) { }
    public Dbx(Stream s)
    {
        using BinaryReader r = new BinaryReader(s);
        // metadata
        var magic = r.ReadBytes(4);
        if (magic.SequenceEqual(new byte[] { 0xCE, 0xD1, 0xB2, 0x0F })) BigEndian = false;
        else if (magic.SequenceEqual(new byte[] { 0x0F, 0xB2, 0xD1, 0xCE })) BigEndian = true;
        else throw new InvalidDataException("This file is not an EBX!");

        EbxRoot = "";
        TrueFileName = "";
        uint[] headerData = new uint[11];
        for (int i = 0; i < 11; i++)
        {
            headerData[i] = r.ReadUInt32(BigEndian);
        }
        Header = new EbxHeader(headerData);
        ArraySectionStart = Header.AbsStringOffset + Header.LenString + Header.LenPayload;
        FileGuid = r.ReadGuid(BigEndian);
        PrimaryInstanceGuid = r.ReadGuid(BigEndian);
        for (int i = 0; i < Header.NumGUID; i++)
        {
            ExternalGuids.Add((r.ReadGuid(BigEndian), r.ReadGuid(BigEndian)));
        }
        string[] keywords = Encoding.ASCII.GetString(r.ReadBytes((int)Header.LenName)).Split("\0", StringSplitOptions.RemoveEmptyEntries);

        foreach (var keyword in keywords)
        {
            Ebx.StringTable.TryAdd(Ebx.GetHashCode(keyword), keyword);
        }

        FieldDescriptors = new FieldDescriptor[Header.NumField];
        for (int i = 0; i < Header.NumField; i++)
        {
            int[] array = new int[5];
            array[0] = r.ReadInt32(BigEndian);
            array[1] = r.ReadInt16(BigEndian);
            array[2] = r.ReadInt16(BigEndian);
            array[3] = r.ReadInt32(BigEndian);
            array[4] = r.ReadInt32(BigEndian);
        
            FieldDescriptors[i] = new FieldDescriptor(array);
        }
        ComplexDescriptors = new ComplexDescriptor[Header.NumComplex];
        for (int i = 0; i < Header.NumComplex; i++)
        {
            int[] array = new int[7];
            array[0] = r.ReadInt32(BigEndian);
            array[1] = r.ReadInt32(BigEndian);
            array[2] = r.ReadByte();
            array[3] = r.ReadByte();
            array[4] = r.ReadInt16(BigEndian);
            array[5] = r.ReadInt16(BigEndian);
            array[6] = r.ReadInt16(BigEndian);
        
            ComplexDescriptors[i] = new ComplexDescriptor(array);
        }
        InstanceRepeaters = new InstanceRepeater[Header.NumInstanceRepeater];
        for (int i = 0; i < Header.NumInstanceRepeater; i++)
        {
            int[] array = new int[3];
            array[0] = r.ReadInt32(BigEndian);
            array[1] = r.ReadInt32(BigEndian);
            array[2] = r.ReadInt32(BigEndian);
        
            InstanceRepeaters[i] = new InstanceRepeater(array);
        }

        while (r.BaseStream.Position % 16 != 0)
        {
            r.BaseStream.Position += 1;
        }
        ArrayRepeaters = new ArrayRepeater[Header.NumArrayRepeater];
        for (int i = 0; i < Header.NumArrayRepeater; i++)
        {
            ArrayRepeaters[i] = new ArrayRepeater(r.ReadInt32Array(3, BigEndian));
        }

        // payload
        r.BaseStream.Position = Header.AbsStringOffset + Header.LenString;
        InternalGuids = new();
        Instances = new();
        foreach (var instanceRepeater in InstanceRepeaters)
        {
            for (int i = 0; i < instanceRepeater.Repetitions; i++)
            {
                var instanceGuid = r.ReadGuid(BigEndian);
                InternalGuids.Add(instanceGuid);

                if (instanceGuid == PrimaryInstanceGuid)
                    IsPrimaryInstance = true;
                else
                    IsPrimaryInstance = false;
                var inst = r.ReadComplex(this, instanceRepeater.ComplexIndex);
                inst.Guid = instanceGuid;

                if (IsPrimaryInstance)
                    Prim = inst;
                Instances.Add(instanceGuid, inst);
            }
        }

        if (TrueFileName == "")
            TrueFileName = Path.GetRelativePath((r.BaseStream as FileStream).Name, "").Replace("\\", "/");
    }
    public static Dbx Import(string path)
    {
        Dbx dbx = new();
        string[] lines = File.ReadAllLines(path);
        using var w = new BinaryWriter(File.OpenWrite(IO.EnsurePath(path, "_compiled.ebx")));

        List<(string[] Str, int Lvl)> tokens = new();
        for (int i = 0; i < lines.Length; i++)
        {
            tokens.Add((IO.SplitLiteral(lines[i]), IO.GetIndentCount(lines[i])));
        }

        Complex lastComplex = null;
        for (int i = 0; i < tokens.Count; i++)
        {
            var kvp = tokens[i];

            // "Partition" is always the FileGuid.
            if (kvp.Str[0] == "Partition")
                dbx.FileGuid = Guid.Parse(kvp.Str[1]);

            // If we have no indents, we're defining a new instance.
            else if (kvp.Lvl == 0)
            {
                var name = kvp.Str[0];
                var guid = Guid.Parse(kvp.Str[1]);
                
                var cdesc = new ComplexDescriptor(Ebx.GetHashCode(name),0, 0, 0, FieldType.Class, 0, 0);
                var cplx = new Complex(cdesc);

                // Mark IsPrimaryInstance
                if (guid == dbx.FileGuid)
                    dbx.IsPrimaryInstance = true;

                // Add the name with hash to the string table.
                Ebx.StringTable.TryAdd(Ebx.GetHashCode(name), name);

                dbx.Instances.Add(guid, cplx);
            }

            // If we're finished with the current indent level.
            if (tokens[i].Lvl < tokens[i-1].Lvl)
            {

            }
        }

        if (dbx.TrueFileName == "")
            dbx.TrueFileName = Path.GetFileNameWithoutExtension(path);

        return dbx;
    }

    public void Dump(string outName)
    {
        var writer = new StreamWriter(outName, false, Encoding.ASCII);

        writer.WriteLine("Partition " + FileGuid);

        foreach(var instance in Instances)
        {
            if (instance.Key == PrimaryInstanceGuid) writer.WriteInstance(instance.Value, instance.Key + " // Primary instance");
            else writer.WriteInstance(instance.Value, instance.Key.ToString());
            RecurseWrite(instance.Value.Fields, 0, writer);
        }
        writer.Close();
    }

    public byte[] Export()
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII);
        writer.WriteLine("Partition " + FileGuid);

        foreach (var instance in Instances)
        {
            if (instance.Key == PrimaryInstanceGuid) writer.WriteInstance(instance.Value, instance.Key + " // Primary instance");
            else writer.WriteInstance(instance.Value, instance.Key.ToString());
            RecurseWrite(instance.Value.Fields, 0, writer);
        }

        return stream.ToArray();
    }

    private void RecurseWrite(List<Field> fields, int lvl, StreamWriter w)
    {
        lvl += 1;
        foreach(var field in fields)
        {
            var typ = field.Desc.GetFieldType();

            if (typ == FieldType.Void || typ == FieldType.ValueType)
            {
                w.WriteField(field, lvl, "Complex", Ebx.StringTable[(field.Value as Complex).Desc.Name], false);
                RecurseWrite((field.Value as Complex).Fields, lvl, w);
            }
            else if (typ == FieldType.Class)
            {
                var towrite = "";

                if ((uint)field.Value >> 31 == 1)
                {
                    (Guid A, Guid B) extguid = ExternalGuids[(int)((uint)field.Value & 0x7fffffff)];
                    try
                    {
                        towrite = Ebx.GuidTable[extguid.A] + "/" + extguid.B.ToString();
                    }
                    catch
                    {
                        towrite = extguid.A.ToString() + "/" + extguid.B.ToString();
                    }
                }

                else if ((uint)field.Value == 0)
                    towrite = "<NullGuid>";
                else
                {
                    var intGuid = InternalGuids[(int)(uint)field.Value - 1];
                    towrite = intGuid.ToString();
                }
                w.WriteField(field, lvl, towrite, "Class", true);
            }
            else if (typ == FieldType.Array)
            {
                var arrayCmplxDesc = ComplexDescriptors[field.Desc.Ref];
                var arrayFieldDesc = FieldDescriptors[arrayCmplxDesc.FieldStartIndex];

                if ((field.Value as Complex).Fields.Count == 0)
                    w.WriteField(field, lvl, "Null", "Array", false);
                else
                {
                    int fieldCount = (field.Value as Complex).Fields.Count;
                    for (int i = 0; i < fieldCount; i++)
                    {
                        // Replace "member" with an indexer.
                        if (Ebx.WriteArrayIndexers)
                        {
                            if ((field.Value as Complex).Fields[i].Desc.Name == Ebx.GetHashCode("member"))
                            {
                                string toAdd = $"[{i}]";
                                Ebx.StringTable.TryAdd(Ebx.GetHashCode(toAdd), toAdd);
                                (field.Value as Complex).Fields[i].Desc.Name = Ebx.GetHashCode(toAdd);
                            }
                        }
                    }

                    if (arrayFieldDesc.GetFieldType() == FieldType.Enum && arrayFieldDesc.Ref == 0) 
                        w.WriteField(field, lvl, "Anon", "Enum", false);
                    else if (arrayFieldDesc.GetFieldType() == FieldType.Enum && arrayFieldDesc.Ref != 0)
                        w.WriteField(field, lvl, "Anon", "Enum", false);
                    else
                        w.WriteField(field, lvl, "[]", "Array", false);


                    RecurseWrite((field.Value as Complex).Fields, lvl, w);
                }

            }
            else if (typ == FieldType.GUID)
            {
                if (field.Value == null)
                    w.WriteField(field, lvl, "<NullGuid>");
                else
                    w.WriteField(field, lvl, field.Value.ToString());
            }
            else if (typ == FieldType.SHA1)
                w.WriteField(field, lvl, Convert.ToBase64String(field.Value as byte[]), "SHA1", true);
            else
                w.WriteField(field, lvl, 
                    (field.Value != null ? field.Value.ToString() : "<Null>"), 
                    (field.Value != null ? field.Value.GetType().Name : "Null"), true);
        }
    }

    private void RecurseRead(List<Field> fields, int lvl, string[] lines, int index)
    {

    }
    
}
