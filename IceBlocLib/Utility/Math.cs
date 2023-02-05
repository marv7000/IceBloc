namespace IceBlocLib.Utility;

public static class Math
{
    public static float DegRad(this float x)
    {
        return x * MathF.PI / 180.0f;
    }
}