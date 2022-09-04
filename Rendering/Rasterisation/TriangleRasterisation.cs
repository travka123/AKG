﻿using AKG.Rendering.ShaderIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Rendering.Rasterisation
{
    public class TriangleRasterisation<A, U> : Rasterisation<A, U>
    {
        public override void Rasterize(Vector4[,] canvas, VertexShaderOutput[] vo, ShaderProgram<A, U> shader, U uniforms)
        {
            int canvasH = canvas.GetLength(0);
            int canvasW = canvas.GetLength(1);

            var zBuffer = new float[canvasH, canvasW];

            List<VertexShaderOutput[]> voTriangles = new();

            int tLen = vo.Length - vo.Length % 3;
            for (int i = 0; i < tLen; i += 3)
            {
                var triangle = new VertexShaderOutput[3];
                triangle[0] = vo[i + 0];
                triangle[1] = vo[i + 1];
                triangle[2] = vo[i + 2];
                voTriangles.Add(triangle);
            }

            voTriangles = ClipTriangles(voTriangles, canvasW, canvasH);

            object drawLocker = new object();

            voTriangles.AsParallel().ForAll((t) =>
            {
                var drawLine = (VertexShaderOutput a, VertexShaderOutput b, Action<int, int, Vector4, float[]> callback) =>
                {
                    var aPixel = ScreenCoordinates.PixelFromNDC(a.position, canvasW, canvasW);
                    var bPixel = ScreenCoordinates.PixelFromNDC(b.position, canvasW, canvasW);

                    //DDA

                    int xl = Math.Abs((int)aPixel.X - (int)bPixel.X);
                    int yl = Math.Abs((int)aPixel.Y - (int)bPixel.Y);
                    float xNDCL = Math.Abs(a.position.X - b.position.X);
                    float yNDCL = Math.Abs(a.position.Y - b.position.Y);

                    int L = xl > yl ? xl : yl;

                    float xNDCStep = xNDCL / L;
                    float yNDCStep = yNDCL / L;
                    xNDCStep = a.position.X < b.position.X ? xNDCStep : -xNDCStep;
                    yNDCStep = a.position.Y < b.position.Y ? yNDCStep : -yNDCStep;

                    for (int i = 0; i <= L; i++)
                    {
                        var ndc = new Vector4(a.position.X + xNDCStep * i, a.position.Y + yNDCStep * i, 0, 0);
                        ndc.Z = Interpolate(a.position, b.position, a.position.Z, b.position.Z, ndc);
                        ndc.W = Interpolate(a.position, b.position, a.position.W, b.position.W, ndc);

                        var pixel = ScreenCoordinates.PixelFromNDC(ndc, canvasW, canvasH);
                        int pixelX = (int)pixel.X;
                        int pixelY = (int)pixel.Y;
                        
                        float[] varying = Interpolate(a.position, b.position, a.varying, b.varying, ndc);

                        callback(pixelX, pixelY, ndc, varying);
                    }
                };

                var drawCallback = (int pixelX, int pixelY, Vector4 ndc, float[] varying) =>
                {
                    var fo = shader.fragmentShader(new(varying, uniforms, new Vector2(pixelX, pixelY)));

                    if (fo.color is not null)
                    {
                        SetColor(canvas, zBuffer, fo.color.Value, pixelX, pixelY, ndc.Z, drawLocker);
                    }
                };

                if (true)
                {
                    var boarders = new SortedDictionary<int, List<(int x, VertexShaderOutput vo)>>();

                    var collectCallback = (int pixelX, int pixelY, Vector4 ndc, float[] varying) =>
                    {
                        var list = boarders.GetValueOrDefault(pixelY);
                        if (list is null)
                        {
                            list = new();
                            boarders[pixelY] = list;
                        }
                        list.Add((pixelX, new(ndc, varying)));
                    };

                    drawLine(t[0], t[1], collectCallback);
                    drawLine(t[1], t[2], collectCallback);
                    drawLine(t[2], t[0], collectCallback);

                    foreach ((int pixelY, var list) in boarders)
                    {
                        list.Sort((a, b) => a.x - b.x);

                        for (int i = 1; i < list.Count; i++)
                        {
                            if (list[i].x - list[i - 1].x > 1)
                            {
                                drawLine(list[i - 1].vo, list[i].vo, drawCallback);
                            }
                            else if (list[i].x - list[i - 1].x == 1)
                            {
                                drawCallback(list[i - 1].x, pixelY, list[i - 1].vo.position, list[i - 1].vo.varying);
                            }
                        }
                    }
                }
                else
                {
                    drawLine(t[0], t[1], drawCallback);
                    drawLine(t[1], t[2], drawCallback);
                    drawLine(t[2], t[0], drawCallback);
                }
            });
        }

        private float Interpolate(Vector4 pos1, Vector4 pos2, float val1, float val2, Vector4 pos)
        {
            if (pos2.X - pos1.X == 0) return val1;

            float w1 = (pos2.X - pos.X) / (pos2.X - pos1.X);
            float w2 = 1 - w1;

            return w1 * val1 + w2 * val2;
        }

        private float[] Interpolate(Vector4 pos1, Vector4 pos2, float[] val1, float[] val2, Vector4 pos)
        {
            if (pos2.X - pos1.X == 0) return val1;

            float w1 = (pos2.X - pos.X) / (pos2.X - pos1.X);
            float w2 = 1 - w1;

            float[] result = new float[val1.Length];
            for (int i = 0; i < val1.Length; i++)
            {
                result[i] = w1 * val1[i] + w2 * val2[i];
            }

            return result;
        }

        private List<VertexShaderOutput[]> ClipTriangles(List<VertexShaderOutput[]> vo, int width, int height)
        {
            //rude

            var checkVec = (Vector4 vec) =>
            {
                return vec.X >= -1 && vec.X <= 1 &&
                       vec.Y >= -1 && vec.Y <= 1 &&
                       vec.Z >= -1 && vec.Z <= 1 &&
                       vec.W >= -1 && vec.W <= 1;
            };

            return vo.Where((vo) => vo.All((vo) => checkVec(vo.position))).ToList();
        }
    }
}
