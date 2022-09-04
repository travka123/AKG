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
    public class NCube : Mesh
    {
        private Attributes[] _vertices = null!;

        private Renderer<Attributes, Uniforms> _renderer;

        private Primitives _primitive = Primitives.TRIANGLE_LINES;

        struct Attributes
        {
            public Vector4 position;
            public Vector3 normal;
        }

        struct Uniforms
        {
            public VCamera camera;
        }

        private Uniforms _uniforms;

        public NCube()
        {
            _vertices = ObjFileParser.Parse("../../../../ObjFiles/cube.obj").BuildFlat<Attributes>();

            var shader = new ShaderProgram<Attributes, Uniforms>();

            var lightSourseColor = new Vector4(1.0f, 0.5f, 0.2f, 1.0f);

            var lightSoursePosition= new Vector4(10f, 0.5f, 10f, 1.0f);

            shader.vertexShader = (vi) =>
            {
                var PVposition = Vector4.Transform(vi.attribute.position, vi.uniforms.camera.VP);

                float[] varying = new float[3];

                varying[0] = Math.Max(vi.attribute.normal.X, 0);
                varying[1] = Math.Max(vi.attribute.normal.Y, 0);
                varying[2] = Math.Max(vi.attribute.normal.Z, 0);

                return new(PVposition, varying);
            };

            shader.fragmentShader = (fi) =>
            {
                return new(new(fi.varying[0], fi.varying[1], fi.varying[2], 1.0f));
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
