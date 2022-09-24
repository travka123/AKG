using AKG.Rendering.Rasterisation;
using AKG.Rendering.ShaderIO;
using System;
using System.Collections.Concurrent;
using System.Numerics;

namespace AKG.Rendering
{
    public class Renderer<A, U>
    {
        private ShaderProgram<A, U> _shader;

        private TriangleRasterisation<A, U> triangleRasterisationAlg = new TriangleRasterisation<A, U>();

        public Renderer(ShaderProgram<A, U> shaderProgram)
        {
            _shader = shaderProgram;
        }

        public void Draw(Canvas canvas, A[,] attributes, U uniforms, RenderingOptions options)
        {
            var voTriangles = new VertexShaderOutput[attributes.GetLength(0)][];

            var trCount = attributes.GetLength(0);

            Parallel.For(0, trCount, (i) =>
            {
                var voTriangle = new VertexShaderOutput[3];

                voTriangle[0] = WDiv(_shader.vertexShader(new(attributes[i, 0], uniforms)));
                voTriangle[1] = WDiv(_shader.vertexShader(new(attributes[i, 1], uniforms)));
                voTriangle[2] = WDiv(_shader.vertexShader(new(attributes[i, 2], uniforms)));

                voTriangles[i] = voTriangle;
            });

            var voTrianglesList = new List<VertexShaderOutput[]>();

            for (int i = 0; i < trCount; i++)
            {
                voTrianglesList.Add(voTriangles[i]);
            }

            triangleRasterisationAlg.Rasterize(canvas, voTrianglesList, _shader, uniforms, options);
        }

        private VertexShaderOutput WDiv(VertexShaderOutput vo)
        {
            vo.W = vo.position.W;
            vo.position = vo.position / vo.W;
            return vo;
        }
    }
}
