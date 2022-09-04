using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Camera.Controls
{
    public class FlyingCameraControls : CameraControl
    {
        private static readonly Vector3 _up = new Vector3(0, 1, 0);

        private float _moveSpeed = 0.01f;
        private float _rotationSpeed = 0.001f;

        private VCamera _camera;

        private float _yaw = -(float)Math.PI / 180 * 90;
        private float _pitch = 0;

        private Vector3 _position;

        public FlyingCameraControls(VCamera camera, Vector3 position)
        {
            _position = position;
            _camera = camera;
            Update();
        }

        public override bool Process(Input input)
        {
            bool update = false;

            if (input.mouseBtn1Pressed && (input.mouseOffset != Vector2.Zero))
            {
                _yaw += _rotationSpeed * input.mouseOffset.X * input.msDelta;
                _pitch -= _rotationSpeed * input.mouseOffset.Y * input.msDelta;

                update = true;
            }

            if (input.pressedKeys.Count != 0)
            {
                Vector3 move = Vector3.Zero;

                var direction = CreateDirection();

                float sYaw = (float)Math.Sin(_yaw);
                float cYaw = (float)Math.Cos(_yaw);

                if (input.pressedKeys.Contains(87))
                {
                    move += direction;
                    update = true;
                }

                if (input.pressedKeys.Contains(83))
                {
                    move -= direction;
                    update = true;
                }

                if (input.pressedKeys.Contains(65))
                {
                    move -= Vector3.Cross(direction, _up);
                    update = true;
                }

                if (input.pressedKeys.Contains(68))
                {
                    move += Vector3.Cross(direction, _up);
                    update = true;
                }

                update = true;

                move += Vector3.Multiply(_moveSpeed * input.msDelta, Vector3.Normalize(move));

                _position += move;
            }

            if (update)
            {
                Update();
            }

            return update;
        }

        private Vector3 CreateDirection()
        {
            return Vector3.Normalize(new Vector3((float)(Math.Cos(_yaw) * Math.Cos(_pitch)), (float)Math.Sin(_pitch),
                (float)(Math.Sin(_yaw) * Math.Cos(_pitch))));
        }

        private void Update()
        {
            _camera.SetView(Matrix4x4.CreateLookAt(_position, _position + CreateDirection(), _up));
        }
    }
}
