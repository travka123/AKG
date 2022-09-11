using AKG.Camera;
using AKG.Camera.Controls;
using AKG.Components;
using AKG.ObjReader;
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
        private const string PATH = "..\\..\\..\\..\\ObjFiles\\";

        private bool _stretch = false;

        Bitmap _bmp;

        private Input _input;

        private Uniforms _uniforms;

        private CameraControl _controls;

        private Entity _entity;

        private Dictionary<string, (ObjModelBuildConfig conf, Func<Mesh> create)> _meshes;

        private ObjModelBuilder _builder;

        private Dictionary<string, Func<Positionable>> _selectables;

        public FormMain()
        {
            InitializeComponent();

            _bmp = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppPArgb);

            _input = new();

            var lights = new List<LightBox>() { new LightBox(new(10, 10, 10), new Vector3(1, 1, 1)) };

            var camera = new Camera();

            camera.SetProjection(Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 180 * 70, (float)Width / Height, 0.01f, 300f));

            camera.SetPosition(new Vector3(5, 0, 30));

            _entity = new Entity();

            _uniforms = new Uniforms(camera, lights);

            var files = Directory.EnumerateFiles(PATH, "*.obj");

            _meshes = new Dictionary<string, (ObjModelBuildConfig conf, Func<Mesh> create)>()
            {
                { "solid color", (SolidColor.ModelBuildConfig, () => new SolidColor(_builder!)) },
                { "normals", (Normals.ModelBuildConfig, () => new Normals(_builder!)) },
                { "lambert", (Lambert.ModelBuildConfig, () => new Lambert(_builder!)) },
            };

            _selectables = new Dictionary<string, Func<Positionable>>()
            {
                { "camera", () => _uniforms.camera },
                { "light", () => lights[0] },
                { "mesh", () => _entity },
            };

            cbModels.DataSource = files.Select(f => f.Substring(PATH.Length)).ToList();
            cbSelectedMesh.DataSource = _selectables.Keys.ToList();
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

                while (_entity is null) { Thread.Sleep(0); }

                while (true)
                {
                    if (_bmpOutdated)
                    {
                        _bmpOutdated = false;

                        Clear(v4Colors);
                        ClearZ(zBuffer);

                        _entity.Draw(v4Colors, zBuffer, _uniforms);

                        foreach (var light in _uniforms.lights)
                        {
                            light.Draw(v4Colors, zBuffer, _uniforms);
                        }

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
                        _input.mouseOffset = _input.mouseCurPosition - _input.mousePrevPosition;

                        _bmpOutdated |= _controls.Process(_input);

                        _input.mouseOffset = Vector2.Zero;
                        _input.mousePrevPosition = _input.mouseCurPosition;
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

        protected override void OnMouseEnter(EventArgs e)
        {

            lock (_inputLock)
            {
                _input.mousePrevPosition = _input.mouseCurPosition;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var location = new Vector2(e.X, e.Y);

            lock (_inputLock)
            {
                _input.mouseCurPosition = location;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            lock (_bmp)
            {
                if (_stretch)
                {
                    e.Graphics.DrawImage(_bmp, 0, 0, this.Width, this.Height);
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
            var meshStr = (string)((ComboBox)sender).SelectedItem;

            _entity.SetMesh(_meshes[meshStr].create());

            lock (_inputLock)
            {
                _bmpOutdated = true;
            }

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
            cbModels.Hide();
            cbSelectedMesh.Hide();
        }

        private void ShowButtons()
        {
            btnShow.Hide();

            cbMeshes.Show();
            cbModels.Show();
            cbSelectedMesh.Show();
        }

        private void cbModels_SelectedIndexChanged(object sender, EventArgs e)
        {
            var model = (string)((ComboBox)sender).SelectedItem;

            _builder = ObjFileParser.Parse(PATH + model);

            FilterMeshes();

            HideButtons();
        }

        private void FilterMeshes()
        {
            var conf = _builder.BuildConfig();

            cbMeshes.DataSource = _meshes.Where(m => conf.Is(m.Value.conf)).Select(c => c.Key).ToList();
        }

        private void cbSelectedMesh_SelectedIndexChanged(object sender, EventArgs e)
        {
            var positionable = _selectables[(string)((ComboBox)sender).SelectedItem]();

            _controls = new FlyingCameraControls(positionable);

            HideButtons();
        }
    }
}