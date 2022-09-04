using AKG.Rendering.Rasterisation;
using AKG.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Viewer;
using AKG.Camera;
using Rendering;

namespace AKG.Viewer.Meshes
{
    public class Teapot : Mesh
    {
        private Vector4[] _vertices = null!;

        private Renderer<Vector4, Uniforms> _renderer;

        private Primitives _primitive = Primitives.TRIANGLE_LINES;

        struct Uniforms
        {
            public VCamera camera;
        }

        private Uniforms _uniforms;

        public Teapot()
        {
            _vertices = ObjFileParser.Parse("../../../../ObjFiles/teapot.obj").BuildFlat<Vector4>();

            var shader = new ShaderProgram<Vector4, Uniforms>();

            shader.vertexShader = (vi) =>
            {
                var position = Vector4.Transform(vi.attribute, vi.uniforms.camera.VP);
                return new(position, Array.Empty<float>());
            };

            shader.fragmentShader = (fi) =>
            {
                return new(new Vector4(1.0f, 0.5f, 0.2f, 1.0f));
            };

            _renderer = new Renderer<Vector4, Uniforms>(shader);
        }

        public override void Draw(Vector4[,] colors, VCamera camera)
        {
            _uniforms.camera = camera;

            _renderer.Draw(colors, _primitive, _vertices, _uniforms);
        }
    }
}
