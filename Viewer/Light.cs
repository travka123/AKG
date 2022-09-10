using System.Numerics;

namespace AKG.Viewer
{
    public interface Light
    {
        public Vector3 Position { get; set; }
        public Vector3 Color { get; set; }
    }
}
