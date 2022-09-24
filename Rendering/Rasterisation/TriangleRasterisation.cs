using AKG.Rendering.ShaderIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Rendering.Rasterisation
{
    public class TriangleRasterisation<A, U> : Rasterisation<A, U>
    {
        public override void Rasterize(Vector4[,] canvas, float[,] zBuffer, List<VertexShaderOutput[]> voTriangles, ShaderProgram<A, U> shader, U uniforms, RenderingOptions options)
        {
            int canvasH = canvas.GetLength(0);
            int canvasW = canvas.GetLength(1);

            voTriangles = ClipTriangles(voTriangles, canvasW, canvasH);
            voTriangles = CullingTriangles(voTriangles);

            if (shader.geometryShader is not null)
                Parallel.ForEach(voTriangles, (vo) => shader.geometryShader(new GeometryShaderInput<U>(vo, uniforms)));

            object drawLocker = new object();

            voTriangles.AsParallel().ForAll((t) =>
            {
                var drawLine = (VertexShaderOutput a, VertexShaderOutput b, Action<int, int, VertexShaderOutput> callback) =>
                {
                    var aPixel = ScreenCoordinates.PixelFromNDC(a.position, canvasW, canvasH);
                    var bPixel = ScreenCoordinates.PixelFromNDC(b.position, canvasW, canvasH);

                    //DDA

                    int xl = Math.Abs((int)aPixel.X - (int)bPixel.X);
                    int yl = Math.Abs((int)aPixel.Y - (int)bPixel.Y);

                    int L = xl > yl ? xl : yl;

                    float xNDCStep = (b.position.X - a.position.X) / L;
                    float yNDCStep = (b.position.Y - a.position.Y) / L;

                    float xPixelStep = (bPixel.X - aPixel.X) / L;
                    float yPixelStep = (bPixel.Y - aPixel.Y) / L;

                    for (int i = 0; i < L; i++)
                    {
                        var ndc = new Vector4(
                            a.position.X + xNDCStep * i, 
                            a.position.Y + yNDCStep * i, 
                            Interpolate(0, L, a.position.Z, b.position.Z, i, a.W, b.W), 
                            1);

                        float[] varying = Interpolate(0, L, a.varying, b.varying, i, a.W, b.W);

                        int pixelX = (int)(aPixel.X + i * xPixelStep);
                        int pixelY = (int)(aPixel.Y + i * yPixelStep);

                        callback(pixelX, pixelY, new(ndc, varying, Interpolate(0, L, a.W, b.W, i, a.W, b.W)));
                    }
                };

                var drawCallback = (int pixelX, int pixelY, VertexShaderOutput vo) =>
                {
                    var fo = shader.fragmentShader(new(vo.varying, uniforms, new Vector2(pixelX, pixelY)));

                    if (fo.color is not null)
                    {
                        SetColor(canvas, zBuffer, fo.color.Value, pixelX, pixelY, vo.position.Z, drawLocker);
                    }
                };

                if (options.FillTriangles)
                {
                    var boarders = new SortedDictionary<int, SortedDictionary<int, VertexShaderOutput>>();

                    var collectCallback = (int pixelX, int pixelY, VertexShaderOutput vo) =>
                    {
                        var xDict = boarders.GetValueOrDefault(pixelY);
                        if (xDict is null)
                        {
                            xDict = new();
                            boarders[pixelY] = xDict;
                        }
                        xDict[pixelX] = vo;
                    };

                    drawLine(t[0], t[1], collectCallback);
                    drawLine(t[1], t[2], collectCallback);
                    drawLine(t[2], t[0], collectCallback);

                    foreach ((int pixelY, var xDict) in boarders)
                    {
                        var list = xDict.ToList();

                        for (int i = 1; i < list.Count; i++)
                        {
                            if (list[i].Key - list[i - 1].Key > 1)
                            {
                                drawLine(list[i - 1].Value, list[i].Value, drawCallback);
                            }
                            else if (list[i].Key - list[i - 1].Key == 1)
                            {
                                drawCallback(list[i - 1].Key, pixelY, list[i - 1].Value);
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

        private float Interpolate(float start, float end, float val1, float val2, float position)
        {
            float w1 = (end - position) / (end - start);
            float w2 = 1 - w1;

            return w1 * val1 + w2 * val2;
        }

        private float[] Interpolate(float start, float end, float[] val1, float[] val2, float position)
        {
            float w1 = (end - position) / (end - start);
            float w2 = 1 - w1;

            float[] result = new float[val1.Length];
            for (int i = 0; i < val1.Length; i++)
            {
                result[i] = w1 * val1[i] + w2 * val2[i];
            }

            return result;
        }

        private float Interpolate(float start, float end, float val1, float val2, float position, float z1, float z2)
        {
            if ((z1 == 0) || (z2 == 0)) return Interpolate(start, end, val1, val2, position);

            float w1 = (end - position) / (end - start);
            float w2 = 1 - w1;

            float d = w1 / z1 + w2 / z2;

            return (w1 * val1 / z1 + w2 * val2 / z2) / d;
        }

        private float[] Interpolate(float start, float end, float[] val1, float[] val2, float position, float z1, float z2)
        {
            if ((z1 == 0) || (z2 == 0)) return Interpolate(start, end, val1, val2, position);

            float w1 = (end - position) / (end - start);
            float w2 = 1 - w1;

            float d = w1 / z1 + w2 / z2;

            float[] result = new float[val1.Length];
            for (int i = 0; i < val1.Length; i++)
            {
                result[i] = (w1 * val1[i] / z1 + w2 * val2[i] / z2) / d;
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
                       vec.Z >= -1 && vec.Z <= 1;
            };

            return vo.AsParallel().Where((vo) => vo.All((vo) => checkVec(vo.position))).ToList();
        }

        private List<VertexShaderOutput[]> CullingTriangles(List<VertexShaderOutput[]> vo)
        {
            var isCounterClockWise = (VertexShaderOutput[] triangle) =>
            {
                return triangle[0].position.X * triangle[1].position.Y - triangle[1].position.X * triangle[0].position.Y +
                triangle[1].position.X * triangle[2].position.Y - triangle[2].position.X * triangle[1].position.Y +
                triangle[2].position.X * triangle[0].position.Y - triangle[0].position.X * triangle[2].position.Y > 0;

            };
            return vo.AsParallel().Where((vo) => isCounterClockWise(vo)).ToList();
        }
    }
}
