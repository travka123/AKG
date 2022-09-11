using AKG.Components;
using System.Data;
using System.Numerics;

public class SCamera : Positionable
{
    private static readonly Vector3 _up = new Vector3(0.0f, 1.0f, 0.0f);

    public Matrix4x4 V { get; set; } = Matrix4x4.Identity;
    private Matrix4x4 _projection = Matrix4x4.Identity;

    public Matrix4x4 VP { get; private set; } = Matrix4x4.Identity;

    public Vector3 Position { get; private set; }

    private bool _positionChanged = false;

    public Vector3 Direction { get; private set; } = new Vector3(0, 0, -1);

    private bool _directionChanged = false;

    public void SetProjection(Matrix4x4 projection)
    {
        _projection = projection;

        Update();
    }

    private void Update()
    {
        VP = V * _projection;
    }

    public static Matrix4x4 CreatePerspectiveFieldOfView(float fovy, float aspect)
    {
        const float NEAR = 0.1f;
        const float FAR = 100.0f;

        return Matrix4x4.CreatePerspectiveFieldOfView(fovy, aspect, NEAR, FAR);
    }

    public void SetPosition(Vector3 position)
    {
        Position = position;

        V = Matrix4x4.CreateLookAt(Position, Position + Direction, _up);

        Update();
    }

    public void SetDirection(Vector3 direction)
    {
        Direction = direction;

        V = Matrix4x4.CreateLookAt(Position, Position + Direction, _up);

        Update();
    }
}
