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
using System;
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

        private RenderingOptions _renderingOptions;

        private Vector4 _clearColor = new Vector4(0.2f, 0.3f, 0.3f, 1.0f);

        Vector4[,] colorsBuffer;

        float[,] zBuffer;

        public FormMain()
        {
            InitializeComponent();

            colorsBuffer = new Vector4[Height, Width];

            zBuffer = new float[Height, Width];

            _bmp = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppPArgb);

            _input = new();

            var lights = new List<LightBox>() { new LightBox(new(10, 10, 10), new Vector3(1, 1, 1)) };

            var camera = new SCamera();

            camera.SetProjection(Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 180 * 70, (float)Width / Height, 0.01f, 300f));

            camera.SetPosition(new Vector3(5, 0, 30));

            _entity = new Entity();

            _uniforms = new Uniforms(camera, lights);

            _uniforms.ambientColor = new Vector3(0.3f, 0.3f, 0.3f);

            _renderingOptions = new RenderingOptions();
            _renderingOptions.FillTriangles = true;

            var opt = new EnumerationOptions();
            opt.RecurseSubdirectories = true;
            var files = Directory.EnumerateFiles(PATH, "*.obj", opt);

            _meshes = new Dictionary<string, (ObjModelBuildConfig conf, Func<Mesh> create)>()
            {
                { "solid color", (SolidColor.ModelBuildConfig, () => new SolidColor(_builder!)) },
                { "normals", (Normals.ModelBuildConfig, () => new Normals(_builder!)) },
                { "bnormals", (Bnormals.ModelBuildConfig, () => new Bnormals(_builder!)) },
                { "flat & lambert", (FlatLambert.ModelBuildConfig, () => new FlatLambert(_builder!)) },
                { "lambert", (Lambert.ModelBuildConfig, () => new Lambert(_builder!)) },
                { "phong", (Phong.ModelBuildConfig, () => new Phong(_builder!)) },
                { "textured", (Textured.ModelBuildConfig, () => new Textured(_builder!)) },
                { "bump maps", (TBSMap.ModelBuildConfig, () => new TBSMap(_builder!)) },
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

        protected override void OnLoad(EventArgs e)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    ProcessInput();

                    Render();
                }
            });
        }

        private readonly object _inputLock = new object();
        private volatile bool _bmpOutdated = true;
        private HashSet<int> _prevPressed = new HashSet<int>();

        private void ProcessInput()
        {
            lock (_inputLock)
            {
                var now = DateTime.Now;

                _input.msDelta = (now - _input.time).Milliseconds;
                _input.time = DateTime.Now;
                _input.mouseOffset = _input.mouseCurPosition - _input.mousePrevPosition;

                _bmpOutdated |= _controls.Process(_input);

                _input.mouseOffset = Vector2.Zero;
                _input.mousePrevPosition = _input.mouseCurPosition;

                var nKeysDown = _input.pressedKeys.Where(i => !_prevPressed.Contains(i)).ToHashSet();

                if (nKeysDown.Contains(120))
                {
                    _renderingOptions.FillTriangles = !_renderingOptions.FillTriangles;
                    _bmpOutdated = true;
                }

                _prevPressed.Clear();
                _prevPressed.UnionWith(_input.pressedKeys);
            }
        }

        private bool painted = true;

        private void Render()
        {
            if (_bmpOutdated)
            {
                Clear(colorsBuffer, _clearColor);
                Clear(zBuffer, float.MaxValue);

                _entity.Draw(colorsBuffer, zBuffer, _uniforms, _renderingOptions);

                foreach (var light in _uniforms.lights)
                {
                    light.Draw(colorsBuffer, zBuffer, _uniforms, _renderingOptions);
                }

                var colors = GetBMPColors(colorsBuffer);

                lock (_bmp)
                {
                    while(!painted)
                        Monitor.Wait(_bmp);

                    var bitmapInfo = _bmp.LockBits(new Rectangle(0, 0, _bmp.Width, _bmp.Height), ImageLockMode.ReadWrite, _bmp.PixelFormat);

                    Marshal.Copy(colors, 0, bitmapInfo.Scan0, colors.Length);

                    _bmp.UnlockBits(bitmapInfo);

                    painted = false;

                    Invalidate();
                }

                _bmpOutdated = false;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ProcessFPS();

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

                painted = true;
                Monitor.Pulse(_bmp);
            }
        }

        private DateTime fpsLastPrintTime = DateTime.Now;
        private int fpsCounter = 0;

        private void ProcessFPS()
        {
            fpsCounter++;

            var curTime = DateTime.Now;

            var sDiff = (curTime - fpsLastPrintTime).TotalSeconds;

            if (sDiff > 1.0)
            {
                lFPS.Text = $"FPS: {fpsCounter}";
                fpsLastPrintTime = curTime;
                fpsCounter = 0;
            }
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

        private void Clear(Vector4[,] buffer, Vector4 value)
        {
            int height = buffer.GetLength(0);
            int width = buffer.GetLength(1);

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    buffer[i, j] = _clearColor;
                }
            }
        }

        private void Clear(float[,] buffer, float value)
        {
            int height = buffer.GetLength(0);
            int width = buffer.GetLength(1);

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    buffer[i, j] = value;
                }
            }
        }

        private void cbMeshes_SelectedIndexChanged(object sender, EventArgs e)
        {
            var meshStr = (string)((ComboBox)sender).SelectedItem;

            _entity.SetMesh(_meshes[meshStr].create());

            ShowVerticesCount();

            lock (_inputLock)
            {
                _bmpOutdated = true;
            }

            HideButtons();
        }

        private void ShowVerticesCount()
        {
            lVertices.Text = $"Vertices: {_entity.GetVerticesNumber()}";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ShowButtons();
        }

        private void HideButtons()
        {
            btnShow.Show();
            cbMeshes.Focus();
            cbMeshes.Hide();
            cbModels.Hide();
            cbSelectedMesh.Hide();
            btnAmbient.Hide();
            btnDiffuse.Hide();
            btnSpecular.Hide();
        }

        private void ShowButtons()
        {
            btnShow.Hide();

            cbMeshes.Show();
            cbModels.Show();
            cbSelectedMesh.Show();
            btnAmbient.Show();
            btnDiffuse.Show();
            btnSpecular.Show();
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

        private void FormMain_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                case Keys.Up:
                case Keys.Left:
                case Keys.Right:
                    e.IsInputKey = true;
                    break;
            }
        }

        private void cbModels_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
        }

        private void cbMeshes_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
        }

        private void cbSelectedMesh_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
        }

        private void emptyBtnKeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
        }

        private void btnAmbient_Click(object sender, EventArgs e)
        {
            _uniforms.ambientColor = GetColorFromUser(_uniforms.ambientColor);

            lock (_inputLock)
            {
                _bmpOutdated = true;
            }

            HideButtons();
        }

        private void btnDiffuse_Click(object sender, EventArgs e)
        {
            _uniforms.lights[0].ColorDiffuse = GetColorFromUser(_uniforms.lights[0].ColorDiffuse);

            lock (_inputLock)
            {
                _bmpOutdated = true;
            }

            HideButtons();
        }

        private void btnSpecular_Click(object sender, EventArgs e)
        {
            _uniforms.lights[0].ColorSpecular = GetColorFromUser(_uniforms.lights[0].ColorSpecular);

            lock (_inputLock)
            {
                _bmpOutdated = true;
            }

            HideButtons();
        }

        private Vector3 GetColorFromUser(Vector3 initColor)
        {
            initColor *= 255;
            cdMain.Color = Color.FromArgb(255, (int)initColor.X, (int)initColor.Y, (int)initColor.Z);
            cdMain.ShowDialog();
            return new Vector3((float)cdMain.Color.R / 255, (float)cdMain.Color.G / 255, (float)cdMain.Color.B / 255);
        }
    }
}