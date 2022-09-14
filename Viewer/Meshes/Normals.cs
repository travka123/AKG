using AKG.Camera;
using AKG.Rendering.Rasterisation;
using AKG.Rendering;
using Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using AKG.ObjReader;

namespace AKG.Viewer.Meshes
{
    public class Normals : Mesh
    {
        public static readonly List<ObjModelAttr> Layout = new() {
            ObjModelAttr.Position,
            ObjModelAttr.Normal,
        };

        public static readonly ObjModelBuildConfig ModelBuildConfig = new(Layout);

        public static readonly Primitives Primitive = Primitives.TRIANGLE_LINES;

        private struct Attributes
        {
            public Vector4 position;
            public Vector3 normal;
        }

        private Attributes[] _vertices = null!;

        private Renderer<Attributes, Uniforms> _renderer;

        public Normals(ObjModelBuilder builder)
        {
            _vertices = builder.BuildFlatByConfig<Attributes>(ModelBuildConfig);

            var shader = new ShaderProgram<Attributes, Uniforms>();

            shader.vertexShader = (vi) =>
            {
                var position = Vector4.Transform(vi.attribute.position, vi.uniforms.MVP);

                float[] varying = new float[3];

                varying[0] = Math.Max(vi.attribute.normal.X, 0);
                varying[1] = Math.Max(vi.attribute.normal.Y, 0);
                varying[2] = Math.Max(vi.attribute.normal.Z, 0);

                return new(position, varying);
            };

            shader.fragmentShader = (fi) =>
            {
                return new(new(fi.varying[0], fi.varying[1], fi.varying[2], 1.0f));
            };

            _renderer = new Renderer<Attributes, Uniforms>(shader);
        }

        public void Draw(Vector4[,] colors, float[,] zBuffer, Uniforms uniforms, RenderingOptions options)
        {
            _renderer.Draw(colors, zBuffer, Primitive, _vertices, uniforms, options);
        }
    }
}
