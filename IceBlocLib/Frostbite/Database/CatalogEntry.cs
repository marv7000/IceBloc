namespace IceBloc.Frostbite.Database;

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
