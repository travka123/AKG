using AKG.Camera;
using System.Numerics;

namespace AKG.Viewer
{
    public class Uniforms
    {
        public VCamera camera;
        public List<LightBox> lights;
        public Matrix4x4 MVP;

        public Uniforms(VCamera camera, List<LightBox> lights)
        {
            this.camera = camera;
            this.lights = lights;
            this.MVP = camera.VP;
        }

        public Uniforms(Uniforms uniforms, Matrix4x4 MVP)
        {
            camera = uniforms.camera;
            lights = uniforms.lights;
            this.MVP = MVP;
        }
    }
}
