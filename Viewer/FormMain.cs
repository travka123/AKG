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

        private Vector4[,] _canvas;

        private Input _input;

        private VCamera _camera;

        private CameraControl _cameraControl;

        private Mesh _mesh;

        public FormMain()
        {
            InitializeComponent();

            _canvas = new Vector4[this.Height, this.Width];

            _input = new();

            _camera = new VCamera();

            _camera.SetProjection(Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 180 * 70, Width / Height, 0.1f, 100f));

            _cameraControl = new FlyingCameraControls(_camera, new Vector3(5, 0, 30));

            _mesh = new Teapot();

            cbMeshes.DataSource = new List<string>() { "teapot", "ncube" };
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            var timer = new System.Windows.Forms.Timer();

            timer.Tick += (obj, e) =>
            {
                var now = DateTime.Now;
                _input.msDelta = (now - _input.time).Milliseconds;
                _input.time = now;

                if (_input.msDelta > 0)
                {
                    if (_cameraControl.Process(_input))
                    {
                        Invalidate();
                    }
                }

                _input.mouseOffset = Vector2.Zero;
            };

            timer.Interval = 1;

            timer.Start();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            _input.pressedKeys.Add(e.KeyValue);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            _input.pressedKeys.Remove(e.KeyValue);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _input.mouseBtn1Pressed = true;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _input.mouseBtn1Pressed = false;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var location = new Vector2(e.X, e.Y);

            _input.mouseOffset = location - _input.mousePosition;
            _input.mousePosition = location;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {

        }

        private SolidBrush _whiteBrush = new SolidBrush(Color.White);

        protected override void OnPaint(PaintEventArgs e)
        {

            int canvasH = _canvas.GetLength(0);
            int canvasW = _canvas.GetLength(1);

            _mesh.Draw(_canvas, _camera);

            var colors = GetBMPColors(_canvas);

            GCHandle arr = GCHandle.Alloc(colors, GCHandleType.Pinned);

            int stride = colors.Length / canvasH;
            PixelFormat format = PixelFormat.Format32bppPArgb;

            IntPtr scan0 = arr.AddrOfPinnedObject();

            using (var bmp = new Bitmap(canvasW, canvasH, stride, format, scan0))
            {
                if (_stretch)
                {
                    int fHeight = this.Height;
                    int fWidth = this.Width;
                    e.Graphics.FillRectangle(_whiteBrush, 0, 0, Width, Height);
                    e.Graphics.DrawImage(bmp, 0, 0, fWidth, fHeight);
                }
                else
                {
                    e.Graphics.DrawImage(bmp, 0, 0);
                }
            }

            arr.Free();

            for (int i = 0; i < _canvas.GetLength(0); i++)
            {
                for (int j = 0; j < _canvas.GetLength(1); j++)
                {
                    _canvas[i, j] = Vector4.Zero;
                }
            }
        }

        private Vector4 _clearColor = new Vector4(0.2f, 0.3f, 0.3f, 1.0f);

        private byte[] GetBMPColors(Vector4[,] colors)
        {
            int height = colors.GetLength(0);
            int width = colors.GetLength(1);

            byte[] bytes = new byte[colors.Length * 4];

            int offset = 0;

            for (int y = 0; y < colors.GetLength(0); y++)
            {
                for (int x = 0; x < colors.GetLength(1); x++)
                {
                    var color = Vector4.Add(Vector4.Multiply(colors[y, x], colors[y, x].W), Vector4.Multiply(_clearColor, 1 - colors[y, x].W));

                    bytes[offset + 0] = (byte)(color.Z * 255);
                    bytes[offset + 1] = (byte)(color.Y * 255);
                    bytes[offset + 2] = (byte)(color.X * 255);
                    bytes[offset + 3] = (byte)(color.W * 255);

                    offset += 4;
                }
            }

            return bytes;
        }

        private void cbMeshes_SelectedIndexChanged(object sender, EventArgs e)
        {
            _mesh = ((ComboBox)sender).SelectedItem switch {
                "teapot" => new Teapot(),
                "ncube" => new NCube(),
            };

            Invalidate();
        }
    }
}