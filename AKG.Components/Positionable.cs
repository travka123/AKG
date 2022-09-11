using System.Numerics;

namespace AKG.Components
{
    public interface Positionable
    {
        public Vector3 Position { get; }
        public Vector3 Direction { get; }

        public void SetPosition(Vector3 position);
        public void SetDirection(Vector3 direction);
    }
}