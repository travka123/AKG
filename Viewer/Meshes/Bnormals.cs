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
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace AKG.Viewer.Meshes
{
    public class Bnormals : Mesh
    {
        public static readonly List<ObjModelAttr> Layout = new() {
            ObjModelAttr.Position,
            ObjModelAttr.TexCords,
            ObjModelAttr.Normal,
            ObjModelAttr.TanBitan,
        };

        public static readonly ObjModelBuildConfig ModelBuildConfig = new(Layout, bumpMap: true);

        private struct Attributes
        {
            public Vector4 position;
            public Vector3 texCords;
            public Vector3 normal;
            public Vector3 tangent;
            public Vector3 bitangent;
        }

        private class Uniforms
        {
            public Matrix4x4 MVP;
            public Matrix4x4 M;
            public Matrix4x4 TIM;
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
                TIM = ShaderHelper.TransposeInverseMatrix(M);
            }
        }

        private Renderer<Attributes, Uniforms> _renderer;

        private ObjModel<Attributes>[] _models;

        public Bnormals(ObjModelBuilder builder)
        {
            _models = builder.BuildByConfig<Attributes>(ModelBuildConfig);

            var shader = new ShaderProgram<Attributes, Uniforms>();

            shader.vertexShader = (vi) =>
            {
                var positionMVP = Vector4.Transform(vi.attribute.position, vi.uniforms.MVP);

                float[] varying = new float[4 + 3 + 3 + 3 + 6];

                var positionM = Vector4.Transform(vi.attribute.position, vi.uniforms.M);

                positionM.CopyTo(varying, 0);
                vi.attribute.texCords.CopyTo(varying, 4);
                vi.attribute.normal.CopyTo(varying, 7);
                vi.attribute.tangent.CopyTo(varying, 10);
                vi.attribute.bitangent.CopyTo(varying, 13);

                return new(positionMVP, varying);
            };

            shader.fragmentShader = (fi) =>
            {           
                var span = new ReadOnlySpan<float>(fi.varying);

                var position = new Vector4(span.Slice(0, 4));
                var texCords = new Vector3(span.Slice(4, 3));

                var normal = ShaderHelper.NormalFromTexture(fi.uniforms.model.mapBump, texCords, fi.uniforms.TIM, span[7..]);

                return new(new (Math.Clamp(normal.X, 0, 1), Math.Clamp(normal.Y, 0, 1), Math.Clamp(normal.Z, 0, 1), 1.0f));
            };

            _renderer = new Renderer<Attributes, Uniforms>(shader);
        }

        public void Draw(Canvas canvas, Viewer.Uniforms uniforms, RenderingOptions options)
        {
            foreach (var model in _models)
            {
                _renderer.Draw(canvas, model.attributes, new Uniforms(uniforms.MVP, uniforms.M,
                    uniforms.ambientColor, uniforms.lights, model, uniforms.camera), options);
            }
        }

        public int GetVerticesNumber()
        {
            return _models.Sum(m => m.attributes.Length);
        }
    }
}
