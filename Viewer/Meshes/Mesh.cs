using AKG.Camera;
using System.Numerics;

namespace AKG.Viewer.Meshes
{
    public abstract class Mesh
    {
        public abstract void Draw(Vector4[,] colors, VCamera camera);
    }
}
