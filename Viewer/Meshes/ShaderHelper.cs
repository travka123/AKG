﻿using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace AKG.Viewer.Meshes
{
    public static class ShaderHelper
    {
        public static Vector3 GetFromTexture(Image<Rgba32>? image, Vector3 texCords)
        {
            if (image is null)
                return Vector3.One;

            int x = (int)((image.Width - 1) * texCords.X);
            int y = (int)((image.Height - 1) * (1 - texCords.Y));

            var rgba32 = image[x, y].ToScaledVector4();

            return new Vector3(rgba32.X, rgba32.Y, rgba32.Z);
        }

        public static Vector3 Clamp(Vector3 value, float min, float max)
        {
            value.X = Math.Clamp(value.X, min, max);
            value.Y = Math.Clamp(value.X, min, max);
            value.Z = Math.Clamp(value.X, min, max);
            return value;
        }

        public static Matrix4x4 TransposeInverseMatrix(Matrix4x4 matrix)
        {
            Matrix4x4.Invert(matrix, out var inverse);
            return Matrix4x4.Transpose(inverse);
        }

        public static Vector3 NormalFromTexture(Image<Rgba32>? image, Vector3 texCords, Matrix4x4 tim, Vector3 tangent, Vector3 bitangent, Vector3 normal)
        {
            tangent = Vector3.Normalize(Vector3.Transform(tangent, tim));
            bitangent = Vector3.Normalize(Vector3.Transform(bitangent, tim));
            normal = Vector3.Normalize(Vector3.Transform(normal, tim));

            var tbn = new Matrix4x4(tangent.X, tangent.Y, tangent.Z, 0.0f,
                                    bitangent.X, bitangent.Y, bitangent.Z, 0.0f, 
                                    normal.X, normal.Y, normal.Z, 
                                    0.0f, 0.0f, 0.0f, 0.0f, 0.0f);

            return Vector3.Normalize(Vector3.Transform(GetFromTexture(image, texCords) * 2.0f - Vector3.One, tbn));
        }

        public static Vector3 NormalFromTexture(Image<Rgba32>? image, Vector3 texCords, Matrix4x4 tim, ReadOnlySpan<float> tbnVectors)
        {
            return NormalFromTexture(image, texCords, tim, new Vector3(tbnVectors[3..]), new Vector3(tbnVectors[6..]), new Vector3(tbnVectors));
        }
    }
}
