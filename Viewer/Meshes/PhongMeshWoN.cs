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
    internal class PhongMeshWoN : Mesh
    {
        private Attributes[] _vertices = null!;

        private Renderer<Attributes, Uniforms> _renderer;

        private Primitives _primitive = Primitives.TRIANGLE_LINES;

        private struct Attributes
        {
            public Vector4 position;
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

        public PhongMeshWoN(string objPath, string mtlPath)
        {
            ObjModelBuilder builder = new ObjModelBuilder();

            ObjFileParser.Parse(objPath, builder);
            ObjFileParser.Parse(mtlPath, builder);

            _vertices = builder.BuildFlat<Attributes>();

            var shader = new ShaderProgram<Attributes, Uniforms>();

            shader.vertexShader = (vi) =>
            {
                var position = Vector4.Transform(vi.attribute.position, vi.uniforms.camera.VP);

                var varying = new float[3];
                varying[0] = vi.attribute.ka.X;
                varying[1] = vi.attribute.ka.Y;
                varying[2] = vi.attribute.ka.Z;

                return new(position, varying);
            };

            shader.fragmentShader = (fi) =>
            {
                return new(new Vector4(fi.varying[0], fi.varying[1], fi.varying[2], 1.0f));
            };

            _renderer = new Renderer<Attributes, Uniforms>(shader);
        }

        public override void Draw(Vector4[,] colors, float[,] zBuffer, VCamera camera)
        {
            _uniforms.camera = camera;

            _renderer.Draw(colors, zBuffer, _primitive, _vertices, _uniforms);
        }
    }
}
