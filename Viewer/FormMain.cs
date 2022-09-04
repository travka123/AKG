using AKG.Camera;
using AKG.Rendering;
using AKG.Rendering.Rasterisation;
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
    struct Uniforms
    {
        public Camera camera;

        public Uniforms(Camera camera)
        {
            this.camera = camera;
        }
    }

    public partial class FormMain : Form
    {
        private Vector4[,] _canvas = null!;

        private Vector4[] _vertices = null!;

        private Renderer<Vector4, Uniforms> _renderer = null!;

        private ShaderProgram<Vector4, Uniforms> _shader;

        private Primitives _primitive = Primitives.TRIANGLE_LINES;

        private bool _stretch = false;

        private Uniforms _uniforms = new(new());

        public FormMain()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            _vertices = ObjFileParser.Parse("../../../../ObjFiles/humanoid_tri.obj").BuildFlat<Vector4>();

            _shader.vertexShader = (vi) =>
            {
                var position = Vector4.Transform(vi.attribute, vi.uniforms.camera.VP);

                return new(position, Array.Empty<float>());
            };

            _shader.fragmentShader = (fi) =>
            {
                return new(new Vector4(1.0f, 0.5f, 0.2f, 1.0f));
            };

            _renderer = new Renderer<Vector4, Uniforms>(_shader);


            _canvas = new Vector4[this.Height, this.Width];

            _uniforms.camera.SetProjection(Camera.CreatePerspectiveFieldOfView((float)((Math.PI / 180) * 70), this.Width / this.Height));

            _uniforms.camera.SetPosition(new Vector3(-20.0f, 0.0f, 30.0f));

            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            
        }

        private SolidBrush _whiteBrush = new SolidBrush(Color.White);

        protected override void OnPaint(PaintEventArgs e)
        {

            int canvasH = _canvas.GetLength(0);
            int canvasW = _canvas.GetLength(1);


            _renderer.Draw(_canvas, _primitive, _vertices, _uniforms);


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
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            Invalidate();
        }

        private Vector4 _clearColor = new Vector4(0.2f, 0.3f, 0.3f, 1.0f);

        private byte[] GetBMPColors(Vector4[,] colors)
        {
            int height = colors.GetLength(0);
            int width = colors.GetLength(1);

            byte[] bytes = new byte[colors.Length * 4];

            int offset = 0;
            for (int y = height - 1; y >= 0; y--)
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
    }
}