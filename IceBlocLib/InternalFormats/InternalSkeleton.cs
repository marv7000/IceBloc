using IceBlocLib.Utility;

namespace IceBlocLib.InternalFormats;

public sealed class InternalSkeleton
{
    public string Name = "Skeleton";
    public List<string> BoneNames = new();
    public List<int> BoneParents = new();
    public List<Transform> BoneTransforms = new();

    public InternalSkeleton(string name, List<string> boneNames, List<int> boneParents, List<Transform> boneTransforms)
    {
        Name = name;
        BoneNames = boneNames;
        BoneParents = boneParents;
        BoneTransforms = boneTransforms;
    }
}
