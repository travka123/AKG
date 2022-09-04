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

namespace AKG.Viewer.Meshes
{
    public class PhongMeshWt : Mesh
    {
        private Attributes[] _vertices = null!;

        private Renderer<Attributes, Uniforms> _renderer;

        private Primitives _primitive = Primitives.TRIANGLE_LINES;

        private struct Attributes
        {
            public Vector4 position;
            public Vector3 normals;
            public Vector3 textures;
            public Vector3 ka;
            public Vector3 kd;
            public Vector3 ks;
            public float illum;
            public float ns;
        }

        private struct Uniforms
        {
            public VCamera camera;
        }

        private Uniforms _uniforms;

        public PhongMeshWt(string objPath, string mtlPath)
        {
            ObjModelBuilder builder = new ObjModelBuilder();

            ObjFileParser.Parse(objPath, builder);
            ObjFileParser.Parse(mtlPath, builder);

            _vertices = builder.BuildFlat<Attributes>();

            var light = new Vector3(15, 15, 0);
            var lightColor = new Vector3(0.1f, 0.1f, 0.1f);

            var shader = new ShaderProgram<Attributes, Uniforms>();

            shader.vertexShader = (vi) =>
            {
                var position = Vector4.Transform(vi.attribute.position, vi.uniforms.camera.VP);

                var varying = new float[9];

                varying[0] = vi.attribute.position.X;
                varying[1] = vi.attribute.position.Y;
                varying[2] = vi.attribute.position.Z;

                varying[3] = vi.attribute.normals.X;
                varying[4] = vi.attribute.normals.Y;
                varying[5] = vi.attribute.normals.Z;

                varying[6] = vi.attribute.kd.X;
                varying[7] = vi.attribute.kd.Y;
                varying[8] = vi.attribute.kd.Z;

                return new(position, varying);
            };

            shader.fragmentShader = (fi) =>
            {
                var lightDir = Vector3.Normalize(light - new Vector3(fi.varying[0], fi.varying[1], fi.varying[2]));

                var rl = Vector3.Dot(lightDir, new Vector3(fi.varying[3], fi.varying[4], fi.varying[5]));

                rl = Math.Max(rl, 0);

                return new(new Vector4(fi.varying[6] * rl, fi.varying[7] * rl, fi.varying[8] * rl, 1.0f));
            };

            _renderer = new Renderer<Attributes, Uniforms>(shader);
        }

        public override void Draw(Vector4[,] colors, VCamera camera)
        {
            _uniforms.camera = camera;

            _renderer.Draw(colors, _primitive, _vertices, _uniforms);
        }
    }
}
