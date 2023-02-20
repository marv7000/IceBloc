using System.Runtime.InteropServices;

namespace IceBlocLib.Frostbite2013;

public class Compression
{
    [DllImport("liblz4.dll", EntryPoint = "LZ4_decompress_safe_partial")]
    public static extern void Lz4DecompressSafePartial(nint sourceBuf, nint destBuf, int compressedSize, int uncompressedSize, int bufSize);
}
