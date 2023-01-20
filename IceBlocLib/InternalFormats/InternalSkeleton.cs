using IceBlocLib.Utility;

namespace IceBlocLib.InternalFormats;

public sealed class InternalSkeleton
{
    public List<string> BoneNames = new();
    public List<int> BoneParents = new();
    public List<Transform> BoneTransforms = new();

    public InternalSkeleton(List<string> boneNames, List<int> boneParents, List<Transform> boneTransforms)
    {
        BoneNames = boneNames;
        BoneParents = boneParents;
        BoneTransforms = boneTransforms;
    }
}
