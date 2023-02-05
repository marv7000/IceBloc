using System.Numerics;

namespace IceBlocLib.Utility;

public struct Transform
{
    public Vector3 Position = new Vector3();
    public Quaternion Rotation = Quaternion.Identity;
    public Vector3 Scale = new Vector3();

    public Vector3 EulerAngles => ToEulerAngles(Rotation);
    public Matrix4x4 Matrix => Matrix4x4.Identity * Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateTranslation(Position);

    public Transform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Position = position;
        Scale = scale;
        Rotation = rotation;
    }

    public Transform(Matrix4x4 matrix)
    {
        Position = matrix.Translation;
        Rotation = Quaternion.CreateFromRotationMatrix(matrix);
        Scale = new Vector3(matrix.GetDeterminant());
    }

    public static Transform Interpolate(Transform a, Transform b, float progression)
    {
        var pos = Vector3.Lerp(a.Position, b.Position, progression);
        var rot = Quaternion.Slerp(a.Rotation, b.Rotation, progression);
        var scale = Vector3.Lerp(a.Position, b.Position, progression);
        return new(pos, rot, scale);
    }

    public static Transform operator *(Transform a, Transform b)
    {
        return new Transform(a.Position * b.Position, a.Rotation * b.Rotation, a.Scale * b.Scale);
    }
    public static Transform operator +(Transform a, Transform b)
    {
        return new Transform(a.Position + b.Position, a.Rotation + b.Rotation, a.Scale + b.Scale);
    }
    public static Transform operator -(Transform a, Transform b)
    {
        return new Transform(a.Position - b.Position, a.Rotation - b.Rotation, a.Scale - b.Scale);
    }

    public static Transform operator *(Transform a, float b)
    {
        return new Transform(a.Position * b, a.Rotation * b, a.Scale * b);
    }

    public static Vector3 ToEulerAngles(Quaternion q)
    {
        Vector3 angles = new();

        // roll / x
        float sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
        float cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        angles.X = (float)MathF.Atan2(sinr_cosp, cosr_cosp) * (180.0f / MathF.PI);

        // pitch / y
        float sinp = 2 * (q.W * q.Y - q.Z * q.X);
        if (MathF.Abs(sinp) >= 1)
        {
            angles.Y = (float)MathF.CopySign(MathF.PI / 2, sinp) * (180.0f / MathF.PI);
        }
        else
        {
            angles.Y = (float)MathF.Asin(sinp) * (180.0f / MathF.PI);
        }

        // yaw / z
        float siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
        float cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        angles.Z = (float)MathF.Atan2(siny_cosp, cosy_cosp) * (180.0f / MathF.PI);

        return angles;
    }
}
