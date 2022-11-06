namespace IceBloc.Frostbite2;

/// <summary>
/// A <see cref="RelocPtr{T}"/> is a simple pointer that stores a reference to a <typeparamref name="T"/> struct with additional padding;
/// </summary>
public unsafe struct RelocPtr<T> where T : unmanaged
{
    public T* Ptr;
    public int Pad;
}

/// <summary>
/// Like <see cref="RelocPtr{T}"/>, but for NUL-terminated C strings.
/// </summary>
public unsafe struct RelocPtrStr
{
    // Note: This is a byte* and not char*, because in C# a char has a 2-byte character width.
    //public byte* Ptr;
    //public byte Pad;
}

/// <summary>
/// A collection of <see cref="RelocPtr{T}"/>, prefixed by an array size.
/// </summary>
public unsafe struct RelocArray<T> where T : unmanaged
{
    public uint Size;
    public T*[] Ptr;
}