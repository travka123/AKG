using AKG.Camera;
using AKG.Camera.Controls;
using AKG.Rendering;
using AKG.Rendering.Rasterisation;
using AKG.Viewer;
using AKG.Viewer.Meshes;
using Microsoft.VisualBasic.Devices;
using Rendering;
using System.Collections;
using System.Configuration;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Viewer
{
    public partial class FormMain : Form
    {
        private bool _stretch = false;

        Bitmap _bmp;

        private Input _input;

        private VCamera _camera;

        private CameraControl _cameraControl;

        private Mesh _mesh;

        private Dictionary<string, Func<Mesh>> _meshes;

        public FormMain()
        {
            InitializeComponent();

            _bmp = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppPArgb);

            _input = new();

            _camera = new VCamera();

            _camera.SetProjection(Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 180 * 70, Width / Height, 0.1f, 1000f));

            _cameraControl = new FlyingCameraControls(_camera, new Vector3(5, 0, 30));

            _mesh = new Teapot();

            _meshes = new Dictionary<string, Func<Mesh>>()
            {
                { "teapot", () => new Teapot() },
                { "NCube", () => new NCube() },
                { "shuttle", () => new PhongMeshWoN("../../../../ObjFiles/shuttle.obj", "../../../../ObjFiles/vp.mtl") },
                { "tree", () => new PhongMeshWt("../../../../ObjFiles/Lowpoly_tree_sample.obj", "../../../../ObjFiles/Lowpoly_tree_sample.mtl") },
            };

            cbMeshes.DataSource = _meshes.Keys.ToList();

            Invalidate();
        }

        private volatile bool _bmpOutdated = true;

        private readonly object _inputLock = new object();

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Task.Run(() =>
            {
                Vector4[,] v4Colors = new Vector4[Height, Width];
                float[,] zBuffer = new float[Height, Width];

                while (true)
                {
                    if (_bmpOutdated)
                    {
                        _bmpOutdated = false;

                        Clear(v4Colors);
                        ClearZ(zBuffer);

                        _mesh.Draw(v4Colors, zBuffer, _camera);

                        var colors = GetBMPColors(v4Colors);

                        lock (_bmp)
                        {
                            var bitmapInfo = _bmp.LockBits(new Rectangle(0, 0, _bmp.Width, _bmp.Height), ImageLockMode.ReadWrite, _bmp.PixelFormat);

                            Marshal.Copy(colors, 0, bitmapInfo.Scan0, colors.Length);

                            _bmp.UnlockBits(bitmapInfo);
                        }

                        Invalidate();
                    }

                    lock (_inputLock)
                    {
                        var now = DateTime.Now;
                        _input.msDelta = (now - _input.time).Milliseconds;
                        _input.time = now;

                        _bmpOutdated |= _cameraControl.Process(_input);

                        _input.mouseOffset = Vector2.Zero;
                    }
                }
            });
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            lock (_inputLock)
            {
                _input.pressedKeys.Add(e.KeyValue);
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            lock (_inputLock)
            {
                _input.pressedKeys.Remove(e.KeyValue);
            }

        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                lock (_inputLock)
                {
                    _input.mouseBtn1Pressed = true;
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                lock (_inputLock)
                {
                    _input.mouseBtn1Pressed = false;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var location = new Vector2(e.X, e.Y);

            lock (_inputLock)
            {
                _input.mouseOffset = location - _input.mousePosition;
                _input.mousePosition = location;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            lock (_bmp)
            {
                if (_stretch)
                {
                    int fHeight = this.Height;
                    int fWidth = this.Width;
                    e.Graphics.DrawImage(_bmp, 0, 0, fWidth, fHeight);
                }
                else
                {
                    e.Graphics.DrawImage(_bmp, 0, 0);
                }
            }
        }

        private byte[] GetBMPColors(Vector4[,] colors)
        {
            int height = colors.GetLength(0);
            int width = colors.GetLength(1);

            byte[] bytes = new byte[colors.Length * 4];

            int offset = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var byteColor = Vector4.Multiply(colors[y, x], 255);

                    bytes[offset + 0] = (byte)byteColor.Z;
                    bytes[offset + 1] = (byte)byteColor.Y;
                    bytes[offset + 2] = (byte)byteColor.X;
                    bytes[offset + 3] = (byte)byteColor.W;

                    offset += 4;
                }
            }

            return bytes;
        }

        private Vector4 _clearColor = new Vector4(0.2f, 0.3f, 0.3f, 1.0f);

        private void Clear(Vector4[,] colors)
        {
            int height = colors.GetLength(0);
            int width = colors.GetLength(1);

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    colors[i, j] = _clearColor;
                }
            }
        }

        private void ClearZ(float[,] colors)
        {
            int height = colors.GetLength(0);
            int width = colors.GetLength(1);

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    colors[i, j] = float.MaxValue;
                }
            }
        }

        private void cbMeshes_SelectedIndexChanged(object sender, EventArgs e)
        {
            var cb = (ComboBox)sender;

            _mesh = _meshes[(string)cb.SelectedItem]();

            _bmpOutdated = true;

            HideButtons();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ShowButtons();
        }

        private void HideButtons()
        {
            btnShow.Show();

            cbMeshes.Hide();
        }

        private void ShowButtons()
        {
            btnShow.Hide();

            cbMeshes.Show();
        }
    }
}