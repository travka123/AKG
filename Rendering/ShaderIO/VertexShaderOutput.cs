using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Rendering.ShaderIO
{
    public struct VertexShaderOutput
    {
        public Vector4 position;
        public float[] varying;

        public VertexShaderOutput(Vector4 position, float[] varying)
        {
            this.position = position;
            this.varying = varying;
        }
    }
}
