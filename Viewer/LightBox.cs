using AKG.ObjReader;
using AKG.Rendering;
using AKG.Rendering.Rasterisation;
using AKG.Rendering.ShaderIO;
using AKG.Viewer.Meshes;
using Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Viewer
{
    public class LightBox : Mesh, Light
    {
        private Renderer<Vector4, Uniforms> _renderer;

        private Vector4[] _vertices;

        public static readonly Primitives Primitive = Primitives.TRIANGLE_LINES;

        private Matrix4x4 _m;

        public LightBox(Vector3 position, Vector3 color)
        {
            var config = new ObjModelBuildConfig(new() { ObjModelAttr.Position });

            _vertices = ObjFileParser.Parse("..\\..\\..\\..\\ObjFiles\\cube.obj").BuildFlatByConfig<Vector4>(config);

            Position = position;
            Color = color;

            var shaderProgram = new ShaderProgram<Vector4, Uniforms>();
            shaderProgram.vertexShader = VertexShader;
            shaderProgram.fragmentShader = FragmentShader;

            _renderer = new Renderer<Vector4, Uniforms>(shaderProgram);

            _m = Matrix4x4.CreateTranslation(Position);
        }

        public Vector3 Position { get; set; }
        public Vector3 Color { get; set; }

        public void Draw(Vector4[,] colors, float[,] zBuffer, Uniforms uniforms)
        {
            _renderer.Draw(colors, zBuffer, Primitive, _vertices, new Uniforms(uniforms,
                Matrix4x4.CreateScale(0.1f) * Matrix4x4.CreateTranslation(Position) * uniforms.camera.VP));
        }

        private VertexShaderOutput VertexShader(VertexShaderInput<Vector4, Uniforms> vi)
        {
            var position = Vector4.Transform(vi.attribute, vi.uniforms.MVP);
            return new(position, Array.Empty<float>());
        }

        private FragmentShaderOutput FragmentShader(FragmentShaderInput<Uniforms> fi)
        {
            return new(new Vector4(Color.X, Color.Y, Color.Z, 1.0f));
        }
    }
}
