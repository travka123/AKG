using AKG.ObjReader;
using AKG.Rendering.Rasterisation;
using AKG.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Rendering;
using System.Diagnostics;

namespace AKG.Viewer.Meshes
{
    public class Phong : Mesh
    {
        public static readonly List<ObjModelAttr> Layout = new() {
            ObjModelAttr.Position,
            ObjModelAttr.Normal,
            ObjModelAttr.Ka,
            ObjModelAttr.Kd,
            ObjModelAttr.Ks,
            ObjModelAttr.Ns,
        };

        public static readonly ObjModelBuildConfig ModelBuildConfig = new(Layout);

        public static readonly Primitives Primitive = Primitives.TRIANGLE_LINES;

        private struct Attributes
        {
            public Vector4 position;
            public Vector3 normal;
            public Vector3 ka;
            public Vector3 kd;
            public Vector3 ks;
            public float ns;
        }

        const int POSITION_OFFSET = 0;
        const int NORMAL_OFFSET = 4;
        const int KA_OFFSET = 7;
        const int KD_OFFSET = 10;
        const int KS_OFFSET = 13;
        const int NS_OFFSET = 16;

        private Attributes[] _vertices;

        private Renderer<Attributes, Uniforms> _renderer;

        public Phong(ObjModelBuilder builder)
        {
            _vertices = builder.BuildFlatByConfig<Attributes>(ModelBuildConfig);

            var shader = new ShaderProgram<Attributes, Uniforms>();

            shader.vertexShader = (vi) =>
            {
                var position = Vector4.Transform(vi.attribute.position, vi.uniforms.MVP);

                float[] varying = new float[17];

                vi.attribute.position.CopyTo(varying, POSITION_OFFSET);

                vi.attribute.normal.CopyTo(varying, NORMAL_OFFSET);

                vi.attribute.ka.CopyTo(varying, KA_OFFSET);

                vi.attribute.kd.CopyTo(varying, KD_OFFSET);

                vi.attribute.ks.CopyTo(varying, KS_OFFSET);

                varying[NS_OFFSET] = vi.attribute.ns;

                return new(position, varying);
            };

            shader.fragmentShader = (fi) =>
            {
                var ka = new Vector3(new ReadOnlySpan<float>(fi.varying, KA_OFFSET, 3));

                var ambient = Vector3.Multiply(fi.uniforms.ambientColor, ka);

                var position = new Vector4(new ReadOnlySpan<float>(fi.varying, POSITION_OFFSET, 4));

                var positionM = Vector4.Transform(position, fi.uniforms.M);

                var normal = new Vector3(new ReadOnlySpan<float>(fi.varying, NORMAL_OFFSET, 3));

                var normalM = Vector4.Transform(new Vector4(normal, 0), fi.uniforms.M);

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

                var ks = new Vector3(new ReadOnlySpan<float>(fi.varying, KS_OFFSET, 3));
                var ns = fi.varying[NS_OFFSET];

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

        public void Draw(Vector4[,] colors, float[,] zBuffer, Uniforms uniforms)
        {
            _renderer.Draw(colors, zBuffer, Primitive, _vertices, uniforms);
        }
    }
}
