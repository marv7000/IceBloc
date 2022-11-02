namespace IceBloc.Frostbite2;

/// <summary>
/// A <see cref="RelocPtr"/> is a simple pointer that stores a reference to a <typeparamref name="T"/> struct with additional padding;
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

}

/// <summary>
/// A collection of <see cref="RelocPtr{T}"/>, prefixed by an array size.
/// </summary>
/// <typeparam name="T"></typeparam>
public unsafe struct RelocArray<T> where T : unmanaged
{

}