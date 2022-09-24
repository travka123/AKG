using AKG.Camera;
using AKG.Rendering;
using System.Numerics;

namespace AKG.Viewer.Meshes
{
    public interface Mesh
    {
        public void Draw(Canvas canvas, Uniforms uniforms, RenderingOptions options);
        public int GetVerticesNumber();
    }
}
