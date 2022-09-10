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
using AKG.ObjReader;

namespace AKG.Viewer.Meshes
{
    public class SolidColor : Mesh
    {
        public static readonly List<ObjModelAttr> Layout = new() {
            ObjModelAttr.Position,
        };

        public static readonly ObjModelBuildConfig ModelBuildConfig = new(Layout);

        public static readonly Primitives Primitive = Primitives.TRIANGLE_LINES;

        private Vector4[] _vertices = null!;

        private Renderer<Vector4, Uniforms> _renderer;

        public SolidColor(ObjModelBuilder builder)
        {
            _vertices = builder.BuildFlatByConfig<Vector4>(ModelBuildConfig);

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

        public void Draw(Vector4[,] colors, float[,] zBuffer, Uniforms uniforms)
        {
            _renderer.Draw(colors, zBuffer, Primitive, _vertices, uniforms);
        }
    }
}
