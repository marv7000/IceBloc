namespace IceBlocLib.Frostbite.Database;

public class CatalogEntry
{
    public byte[] SHA = new byte[20];
    public uint Offset;
    public int DataSize;
    public int CasFileIndex;
    public bool IsCompressed;

    public CatalogEntry() { }

    public CatalogEntry(byte[] sHA, uint offset, int dataSize, int casFileIndex, bool isCompressed)
    {
        SHA = sHA;
        Offset = offset;
        DataSize = dataSize;
        CasFileIndex = casFileIndex;
        IsCompressed = isCompressed;
    }
}

public static class CatalogEntryExtensions
{
    public static CatalogEntry ReadCatalogEntry(this BinaryReader reader)
    {
        CatalogEntry entry = new();
        entry.SHA = reader.ReadBytes(20);
        entry.Offset = reader.ReadUInt32();
        entry.DataSize = reader.ReadInt32();
        entry.CasFileIndex = reader.ReadInt32();

        return entry;
    }
}
