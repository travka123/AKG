using AKG.Camera;
using AKG.Rendering;
using System.Numerics;

namespace AKG.Viewer.Meshes
{
    public interface Mesh
    {
        public void Draw(Vector4[,] colors, float[,] zBuffer, Uniforms uniforms, RenderingOptions options);
    }
}
