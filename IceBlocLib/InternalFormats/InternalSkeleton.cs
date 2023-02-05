using IceBlocLib.Utility;
using System.Numerics;

namespace IceBlocLib.InternalFormats;

public sealed class InternalSkeleton
{
    public string Name = "Skeleton";
    public List<string> BoneNames = new();
    public List<int> BoneParents = new();
    public List<Transform> BoneTransforms = new();
    public List<Transform> LocalTransforms = new();

    public InternalSkeleton(string name, List<string> boneNames, List<int> boneParents, List<Transform> boneTransforms, List<Transform> localTranforms)
    {
        Name = name;
        BoneNames = boneNames;
        BoneParents = boneParents;
        BoneTransforms = boneTransforms;
        LocalTransforms = localTranforms;
    }

    public Transform GetLocalTransform(int i)
    {
        if (i > 0)
            return GetLocalTransform(BoneParents[i]) + LocalTransforms[i] - BoneTransforms[i];
        else
            return LocalTransforms[i];
    }
}
