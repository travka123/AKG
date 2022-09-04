using System.Data;
using System.Numerics;

namespace AKG.Camera
{
    public class VCamera
    {
        private static readonly Vector3 _up = new Vector3(0.0f, 1.0f, 0.0f);

        public Matrix4x4 V { get; set; } = Matrix4x4.Identity;
        private Matrix4x4 _projection = Matrix4x4.Identity;

        public Matrix4x4 VP { get; private set; } = Matrix4x4.Identity;

        public void SetProjection(Matrix4x4 projection)
        {
            _projection = projection;
            Update();
        }

        public void SetView(Matrix4x4 view)
        {
            V = view;
            Update();
        }

        private void Update()
        {
            VP =  V * _projection;
        }

        public static Matrix4x4 CreatePerspectiveFieldOfView(float fovy, float aspect)
        {
            const float NEAR = 0.1f;
            const float FAR = 100.0f;

            return Matrix4x4.CreatePerspectiveFieldOfView(fovy, aspect, NEAR, FAR);
        }
    }
}