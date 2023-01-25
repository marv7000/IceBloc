using System.Runtime.InteropServices;

namespace IceBlocLib.Utility;

public static class UnsafeOperations
{
    public static unsafe T StructFromMemory<T>(Memory<byte> data, int offset = 0)
    {
        var handle = data.Pin();
        nint ptr = (nint)handle.Pointer;
        T val = Marshal.PtrToStructure<T>(ptr);
        handle.Dispose();
        return val;
    }


}
