using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Rendering
{
    public static class ScreenCoordinates
    {
        public static Vector2 PixelFromNDC(Vector4 position, int screenW, int screenH)
        {
            float px = (position.X + 1) / 2 * (screenW - 1);
            float py = (position.Y + 1) / 2 * (screenH - 1);
            return new Vector2(px, py);
        }

        public static Vector2 NDCFromPixel(Vector2 position, int screenW, int screenH)
        {
            float nx = 2 * position.X / (screenW - 1) - 1;
            float ny = 2 * position.Y / (screenH - 1) - 1;
            return new Vector2(nx, ny);
        }
    }
}
