using AKG.Components;
using AKG.Rendering;
using AKG.Viewer.Meshes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Viewer
{
    public class Entity : Mesh, Positionable
    {
        private static readonly Vector3 _up = new Vector3(0.0f, 1.0f, 0.0f);

        public Vector3 Position { get; set; }

        public Vector3 Direction { get; set; } = new Vector3(0, 0, -1);

        private Mesh? _mesh;

        public void Draw(Vector4[,] colors, float[,] zBuffer, Uniforms uniforms, RenderingOptions options)
        {
            if (_mesh is not null)
            _mesh.Draw(colors, zBuffer, new Uniforms(uniforms, Matrix4x4.CreateWorld(Position, Direction, _up)), options);
        }

        public void SetMesh(Mesh mesh)
        {
            _mesh = mesh;
        }

        public void SetDirection(Vector3 direction)
        {
            Direction = direction;
        }

        public void SetPosition(Vector3 position)
        {
            Position = position;
        }

        public int GetVerticesNumber()
        {
            return _mesh is not null ? _mesh.GetVerticesNumber() : 0;
        }
    }
}
