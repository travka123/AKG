using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Rendering.ShaderIO
{
    public struct FragmentShaderOutput
    {
        public Vector4 color;

        public FragmentShaderOutput(Vector4 color)
        {
            this.color = color;
        }
    }
}
