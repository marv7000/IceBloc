using IceBlocLib.Frostbite;
using IceBlocLib.InternalFormats;
using IceBlocLib.Utility;
using System.Numerics;

namespace IceBlocLib.Frostbite2.Misc;

public class SkeletonAsset
{
    public static InternalSkeleton ConvertToInternal(in Dbx dbx)
    {

        var boneNames = new List<string>();
        var boneHierarchy = new List<int>();
        var modelPose = new List<Transform>();
        var modelRotations = new List<Vector3>();

        Field b = dbx.Prim["BoneNames"];
        foreach (var name in (b.Value as Frostbite.Complex).Fields)
        {
            boneNames.Add((string)name.Value);
        }
        b = dbx.Prim["Hierarchy"];
        foreach (var index in (b.Value as Frostbite.Complex).Fields)
        {
            boneHierarchy.Add((int)index.Value);
        }
        for (int i = 0; i < (b.Value as Frostbite.Complex).Fields.Count; i++)
        {
            Vector3 right = new(
                (float)dbx.Prim["ModelPose"][i]["right"]["x"].Value,
                (float)dbx.Prim["ModelPose"][i]["right"]["y"].Value,
                (float)dbx.Prim["ModelPose"][i]["right"]["z"].Value
                ); 
            Vector3 up = new(
                (float)dbx.Prim["ModelPose"][i]["up"]["x"].Value,
                (float)dbx.Prim["ModelPose"][i]["up"]["y"].Value,
                (float)dbx.Prim["ModelPose"][i]["up"]["z"].Value
                );
            Vector3 forward = new(
                (float)dbx.Prim["ModelPose"][i]["forward"]["x"].Value,
                (float)dbx.Prim["ModelPose"][i]["forward"]["y"].Value,
                (float)dbx.Prim["ModelPose"][i]["forward"]["z"].Value
                );
            Vector3 trans = new(
                (float)dbx.Prim["ModelPose"][i]["trans"]["x"].Value,
                (float)dbx.Prim["ModelPose"][i]["trans"]["y"].Value,
                (float)dbx.Prim["ModelPose"][i]["trans"]["z"].Value
                );
            Matrix4x4 rotation = new(
                right.X, right.Y, right.Z, 0,
                up.X, up.Y, up.Z, 0,
                forward.X, forward.Y, forward.Z, 0,
                0f, 0f, 0f, 1f);

            var transform = new Transform(trans, Quaternion.CreateFromRotationMatrix(rotation), Vector3.One);

            modelPose.Add(transform);
            modelRotations.Add(transform.EulerAngles);
        }

        return new InternalSkeleton(boneNames, boneHierarchy, modelPose);
    }
}
