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
            float py = screenH - (position.Y + 1) / 2 * (screenH - 1) - 1;
            return new Vector2(px, py);
        }
    }
}
