using AKG.Rendering.ShaderIO;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Rendering.Rasterisation
{
    public class PointRasterisation<A, U> : Rasterisation<A, U>
    {
        public override void Rasterize(Vector4[,] canvas, float[,] zBuffer, VertexShaderOutput[] vo, ShaderProgram<A, U> shader, U uniforms, RenderingOptions options)
        {
            int canvasH = canvas.GetLength(0);
            int canvasW = canvas.GetLength(1);

            object drawLocker = new object();

            vo.AsParallel().ForAll((vo) =>
            {
                var sp = ScreenCoordinates.PixelFromNDC(vo.position, canvasW, canvasH);

                var fo = shader.fragmentShader(new FragmentShaderInput<U>(vo.varying, uniforms, sp));

                if (fo.color is not null)
                {
                    lock (drawLocker)
                    {
                        var rz = float.MaxValue - vo.position.Z;

                        if (zBuffer[(int)sp.Y, (int)sp.X] < rz)
                        {
                            zBuffer[(int)sp.Y, (int)sp.X] = rz;
                            canvas[(int)sp.Y, (int)sp.X] = fo.color.Value;
                        }
                    }
                }
            });
        }
    }
}
