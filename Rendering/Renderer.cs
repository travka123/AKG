using AKG.Rendering.Rasterisation;
using System;
using System.Numerics;

namespace AKG.Rendering
{
    public class Renderer<A, U>
    {
        private ShaderProgram<A, U> _shader;

        private static Dictionary<Primitives, Rasterisation<A, U>> rasterizations = new()
        {
             { Primitives.POINTS, new PointRasterisation<A, U>() },
             { Primitives.TRIANGLE_LINES, new TriangleRasterisation<A, U>() },
        };

        public Renderer(ShaderProgram<A, U> shaderProgram)
        {
            _shader = shaderProgram;
        }

        public void Draw(Vector4[,] canvas, float[,] zBuffer, Primitives primitive, A[] attributes, U uniforms, RenderingOptions options)
        {
            var vo = attributes.AsParallel().Select((a) => _shader.vertexShader(new(a, uniforms))).ToArray();

            for (int i = 0; i < vo.Length; i++)
            {
                var position = vo[i].position;
                vo[i].position = new Vector4(position.X / position.W, position.Y / position.W, position.Z / position.W, position.W);
            }

            rasterizations[primitive].Rasterize(canvas, zBuffer, vo, _shader, uniforms, options);
        }
    }
}
