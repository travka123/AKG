using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Rendering.ShaderIO
{
    public struct FragmentShaderInput<U>
    {
        public float[] varying;
        public U uniforms;
        public Vector2 screenPoint;

        public FragmentShaderInput(float[] varying, U uniforms, Vector2 screenPoint)
        {
            this.varying = varying;
            this.uniforms = uniforms;
            this.screenPoint = screenPoint;
        }
    }
}
