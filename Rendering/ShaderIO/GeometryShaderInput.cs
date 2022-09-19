using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Rendering.ShaderIO
{
    public struct GeometryShaderInput<U>
    {
        public VertexShaderOutput[] vo;
        public U uniforms;

        public GeometryShaderInput(VertexShaderOutput[] vo, U uniforms)
        {
            this.vo = vo;
            this.uniforms = uniforms;
        }
    }
}
