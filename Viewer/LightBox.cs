using AKG.Components;
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
    public class LightBox : Mesh, Light, Positionable
    {
        private static readonly Vector3 _up = new Vector3(0.0f, 1.0f, 0.0f);

        private Renderer<Vector4, Uniforms> _renderer;

        private Vector4[] _vertices;

        public static readonly Primitives Primitive = Primitives.TRIANGLE_LINES;

        public Vector3 Position { get; set; }
        public Vector3 Direction { get; set; } = new Vector3(0, 0, -1);
        public Vector3 Color { get; set; } 

        public LightBox(Vector3 position, Vector3 color)
        {
            var config = new ObjModelBuildConfig(new() { ObjModelAttr.Position });

            _vertices = ObjFileParser.Parse("..\\..\\..\\..\\ObjFiles\\zLightBox.obj").BuildFlatByConfig<Vector4>(config);

            Position = position;
            Color = color;

            var shaderProgram = new ShaderProgram<Vector4, Uniforms>();
            shaderProgram.vertexShader = VertexShader;
            shaderProgram.fragmentShader = FragmentShader;

            _renderer = new Renderer<Vector4, Uniforms>(shaderProgram);
        }

        public void Draw(Vector4[,] colors, float[,] zBuffer, Uniforms uniforms)
        {
            _renderer.Draw(colors, zBuffer, Primitive, _vertices,
                new Uniforms(uniforms, Matrix4x4.CreateWorld(Position, Direction, _up)));
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

        public void SetPosition(Vector3 position)
        {
            Position = position;
        }

        public void SetDirection(Vector3 direction)
        {
            Direction = direction;
        }
    }
}
