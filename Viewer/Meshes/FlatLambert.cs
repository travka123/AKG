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
    public class FlatLambert : Mesh
    {
        public static readonly List<ObjModelAttr> Layout = new() {
            ObjModelAttr.Position,
            ObjModelAttr.Normal,
            ObjModelAttr.Kd,
        };

        public static readonly ObjModelBuildConfig ModelBuildConfig = new(Layout);

        private Attributes[,] _vertices = null!;

        private Renderer<Attributes, Uniforms> _renderer;

        private struct Attributes
        {
            public Vector4 position;
            public Vector3 normals;
            public Vector3 kd;
        }

        public FlatLambert(ObjModelBuilder builder)
        {
            _vertices = builder.BuildFlatByConfig<Attributes>(ModelBuildConfig);

            var shader = new ShaderProgram<Attributes, Uniforms>();

            shader.vertexShader = (vi) =>
            {
                var positionVP = Vector4.Transform(vi.attribute.position, vi.uniforms.MVP);

                var positionM = Vector4.Transform(vi.attribute.position, vi.uniforms.M);
                var normalM = Vector4.Transform(new Vector4(vi.attribute.normals, 0), vi.uniforms.M);

                var varying = new float[11];

                positionM.CopyTo(varying, 0);
                normalM.CopyTo(varying, 4);
                vi.attribute.kd.CopyTo(varying, 8);

                return new(positionVP, varying);
            };

            shader.geometryShader = (gi) =>
            {
                var positionM = new Vector4();
                var normalM = new Vector4();
                var kd = new Vector3();

                for (int i = 0; i < gi.vo.Length; i++)
                {
                    positionM += new Vector4(new ReadOnlySpan<float>(gi.vo[i].varying, 0, 4));
                    normalM += new Vector4(new ReadOnlySpan<float>(gi.vo[i].varying, 4, 4));
                    kd += new Vector3(new ReadOnlySpan<float>(gi.vo[i].varying, 8, 3));
                }
                    
                positionM /= 3;
                normalM = Vector4.Normalize(normalM);
                kd /= 3;

                var diffuse = new Vector3();

                foreach (var light in gi.uniforms.lights)
                {
                    var lightDir = new Vector4(light.Position.X, light.Position.Y, light.Position.Z, 1) - positionM;

                    var w = Math.Max(Vector4.Dot(Vector4.Normalize(lightDir), Vector4.Normalize(normalM)), 0);

                    diffuse += w * light.ColorDiffuse * kd;
                }

                for (int i = 0; i < gi.vo.Length; i++)
                {
                    diffuse.CopyTo(gi.vo[i].varying, 0);
                }

                return new() { gi.vo };
            };

            shader.fragmentShader = (fi) =>
            {
                return new(new Vector4(fi.varying[0], fi.varying[1], fi.varying[2], 1.0f));
            };

            _renderer = new Renderer<Attributes, Uniforms>(shader);
        }

        public void Draw(Canvas canvas, Uniforms uniforms, RenderingOptions options)
        {
            _renderer.Draw(canvas, _vertices, uniforms, options);
        }

        public int GetVerticesNumber()
        {
            return _vertices.Length;
        }
    }
}
