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

        private Bitmap _bmp;

        private byte[] _bmpColors;

        private Input _input;

        private Uniforms _uniforms;

        private Entity _entity;

        private Dictionary<string, (ObjModelBuildConfig conf, Func<Mesh> create)> _meshes;

        private Dictionary<string, Func<Positionable>> _selectables;

        private RenderingOptions _renderingOptions;

        private Vector4 _clearColor = new Vector4(0.2f, 0.3f, 0.3f, 1.0f);

        Canvas _canvas;

        private Vector4[] _shadowColorBuffer;

        private ObjModelBuilder _builder = null!;

        private CameraControl _controls;

        private bool PBRDemo = false;

        private List<string> _models;

        private ImageAttributes _imageAttributes = new ImageAttributes();

        public FormMain()
        {
            InitializeComponent();

            _canvas = new Canvas(Height, Width);

            _shadowColorBuffer = new Vector4[Height * Width];

            _bmp = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppPArgb);

            _bmpColors = new byte[Height * Width * 4];

            _input = new();

            var camera = new SCamera();

            camera.SetProjection(Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 180 * 70, (float)Width / Height, 0.01f, 300f));

            camera.SetPosition(new Vector3(5, 0, 30));

            _entity = new Entity();

            var lights = new List<LightBox>()
            {
                new LightBox(new Vector3(5, 5, 5), new Vector3(1, 1, 1), 1000),
            };

            _uniforms = new Uniforms(camera, lights);

            _uniforms.ambientColor = new Vector3(0.0f, 0.0f, 0.0f);

            _renderingOptions = new RenderingOptions();
            _renderingOptions.FillTriangles = true;

            var opt = new EnumerationOptions();
            opt.RecurseSubdirectories = true;
            _models = Directory.EnumerateFiles(PATH, "*.obj", opt).ToList();

            _entity = new Entity();

            _controls = new FlyingCameraControls(_uniforms.camera);

            _uniforms.lights = new List<LightBox>() { new LightBox(new(5, 5, 5), new Vector3(1, 1, 1), 1000) };

            _meshes = new Dictionary<string, (ObjModelBuildConfig conf, Func<Mesh> create)>()
            {
                { "solid color", (SolidColor.ModelBuildConfig, () => new SolidColor(_builder)) },
                { "normals", (Normals.ModelBuildConfig, () => new Normals(_builder)) },
                { "bnormals", (Bnormals.ModelBuildConfig, () => new Bnormals(_builder)) },
                { "flat & lambert", (FlatLambert.ModelBuildConfig, () => new FlatLambert(_builder)) },
                { "lambert", (Lambert.ModelBuildConfig, () => new Lambert(_builder)) },
                { "phong", (Phong.ModelBuildConfig, () => new Phong(_builder)) },
                { "textured", (Textured.ModelBuildConfig, () => new Textured(_builder)) },
                { "bump maps", (TBSMap.ModelBuildConfig, () => new TBSMap(_builder)) },
                { "PBR", (PBR.ModelBuildConfig, () => new PBR(_builder)) },
            };

            _selectables = new Dictionary<string, Func<Positionable>>()
            {
                { "camera", () => _uniforms.camera },
                { "light", () => _uniforms.lights[0] },
                { "mesh", () => _entity },
            };

            cbModels.DataSource = _models.Select(f => f.Substring(PATH.Length)).ToList();
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
        private HashSet<int> _prevPressed = new HashSet<int>();

        private void ProcessInput()
        {
            lock (_inputLock)
            {
                var now = DateTime.Now;

                _input.msDelta = (now - _input.time).Milliseconds;
                _input.time = DateTime.Now;
                _input.mouseOffset = _input.mouseCurPosition - _input.mousePrevPosition;

                _controls.Process(_input);

                _input.mouseOffset = Vector2.Zero;
                _input.mousePrevPosition = _input.mouseCurPosition;

                var nKeysDown = _input.pressedKeys.Where(i => !_prevPressed.Contains(i)).ToHashSet();

                if (nKeysDown.Contains(120))
                {
                    _renderingOptions.FillTriangles = !_renderingOptions.FillTriangles;
                }

                if (nKeysDown.Contains(115))
                {
                    Invoke(() => SwitchPBRDemo());
                }

                if (nKeysDown.Contains(84))
                {
                    _renderingOptions.UseTessellation = !_renderingOptions.UseTessellation;
                }

                _prevPressed.Clear();
                _prevPressed.UnionWith(_input.pressedKeys);
            }
        }

        private bool painted = true;

        private void Render()
        {
            _canvas.Clear(_clearColor);

            _entity.Draw(_canvas, _uniforms, _renderingOptions);

            foreach (var light in _uniforms.lights)
            {
                light.Draw(_canvas, _uniforms, _renderingOptions);
            }

            lock (_bmp)
            {
                while (!painted)
                    Monitor.Wait(_bmp);

                painted = false;
            }

            Array.Copy(_canvas.colors, _shadowColorBuffer, _canvas.colors.Length);

            Task.Run(() =>
            {
                lock (_bmp)
                {
                    Canvas.GetBMPColors(_bmpColors, _shadowColorBuffer);

                    var bitmapInfo = _bmp.LockBits(new Rectangle(0, 0, _bmp.Width, _bmp.Height), ImageLockMode.ReadWrite, _bmp.PixelFormat);

                    Marshal.Copy(_bmpColors, 0, bitmapInfo.Scan0, _bmpColors.Length);

                    _bmp.UnlockBits(bitmapInfo);

                    try
                    {

                        var graphics = CreateGraphics();

                        graphics.DrawImage(_bmp, new Rectangle(0, 0, this.Width, this.Height), 0, 0, _bmp.Width, _bmp.Height, GraphicsUnit.Pixel, _imageAttributes);

                    }
                    finally
                    {

                    }

                    painted = true;
                    Monitor.Pulse(_bmp);
                }
            });
        }

        //private DateTime fpsLastPrintTime = DateTime.Now;
        //private int fpsCounter = 0;

        //private void ProcessFPS()
        //{
        //    fpsCounter++;

        //    var curTime = DateTime.Now;

        //    var sDiff = (curTime - fpsLastPrintTime).TotalSeconds;

        //    if (sDiff > 1.0)
        //    {
        //        lFPS.Text = $"FPS: {fpsCounter}";
        //        fpsLastPrintTime = curTime;
        //        fpsCounter = 0;
        //    }
        //}

        private void SwitchPBRDemo()
        {
            if (!PBRDemo)
            {
                PBRDemo = true;

                HideButtons();
                lVertices.Hide();
                btnShow.Hide();

                _uniforms.lights = new List<LightBox>() {
                    new LightBox(new(-10,  10,  15), new Vector3(1, 1, 0), 1000),
                    new LightBox(new( 10,  10,  15), new Vector3(1, 0, 1), 1000),
                    new LightBox(new(  0, -10,  15), new Vector3(0, 1, 1), 1000),
                    new LightBox(new(  0,   0, -15), new Vector3(1, 1, 1), 1000),
                };

                _controls = new FlyingCameraControls(_uniforms.camera);

                _clearColor = new Vector4(0.0064f, 0.0064f, 0.0064f, 1.0f);

                _uniforms.ambientColor = new Vector3(0.0f, 0.0f, 0.0f);

                _entity.SetMesh(new PBRDemo(_models.Where(f => f.Contains("ObjFiles\\PBR\\")).ToList()));
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

        private void cbMeshes_SelectedIndexChanged(object sender, EventArgs e)
        {
            var meshStr = (string)((ComboBox)sender).SelectedItem;

            _entity.SetMesh(_meshes[meshStr].create());

            ShowVerticesCount();

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

            HideButtons();
        }

        private void btnDiffuse_Click(object sender, EventArgs e)
        {
            _uniforms.lights[0].ColorDiffuse = GetColorFromUser(_uniforms.lights[0].ColorDiffuse);

            HideButtons();
        }

        private void btnSpecular_Click(object sender, EventArgs e)
        {
            _uniforms.lights[0].ColorSpecular = GetColorFromUser(_uniforms.lights[0].ColorSpecular);

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