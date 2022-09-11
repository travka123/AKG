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
    public class Lambert : Mesh
    {
        public static readonly List<ObjModelAttr> Layout = new() {
            ObjModelAttr.Position,
            ObjModelAttr.Normal,
            ObjModelAttr.Kd,
        };

        public static readonly ObjModelBuildConfig ModelBuildConfig = new(Layout);

        public static readonly Primitives Primitive = Primitives.TRIANGLE_LINES;

        private Attributes[] _vertices = null!;

        private Renderer<Attributes, Uniforms> _renderer;

        private struct Attributes
        {
            public Vector4 position;
            public Vector3 normals;
            public Vector3 kd;
        }

        public Lambert(ObjModelBuilder builder)
        {
            _vertices = builder.BuildFlatByConfig<Attributes>(ModelBuildConfig);

            var shader = new ShaderProgram<Attributes, Uniforms>();

            shader.vertexShader = (vi) =>
            {
                var positionVP = Vector4.Transform(vi.attribute.position, vi.uniforms.MVP);

                var positionM = Vector4.Transform(vi.attribute.position, vi.uniforms.M);
                var normalsR = Vector4.Transform(new Vector4(vi.attribute.normals, 0), vi.uniforms.M);

                var varying = new float[3];

                foreach (var light in vi.uniforms.lights)
                {
                    var lightDir = new Vector4(light.Position.X, light.Position.Y, light.Position.Z, 1) - positionM;

                    var w = Math.Max(Vector4.Dot(Vector4.Normalize(lightDir), Vector4.Normalize(normalsR)), 0);

                    varying[0] += w * light.Color.X * vi.attribute.kd.X;
                    varying[1] += w * light.Color.Y * vi.attribute.kd.Y;
                    varying[2] += w * light.Color.Z * vi.attribute.kd.Z;
                }

                for (int i = 0; i < 3; i++)
                    varying[i] = Math.Min(varying[i], 1);

                return new(positionVP, varying);
            };

            shader.fragmentShader = (fi) =>
            {
                return new(new Vector4(fi.varying[0], fi.varying[1], fi.varying[2], 1.0f));
            };

            _renderer = new Renderer<Attributes, Uniforms>(shader);
        }

        public void Draw(Vector4[,] colors, float[,] zBuffer, Uniforms uniforms)
        {
            _renderer.Draw(colors, zBuffer, Primitive, _vertices, uniforms);
        }
    }
}
