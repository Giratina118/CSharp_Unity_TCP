using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public static class Utils
    {
        public static bool WriteVector3(Vector3 vec, ref Span<byte> span, ref ushort count)
        {
            bool success = true;

            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), vec.X);
            count += sizeof(float); // x좌표
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), vec.Y);
            count += sizeof(float); // y좌표
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), vec.Z);
            count += sizeof(float); // z좌표
            return success;
        }

        public static void ReadVector3(ref Vector3 vec, ReadOnlySpan<byte> span, ref ushort count)
        {
            vec.X = BitConverter.ToSingle(span.Slice(count, span.Length - count));
            count += sizeof(float); // x좌표
            vec.Y = BitConverter.ToSingle(span.Slice(count, span.Length - count));
            count += sizeof(float); // y좌표
            vec.Z = BitConverter.ToSingle(span.Slice(count, span.Length - count));
            count += sizeof(float); // z좌표
        }
    }
}
