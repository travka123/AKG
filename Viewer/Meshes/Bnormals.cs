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
            ObjModelAttr.Normal,
            ObjModelAttr.TexCords,
        };

        public static readonly ObjModelBuildConfig ModelBuildConfig = new(Layout, bumpMap: true);

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
            public Matrix4x4 tiM;
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

                tiM = new Matrix4x4();
                Matrix4x4.Invert(M, out tiM);
                tiM = Matrix4x4.Transpose(tiM);
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

                float[] varying = new float[4 + 3 + 3];

                var positionM = Vector4.Transform(vi.attribute.position, vi.uniforms.M);
                var normalM = Vector3.Transform(vi.attribute.normal, vi.uniforms.tiM);

                positionM.CopyTo(varying, 0);
                vi.attribute.texCords.CopyTo(varying, 4);
                normalM.CopyTo(varying, 7);

                return new(positionMVP, varying);
            };

            shader.fragmentShader = (fi) =>
            {
                var span = new ReadOnlySpan<float>(fi.varying);

                var position = new Vector4(span.Slice(0, 4));
                var texCords = new Vector3(span.Slice(4, 3));
                var normal = new Vector3(span.Slice(7, 3));

                normal = Vector3.Transform(GetFromTexture(fi.uniforms.model.mapBump, texCords) * 2.0f - Vector3.One, fi.uniforms.tiM);

                return new(new(Math.Clamp(normal.X, 0, 1), Math.Clamp(normal.Y, 0, 1), Math.Clamp(normal.Z, 0, 1), 1.0f));
            };

            _renderer = new Renderer<Attributes, Uniforms>(shader);
        }

        private static Vector3 GetFromTexture(Image<Rgba32>? image, Vector3 texCords)
        {
            if (image is null)
                return Vector3.One;

            int x = (int)((image.Width - 1) * texCords.X);
            int y = (int)((image.Height - 1) * (1 - texCords.Y));

            var rgba32 = image[x, y].ToScaledVector4();

            return new Vector3(rgba32.X, rgba32.Y, rgba32.Z);
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
