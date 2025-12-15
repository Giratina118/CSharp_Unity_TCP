using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

public static class ExtensionVector3
{
    public static float Distance(this Vector3 vec, Vector3 other)
    {
        return MathF.Sqrt((vec.X - other.X) * (vec.X - other.X) + (vec.Y - other.Y) * (vec.Y - other.Y) + (vec.Z - other.Z) * (vec.Z - other.Z));
    }
}