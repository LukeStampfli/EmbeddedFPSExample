using System;
using DarkRift;
using UnityEngine;

namespace DarkriftSerializationExtensions
{
    public static class SerializationExtensions
    {

        /// <summary>
        /// Writes a Vector3 (12 bytes)
        /// </summary>
        public static void WriteVector3(this DarkRiftWriter writer, Vector3 v)
        {
            writer.Write(v.x);
            writer.Write(v.y);
            writer.Write(v.z);
        }

        /// <summary>
        /// Reads a Vector3 (12 bytes)
        /// </summary>
        public static Vector3 ReadVector3(this DarkRiftReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        /// <summary>
        /// Writes a Vector2 (8 bytes)
        /// </summary>
        public static void WriteVector2(this DarkRiftWriter writer, Vector2 v)
        {
            writer.Write(v.x);
            writer.Write(v.y);
        }

        /// <summary>
        /// Reads a Vector2 (8 bytes)
        /// </summary>
        public static Vector2 ReadVector2(this DarkRiftReader reader)
        {
            return new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        /// <summary>
        /// Writes a Quaternion (12 bytes)
        /// </summary>
        public static void WriteQuaternion(this DarkRiftWriter writer, Quaternion q)
        {
            // x*x+y*y+z*z+w*w = 1 => We don't have to send w.
            writer.Write(q.x);
            writer.Write(q.y);
            writer.Write(q.z);
        }

        /// <summary>
        /// Reads a Quaternion (12 bytes)
        /// </summary>
        public static Quaternion ReadQuaternion(this DarkRiftReader reader)
        {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            float w = Mathf.Sqrt(1f - (x * x + y * y + z * z));

            return new Quaternion(x, y, z, w);
        }

        /// <summary>
        /// Writes a quaternion compressed as 50 bits (6 bytes + 2 bits)
        /// </summary>
        public static void WriteQuaternionCompressed(this DarkRiftWriter writer, Quaternion q)
        {
            byte maxIndex = 0;
            float maxValue = float.MinValue;
            float sign = 1f;

            for (int i = 0; i < 4; i++)
            {
                float f = q[i];
                float abs = Mathf.Abs(f);
                if (abs > maxValue)
                {
                    maxValue = abs;
                    maxIndex = (byte)i;
                    sign = f > 0 ? 1 : -1;
                }
            }


            short a = 0, b = 0, c = 0;

            switch (maxIndex)
            {
                case 0:
                    a = (short)(q.y * sign * 32767f);
                    b = (short)(q.z * sign * 32767f);
                    c = (short)(q.w * sign * 32767f);
                    break;
                case 1:
                    a = (short)(q.x * sign * 32767f);
                    b = (short)(q.z * sign * 32767f);
                    c = (short)(q.w * sign * 32767f);
                    break;
                case 2:
                    a = (short)(q.x * sign * 32767f);
                    b = (short)(q.y * sign * 32767f);
                    c = (short)(q.w * sign * 32767f);
                    break;
                case 3:
                    a = (short)(q.x * sign * 32767f);
                    b = (short)(q.y * sign * 32767f);
                    c = (short)(q.z * sign * 32767f);
                    break;
            }

            writer.Write(maxIndex);
            writer.Write(a);
            writer.Write(b);
            writer.Write(c);
        }

        /// <summary>
        /// Reads a quaternion that was written by WriteQuaternionCompressed
        /// </summary>
        public static Quaternion ReadQuaternionCompressed(this DarkRiftReader reader)
        {
            byte maxIndex = reader.ReadByte();

            float a = reader.ReadInt16() / 32767f;
            float b = reader.ReadInt16() / 32767f;
            float c = reader.ReadInt16() / 32767f;
            float d = Mathf.Sqrt(1f - (a * a + b * b + c * c));

            switch (maxIndex)
            {
                case 0:
                    return new Quaternion(d, a, b, c);
                case 1:
                    return new Quaternion(a, d, b, c);
                case 2:
                    return new Quaternion(a, b, d, c);
                default:
                    return new Quaternion(a, b, c, d);
            }
        }

        private static bool GetBit(this byte b, int bitIndex)
        {
            return ((b >> bitIndex) & 1) != 0;
        }

        /// <summary>
        /// Writes an angle in radiant as 2 bytes
        /// </summary>
        public static void WriteAngle(this DarkRiftWriter writer, float angle)
        {
            ushort a = (ushort)(angle * 10430);
            writer.Write(a);
        }

        /// <summary>
        /// Reads an angle written by WriteAngle
        /// </summary>
        public static float ReadAngle(this DarkRiftReader reader)
        {
            ushort a = reader.ReadUInt16();
            return a / 10430f;
        }

    }
}