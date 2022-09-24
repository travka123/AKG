using AKG.Rendering.ShaderIO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Rendering.Rasterisation
{
    public class TriangleRasterisation<A, U> : Rasterisation<A, U>
    {
        public override void Rasterize(Canvas canvas, List<VertexShaderOutput[]> voTriangles, ShaderProgram<A, U> shader, U uniforms, RenderingOptions options)
        {
            voTriangles = ClipTriangles(voTriangles, canvas.width, canvas.height);
            voTriangles = CullingTriangles(voTriangles);

            if (shader.geometryShader is not null)
                Parallel.ForEach(voTriangles, (vo) => shader.geometryShader(new GeometryShaderInput<U>(vo, uniforms)));

            object drawLocker = new object();

            var drawLine = (VertexShaderOutput a, VertexShaderOutput b, Action<int, int, VertexShaderOutput> callback) =>
            {
                var aPixel = ScreenCoordinates.PixelFromNDC(a.position, canvas.width, canvas.height);
                var bPixel = ScreenCoordinates.PixelFromNDC(b.position, canvas.width, canvas.height);

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
                int i = pixelY * canvas.width + pixelX;

                if (canvas.z[i] > vo.position.Z)
                {
                    canvas.z[i] = vo.position.Z;
                    canvas.colors[i] = shader.fragmentShader(new(vo.varying, uniforms, new Vector2(pixelX, pixelY))).color;
                }
            };

            Parallel.ForEach(voTriangles, (t) =>
            {
                if (options.FillTriangles)
                {
                    var boarders = new SortedDictionary<int, SortedDictionary<int, VertexShaderOutput>>();

                    var collectCallback = (int pixelX, int pixelY, VertexShaderOutput vo) =>
                    {
                        var xsd = boarders.GetValueOrDefault(pixelY);

                        if (xsd is null)
                        {
                            xsd = new SortedDictionary<int, VertexShaderOutput>();
                            boarders[pixelY] = xsd;
                        }

                        xsd[pixelX] = vo;
                    };

                    drawLine(t[0], t[1], collectCallback);
                    drawLine(t[1], t[2], collectCallback);
                    drawLine(t[2], t[0], collectCallback);

                    foreach ((int pixelY, var xsd) in boarders)
                    {
                        var kvpItr = xsd.GetEnumerator();

                        kvpItr.MoveNext();
                        var kvpPrev = kvpItr.Current;

                        for (int i = 1; kvpItr.MoveNext(); i++)
                        {
                            var kvpCurr = kvpItr.Current;

                            if (kvpCurr.Key - kvpPrev.Key > 1)
                            {
                                drawLine(kvpPrev.Value, kvpCurr.Value, drawCallback);
                            }
                            else if (kvpCurr.Key - kvpPrev.Key == 1)
                            {
                                drawCallback(kvpPrev.Key, pixelY, kvpPrev.Value);
                            }

                            kvpPrev = kvpItr.Current;
                        }

                        //drawLine(kvpPrev.Value, kvpCurr.Value, drawCallback);
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

            float w1dz1 = w1 / z1;
            float w2dz2 = w2 / z2;

            float d = w1dz1 + w2dz2;

            return (w1dz1 * val1 + w2dz2 * val2) / d;
        }

        private float[] Interpolate(float start, float end, float[] val1, float[] val2, float position, float z1, float z2)
        {
            if ((z1 == 0) || (z2 == 0)) return Interpolate(start, end, val1, val2, position);

            float w1 = (end - position) / (end - start);
            float w2 = 1 - w1;

            float w1dz1 = w1 / z1;
            float w2dz2 = w2 / z2;

            float d = w1dz1 + w2dz2;

            float[] result = new float[val1.Length];

            for (int i = 0; i < val1.Length; i++)
            {
                result[i] = (w1dz1 * val1[i] + w2dz2 * val2[i]) / d;
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
