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
        private float _rotationSpeed = 0.0018f;

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
                var nOffset = Vector2.Normalize(input.mouseOffset) * input.mouseOffset.Length();

                _yaw += _rotationSpeed * nOffset.X * input.msDelta;
                _pitch -= _rotationSpeed * nOffset.Y * input.msDelta;

                if (_pitch > (float)Math.PI /180 * 89)
                {
                    _pitch = (float)Math.PI / 180 * 89;
                }
                else if (_pitch < -(float)Math.PI / 180 * 89)
                {
                    _pitch = -(float)Math.PI / 180 * 89;
                }

                if (_yaw > (float)Math.PI)
                {
                    _yaw = -2 * (float)Math.PI + _yaw;
                } else if (Math.Abs(_yaw)  > (float)Math.PI)
                {
                    _yaw = 2 * (float)Math.PI + _yaw;
                }

                update = true;
            }

            if (input.pressedKeys.Count != 0)
            {
                Vector3 move = Vector3.Zero;

                var direction = CreateDirection();

                if (input.pressedKeys.Contains(87))
                {
                    move += direction;
                }

                if (input.pressedKeys.Contains(83))
                {
                    move -= direction;
                }

                if (input.pressedKeys.Contains(65))
                {
                    move -= Vector3.Cross(direction, _up);
                }

                if (input.pressedKeys.Contains(68))
                {
                    move += Vector3.Cross(direction, _up);
                }

                if (move.Length() > 0.1f)
                {
                    _position += Vector3.Multiply(_moveSpeed * input.msDelta, Vector3.Normalize(move));
                    update = true;
                }

                if (input.pressedKeys.Contains(32))
                {
                    move += _up;
                }

                if (input.pressedKeys.Contains(16))
                {
                    move -= _up;
                }

                if (move.Length() > 0.1f)
                {
                    _position += Vector3.Multiply(_moveSpeed * input.msDelta, Vector3.Normalize(move));
                    update = true;
                }
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
