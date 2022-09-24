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

        private Vector4[,] _vertices;

        public Vector3 Position { get; set; }
        public Vector3 Direction { get; set; } = new Vector3(0, 0, -1);
        public Vector3 ColorDiffuse { get; set; }
        public Vector3 ColorSpecular { get; set; }

        public LightBox(Vector3 position, Vector3 color)
        {
            var config = new ObjModelBuildConfig(new() { ObjModelAttr.Position });

            _vertices = ObjFileParser.Parse("..\\..\\..\\..\\ObjFiles\\zLightBox.obj").BuildFlatByConfig<Vector4>(config);

            Position = position;

            ColorDiffuse = color;
            ColorSpecular = color;

            var shaderProgram = new ShaderProgram<Vector4, Uniforms>();
            shaderProgram.vertexShader = VertexShader;
            shaderProgram.fragmentShader = FragmentShader;

            _renderer = new Renderer<Vector4, Uniforms>(shaderProgram);
        }

        public void Draw(Canvas canvas, Uniforms uniforms, RenderingOptions options)
        {
            _renderer.Draw(canvas, _vertices, new Uniforms(uniforms, Matrix4x4.CreateWorld(Position, Direction, _up)), options);
        }

        private VertexShaderOutput VertexShader(VertexShaderInput<Vector4, Uniforms> vi)
        {
            var positionM = Vector4.Transform(vi.attribute, vi.uniforms.MVP);

            var varying = new float[4];
            vi.attribute.CopyTo(varying);

            return new(positionM, varying);
        }

        private FragmentShaderOutput FragmentShader(FragmentShaderInput<Uniforms> fi)
        {
            var position = new Vector3(new ReadOnlySpan<float>(fi.varying, 0, 3));

            bool isBorder = false;
            float border = 0.4f;

            if (!(Math.Abs(position.X) == 0.5))
            {
                if (!(Math.Abs(position.Y) == 0.5))
                {
                    isBorder = (Math.Abs(position.X) > border) || (Math.Abs(position.Y) > border);
                }
                else
                {
                    isBorder = (Math.Abs(position.X) > border) || (Math.Abs(position.Z) > border);
                }
            }
            else
            {
                isBorder = (Math.Abs(position.Y) > border) || (Math.Abs(position.Z) > border);
            }

            if (isBorder)
            {
                return new(new Vector4(fi.uniforms.ambientColor.X, fi.uniforms.ambientColor.Y, fi.uniforms.ambientColor.Z, 1.0f));
            }

            if (position.Length() < 0.55)
            {
                return new(new Vector4(ColorSpecular.X, ColorSpecular.Y, ColorSpecular.Z, 1.0f));
            }

            return new(new Vector4(ColorDiffuse.X, ColorDiffuse.Y, ColorDiffuse.Z, 1.0f));
        }

        public void SetPosition(Vector3 position)
        {
            Position = position;
        }

        public void SetDirection(Vector3 direction)
        {
            Direction = direction;
        }

        public int GetVerticesNumber()
        {
            return _vertices.Length;
        }
    }
}
