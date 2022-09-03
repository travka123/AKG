using AKG.Rendering.ShaderIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Rendering.Rasterisation
{
    public class TriangleLineRasterisation<A, U> : Rasterisation<A, U>
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

            ClipTriangles(voTriangles, canvasW, canvasH);

            object drawLocker = new object();

            voTriangles.AsParallel().ForAll((t) =>
            {
                var drawLine = (VertexShaderOutput a, VertexShaderOutput b) =>
                {
                    var aPosition = a.position;
                    var bPosition = b.position;
                    var aScreenPosition = ScreenCoordinates.PixelFromNDC(aPosition, canvasW, canvasW);
                    var bScreenPosition = ScreenCoordinates.PixelFromNDC(bPosition, canvasW, canvasW);

                    //DDA
                    float xl = Math.Abs(aScreenPosition.X - bScreenPosition.X);
                    float yl = Math.Abs(aScreenPosition.Y - bScreenPosition.Y);

                    float xStart = aScreenPosition.X;
                    float yStart = aScreenPosition.Y;

                    float L = xl > yl ? xl : yl;

                    float xStep = xl / L;
                    float yStep = yl / L;

                    xStep = xStart < bScreenPosition.X ? xStep : -xStep;
                    yStep = yStart < bScreenPosition.Y ? yStep : -yStep;

                    for (int i = 0; i <= L; i++)
                    {
                        float screenX = xStart + xStep * i;
                        float screenY = yStart + yStep * i;

                        var pixel = new Vector2(screenX, screenY);

                        var av2 = new Vector2(a.position.X, a.position.Y);
                        var bv2 = new Vector2(b.position.X, b.position.Y);

                        var ndc = ScreenCoordinates.NDCFromPixel(pixel, canvasW, canvasH);

                        float[] varying = Interpolate(av2, bv2, a.varying, b.varying, ndc);
                        var fo = shader.fragmentShader(new(varying, uniforms, pixel));

                        if (fo.color is not null)
                        {
                            float z = Interpolate(av2, bv2, a.position.Z, b.position.Z, pixel);
                            SetColor(canvas, zBuffer, fo.color.Value, pixel, a.position.Z, drawLocker);
                        }
                    }
                };

                drawLine(t[0], t[1]);
                drawLine(t[1], t[2]);
                drawLine(t[2], t[0]);
            });
        }

        private float Interpolate(Vector2 pos1, Vector2 pos2, float val1, float val2, Vector2 pos)
        {
            float w1 = (pos2.X - pos.X) / (pos2.X - pos1.X);
            float w2 = 1 - w1;
            return w1 * val1 + w2 * val2;
        }

        private float[] Interpolate(Vector2 pos1, Vector2 pos2, float[] val1, float[] val2, Vector2 pos)
        {
            float w1 = (pos2.X - pos.X) / (pos2.X - pos1.X);
            float w2 = 1 - w1;

            float[] result = new float[val1.Length];
            for (int i = 0; i < val1.Length; i++)
            {
                result[i] = w1 * val1[i] + w2 * val2[i];
            }

            return result;
        }

        private void ClipTriangles(List<VertexShaderOutput[]> vo, int width, int height)
        {
             
        }
    }
}
