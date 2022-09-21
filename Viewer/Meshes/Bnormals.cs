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
    public class Bnormals : Mesh
    {
        public static readonly List<ObjModelAttr> Layout = new() {
            ObjModelAttr.Position,
            ObjModelAttr.Normal,
            ObjModelAttr.TexCords,
        };

        public static readonly ObjModelBuildConfig ModelBuildConfig = new(Layout, bumpMap: true);

        public static readonly Primitives Primitive = Primitives.TRIANGLE_LINES;

        private struct Attributes
        {
            public Vector4 position;
            public Vector3 normal;
            public Vector3 texCords;
        }

        private class Uniforms
        {
            public Matrix4x4 MVP;
            public Matrix4x4 M;
            public Vector3 ambientColor;
            public List<LightBox> lights;
            public ObjModel<Attributes> model;
            public SCamera camera;

            public Uniforms(Matrix4x4 mVP, Matrix4x4 m, Vector3 ambientColor,
                List<LightBox> lights, ObjModel<Attributes> model, SCamera camera)
            {
                MVP = mVP;
                M = m;
                this.ambientColor = ambientColor;
                this.lights = lights;
                this.model = model;
                this.camera = camera;
            }
        }

        private Attributes[] _vertices = null!;

        private Renderer<Attributes, Uniforms> _renderer;

        private ObjModel<Attributes>[] _models;

        public Bnormals(ObjModelBuilder builder)
        {
            _models = builder.BuildByConfig<Attributes>(ModelBuildConfig);

            var shader = new ShaderProgram<Attributes, Uniforms>();

            shader.vertexShader = (vi) =>
            {
                var position = Vector4.Transform(vi.attribute.position, vi.uniforms.MVP);

                float[] varying = new float[6];

                varying[0] = Math.Max(vi.attribute.normal.X, 0);
                varying[1] = Math.Max(vi.attribute.normal.Y, 0);
                varying[2] = Math.Max(vi.attribute.normal.Z, 0);

                vi.attribute.texCords.CopyTo(varying, 3);

                return new(position, varying);
            };

            shader.fragmentShader = (fi) =>
            {
                var texCords = new Vector3(new ReadOnlySpan<float>(fi.varying, 3, 3));

                var normal = new Vector3(new ReadOnlySpan<float>(fi.varying, 0, 3));

                var bump = fi.uniforms.model.mapBump!;

                if (bump is not null)
                {
                    var bv = bump[(int)((bump.Width - 1) * texCords.X), (int)((bump.Height - 1) * (1 - texCords.Y))].ToScaledVector4();

                    normal = Vector3.Multiply(normal, new Vector3(bv.X, bv.Y, bv.Z));
                }

                var normalM = Vector4.Transform(new Vector4(normal, 0), fi.uniforms.M);

                return new(new(normalM.X, normalM.Y, normalM.Z, 1.0f));
            };

            _renderer = new Renderer<Attributes, Uniforms>(shader);
        }

        public void Draw(Vector4[,] colors, float[,] zBuffer, Viewer.Uniforms uniforms, RenderingOptions options)
        {
            foreach (var model in _models)
            {
                _renderer.Draw(colors, zBuffer, Primitive, model.attributes, new Uniforms(uniforms.MVP, uniforms.M,
                    uniforms.ambientColor, uniforms.lights, model, uniforms.camera), options);
            }
        }
    }
}
