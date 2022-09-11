using AKG.Camera;
using System.Numerics;

namespace AKG.Viewer
{
    public class Uniforms
    {
        public global::Camera camera;
        public List<LightBox> lights;
        public Matrix4x4 MVP;
        public Matrix4x4 M;

        public Uniforms(global::Camera camera, List<LightBox> lights)
        {
            this.camera = camera;
            this.lights = lights;
        }

        public Uniforms(Uniforms uniforms, Matrix4x4 M)
        {
            camera = uniforms.camera;
            lights = uniforms.lights;
            this.M = M;
            MVP = M * camera.VP;
        }
    }
}
