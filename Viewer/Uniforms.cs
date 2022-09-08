using AKG.Camera;

namespace AKG.Viewer
{
    public class Uniforms
    {
        public VCamera camera;
        public List<Light> lights;

        public Uniforms(VCamera camera, List<Light> lights)
        {
            this.camera = camera;
            this.lights = lights;
        }
    }
}
