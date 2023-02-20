using IceBlocLib.InternalFormats;

using SELib;

namespace IceBlocLib.Utility.Export;

public class ModelExporterSEMODEL : IModelExporter
{
    public void Export(InternalMesh mesh, string path)
    {
        throw new InvalidDataException("SEModel requires a skeleton.");
    }

    public void Export(InternalMesh mesh, InternalSkeleton skeleton, string path)
    {
        // Start writing to disk.
        using var s = File.OpenWrite(path + ".semodel");
        SEModel model = new SEModel();

        SEModelMesh seMesh = new SEModelMesh();

        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            SEModelVertex v = new();
            v.Position.X = mesh.Vertices[i].PositionX;
            v.Position.Y = mesh.Vertices[i].PositionY;
            v.Position.Z = mesh.Vertices[i].PositionZ;
            v.VertexNormal.X = mesh.Vertices[i].NormalX;
            v.VertexNormal.Y = mesh.Vertices[i].NormalY;
            v.VertexNormal.Z = mesh.Vertices[i].NormalZ;

            SEModelWeight w1 = new SEModelWeight()
            {
                BoneIndex = (uint)mesh.Vertices[i].BoneIndexA,
                BoneWeight = mesh.Vertices[i].BoneWeightA
            };
            SEModelWeight w2 = new SEModelWeight()
            {
                BoneIndex = (uint)mesh.Vertices[i].BoneIndexB,
                BoneWeight = mesh.Vertices[i].BoneWeightB
            };
            SEModelWeight w3 = new SEModelWeight()
            {
                BoneIndex = (uint)mesh.Vertices[i].BoneIndexC,
                BoneWeight = mesh.Vertices[i].BoneWeightC
            };
            SEModelWeight w4 = new SEModelWeight()
            {
                BoneIndex = (uint)mesh.Vertices[i].BoneIndexD,
                BoneWeight = mesh.Vertices[i].BoneWeightD
            };
            
            v.Weights.Add(w1);
            v.Weights.Add(w2);
            v.Weights.Add(w3);
            v.Weights.Add(w4);

            seMesh.AddVertex(v);

        }

        for (int i = 0; i < mesh.Faces.Count; i++)
        {
            seMesh.AddFace((uint)mesh.Faces[i].A, (uint)mesh.Faces[i].B, (uint)mesh.Faces[i].C);
        }

        var mat = new SEModelMaterial()
        {
            Name = mesh.Name != "" ? mesh.Name : "Unnamed_Material",
        };

        model.AddMaterial(mat);

        model.AddMesh(seMesh);

        model.Write(s);
    }

    public void Export(List<InternalMesh> meshes, InternalSkeleton skeleton, string path)
    {
        // Start writing to disk.
        using var s = File.Open(path + ".semodel", FileMode.Create);
        SEModel model = new SEModel();

        for (int i = 0; i < skeleton.BoneNames.Count; i++)
        {
            model.AddBone(skeleton.BoneNames[i], skeleton.BoneParents[i], 
                skeleton.BoneTransforms[i].Position, skeleton.BoneTransforms[i].Rotation,
                skeleton.LocalTransforms[i].Position, skeleton.LocalTransforms[i].Rotation,
                new Vector3(1.0f, 1.0f, 1.0f));
        }
        int matIdx = 0;
        foreach (var mesh in meshes)
        {
            SEModelMesh seMesh = new SEModelMesh();

            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                SEModelVertex v = new();
                v.Position = new(mesh.Vertices[i].PositionX, mesh.Vertices[i].PositionY, mesh.Vertices[i].PositionZ);
                v.VertexNormal = new(mesh.Vertices[i].NormalX, mesh.Vertices[i].NormalY, mesh.Vertices[i].NormalZ);
                v.UVSets.Add(new Vector2(mesh.Vertices[i].TexCoordX, mesh.Vertices[i].TexCoordY));

                SEModelWeight w1 = new SEModelWeight();
                w1.BoneIndex = (uint)mesh.Vertices[i].BoneIndexA;
                w1.BoneWeight = mesh.Vertices[i].BoneWeightA;
                SEModelWeight w2 = new SEModelWeight();
                w2.BoneIndex = (uint)mesh.Vertices[i].BoneIndexB;
                w2.BoneWeight = mesh.Vertices[i].BoneWeightB;
                SEModelWeight w3 = new SEModelWeight();
                w3.BoneIndex = (uint)mesh.Vertices[i].BoneIndexC;
                w3.BoneWeight = mesh.Vertices[i].BoneWeightC;
                SEModelWeight w4 = new SEModelWeight();
                w4.BoneIndex = (uint)mesh.Vertices[i].BoneIndexD;
                w4.BoneWeight = mesh.Vertices[i].BoneWeightD;

                v.Weights.Add(w1);
                v.Weights.Add(w2);
                v.Weights.Add(w3);
                v.Weights.Add(w4);

                seMesh.AddVertex(v);
            }

            for (int i = 0; i < mesh.Faces.Count; i++)
            {
                seMesh.AddFace((uint)mesh.Faces[i].A, (uint)mesh.Faces[i].B, (uint)mesh.Faces[i].C);
            }

            var mat = new SEModelMaterial()
            {
                Name = mesh.Name != "" ? mesh.Name : "Unnamed_Material",
                MaterialData = new SEModelSimpleMaterial(),
            };
            model.AddMaterial(mat);
            seMesh.AddMaterialIndex(matIdx);
            matIdx++;
            model.AddMesh(seMesh);
        }

        model.Write(s);
    }
}
