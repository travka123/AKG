using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Rendering
{
    public struct Canvas
    {
        public int height;
        public int width;
        public Vector4[] colors;
        public float[] z;
        public readonly object[] lockers;

        public Canvas(int height, int width)
        {
            this.height = height;
            this.width = width;
            colors = new Vector4[height * width];
            z = new float[height * width];

            lockers = new object[height * width];
            for (int i = 0; i < lockers.Length; i++)
                lockers[i] = new object();
        }

        public void Clear(Vector4 clearColor)
        {
            Array.Fill(colors, clearColor);
            Array.Fill(z, float.MaxValue);
        }

        public static void GetBMPColors(byte[] dest, Vector4[] colors)
        {
            int offset = 0;

            for (int i = 0; i < colors.Length; i++)
            {
                var byteColor = Vector4.Multiply(colors[i], 255);

                dest[offset + 0] = (byte)byteColor.Z;
                dest[offset + 1] = (byte)byteColor.Y;
                dest[offset + 2] = (byte)byteColor.X;
                dest[offset + 3] = (byte)byteColor.W;

                offset += 4;
            }
        }
    }
}
