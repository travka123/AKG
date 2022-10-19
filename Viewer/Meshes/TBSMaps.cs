using AKG.ObjReader;
using AKG.Rendering;
using AKG.Rendering.Rasterisation;
using Rendering;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.DataFormats;

namespace AKG.Viewer.Meshes
{
    public class TBSMap : Mesh
    {
        public static readonly List<ObjModelAttr> Layout = new() {
            ObjModelAttr.Position,
            ObjModelAttr.TexCords,
            ObjModelAttr.Normal,
            ObjModelAttr.Ka,
            ObjModelAttr.Kd,
            ObjModelAttr.Ks,
            ObjModelAttr.Ns,
        };

        public static readonly ObjModelBuildConfig ModelBuildConfig = new(Layout, true, true);

        private struct Attributes
        {
            public Vector4 position;
            public Vector3 texCords;
            public Vector3 normal;
            public Vector3 ka;
            public Vector3 kd;
            public Vector3 ks;
            public float ns;
        }

        private class Uniforms
        {
            public Matrix4x4 MVP;
            public Matrix4x4 M;
            public Vector3 ambientColor;
            public List<LightBox> lights;
            public ObjModel<Attributes> model;
            public SCamera camera;
            public Matrix4x4 tiM;

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

        const int POSITION_OFFSET = 0;
        const int NORMAL_OFFSET = 4;
        const int KA_OFFSET = 7;
        const int KD_OFFSET = 10;
        const int KS_OFFSET = 13;
        const int NS_OFFSET = 16;
        const int TEXTURE_OFFSET = 17;

        private ObjModel<Attributes>[] _models;

        private Renderer<Attributes, Uniforms> _renderer;

        public TBSMap(ObjModelBuilder builder)
        {
            _models = builder.BuildByConfig<Attributes>(ModelBuildConfig);

            var shader = new ShaderProgram<Attributes, Uniforms>();

            shader.vertexShader = (vi) =>
            {
                var position = Vector4.Transform(vi.attribute.position, vi.uniforms.MVP);

                float[] varying = new float[20];

                vi.attribute.position.CopyTo(varying, POSITION_OFFSET);

                vi.attribute.normal.CopyTo(varying, NORMAL_OFFSET);

                vi.attribute.ka.CopyTo(varying, KA_OFFSET);

                vi.attribute.kd.CopyTo(varying, KD_OFFSET);

                vi.attribute.ks.CopyTo(varying, KS_OFFSET);

                varying[NS_OFFSET] = vi.attribute.ns;

                vi.attribute.texCords.CopyTo(varying, TEXTURE_OFFSET);

                return new(position, varying);
            };

            shader.fragmentShader = (fi) =>
            {
                var texCords = new Vector3(new ReadOnlySpan<float>(fi.varying, TEXTURE_OFFSET, 3));

                var ka = new Vector3(new ReadOnlySpan<float>(fi.varying, KA_OFFSET, 3));

                var ambient = Vector3.Multiply(fi.uniforms.ambientColor, ka);

                var textureKa = fi.uniforms.model.mapKa;

                if (textureKa is not null)
                {
                    var rgba32 = textureKa[(int)((textureKa.Width - 1) * texCords.X), (int)((textureKa.Height - 1) * (1 - texCords.Y))].ToScaledVector4();

                    ambient = Vector3.Multiply(ambient, new Vector3(rgba32.X, rgba32.Y, rgba32.Z));
                }

                var position = new Vector4(new ReadOnlySpan<float>(fi.varying, POSITION_OFFSET, 4));

                var positionM = Vector4.Transform(position, fi.uniforms.tiM);

                var bump = fi.uniforms.model.mapBump!;

                var normalM = Vector4.One;

                if (bump is not null)
                {
                    var bv = bump[(int)((bump.Width - 1) * texCords.X), (int)((bump.Height - 1) * (1 - texCords.Y))].ToScaledVector4();

                    normalM = Vector4.Transform(new Vector3(bv.X, bv.Y, bv.Z) * 2.0f - Vector3.One, fi.uniforms.tiM); 
                }

                var kd = new Vector3(new ReadOnlySpan<float>(fi.varying, KD_OFFSET, 3));

                var diffuse = Vector3.Zero;

                var lightDirs = new Vector4[fi.uniforms.lights.Count];

                for (int i = 0; i < lightDirs.Length; i++)
                {
                    var light = fi.uniforms.lights[i];

                    lightDirs[i] = Vector4.Normalize(new Vector4(light.Position.X, light.Position.Y, light.Position.Z, 1) - positionM);

                    var w = Math.Max(Vector4.Dot(lightDirs[i], Vector4.Normalize(normalM)), 0);

                    if (w > 0)
                        diffuse += kd * w * light.ColorDiffuse;
                }

                var textureKd = fi.uniforms.model.mapKd;

                if (textureKd is not null)
                {
                    var rgba32 = textureKd[(int)((textureKd.Width - 1) * texCords.X), (int)((textureKd.Height - 1) * (1 - texCords.Y))].ToScaledVector4();

                    diffuse = Vector3.Multiply(diffuse, new Vector3(rgba32.X, rgba32.Y, rgba32.Z));
                }

                var ks = new Vector3(new ReadOnlySpan<float>(fi.varying, KS_OFFSET, 3));
                var ns = fi.varying[NS_OFFSET];

                var textureKs = fi.uniforms.model.mapKs;

                if (textureKs is not null)
                {
                    var rgba32 = textureKs[(int)((textureKs.Width - 1) * texCords.X), (int)((textureKs.Height - 1) * (1 - texCords.Y))].ToScaledVector4();

                    ks = Vector3.Multiply(ks, new Vector3(rgba32.X, rgba32.Y, rgba32.Z));
                }

                var cameraPosition = fi.uniforms.camera.Position;

                var viewDir = Vector3.Normalize(cameraPosition - new Vector3(positionM.X, positionM.Y, positionM.Z));

                var specular = Vector3.Zero;

                for (int i = 0; i < lightDirs.Length; i++)
                {
                    var reflectDir = Vector3.Reflect(new Vector3(-lightDirs[i].X, -lightDirs[i].Y, -lightDirs[i].Z),
                        new Vector3(-normalM.X, -normalM.Y, -normalM.Z));

                    var w = Math.Max(Vector3.Dot(viewDir, reflectDir), 0);

                    if (w > 0)
                        specular += ks * (float)Math.Pow(w, ns) * fi.uniforms.lights[i].ColorSpecular;
                }

                var color = ambient + diffuse + specular;

                return new(new(Math.Clamp(color.X, 0, 1), Math.Clamp(color.Y, 0, 1), Math.Clamp(color.Z, 0, 1), 1.0f));
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


