namespace IceBloc.Frostbite.Database;

public class CatalogEntry
{
    public byte[] SHA = new byte[20];
    public uint Offset;
    public int DataSize;
    public int CasFileIndex;

    public CatalogEntry() { }

    public CatalogEntry(byte[] sHA, uint offset, int dataSize, int casFileIndex)
    {
        SHA = sHA;
        Offset = offset;
        DataSize = dataSize;
        CasFileIndex = casFileIndex;
    }
}
