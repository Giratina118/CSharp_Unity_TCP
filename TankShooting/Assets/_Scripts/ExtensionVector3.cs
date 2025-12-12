using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionVector3
{
    public static System.Numerics.Vector3 ConvertToNumericV3(this Vector3 vec)
    {
        return new System.Numerics.Vector3(vec.x, vec.y, vec.z);
    }

    public static Vector3 ConvertToUnityV3(this System.Numerics.Vector3 vec)
    {
        return new Vector3(vec.X, vec.Y, vec.Z);
    }

    public static bool WriteByte(this Vector3 vec, Span<byte> span, ref ushort count)
    {
        bool success = true;
        System.Numerics.Vector3 sendV3 = vec.ConvertToNumericV3(); 

        success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), sendV3.X);
        count += sizeof(float); // xÁÂÇ¥
        success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), sendV3.X);
        count += sizeof(float); // yÁÂÇ¥
        success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), sendV3.X);
        count += sizeof(float); // zÁÂÇ¥

        return success;
    }

    public static void ReadByte(this Vector3 vec, ReadOnlySpan<byte> span, ref ushort count)
    {
        vec.x = BitConverter.ToSingle(span.Slice(count, span.Length - count));
        count += sizeof(float); // xÁÂÇ¥
        vec.y = BitConverter.ToSingle(span.Slice(count, span.Length - count));
        count += sizeof(float); // yÁÂÇ¥
        vec.z = BitConverter.ToSingle(span.Slice(count, span.Length - count));
        count += sizeof(float); // zÁÂÇ¥
    }
}
