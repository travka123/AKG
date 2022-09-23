﻿using AKG.Rendering.ShaderIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Rendering.Rasterisation
{
    public abstract class Rasterisation<A, U>
    {
        public abstract void Rasterize(Vector4[,] canvas, float[,] zBuffer, List<VertexShaderOutput[]> voTriangles, ShaderProgram<A, U> shader, U uniforms, RenderingOptions options);

        protected void SetColor(Vector4[,] canvas, float[,] zBuffer, Vector4 color, Vector2 pixel, float z, object drawLocker)
        {
            lock (drawLocker)
            {
                var rz = float.MaxValue - z;

                if (zBuffer[(int)pixel.Y, (int)pixel.X] < rz)
                {
                    zBuffer[(int)pixel.Y, (int)pixel.X] = rz;
                    canvas[(int)pixel.Y, (int)pixel.X] = color;
                }
            }
        }

        protected void SetColor(Vector4[,] canvas, float[,] zBuffer, Vector4 color, int x, int y, float z, object drawLocker)
        {
            //lock (drawLocker)
            //{
                if (zBuffer[y, x] > z)
                {
                    zBuffer[y, x] = z;
                    canvas[y, x] = color;
                }
            //}
        }
    }
}
