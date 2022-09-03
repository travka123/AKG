using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Rendering.ShaderIO
{
    public struct VertexShaderInput<A, U>
    {
        public A attribute;
        public U uniforms;

        public VertexShaderInput(A attribute, U uniforms)
        {
            this.attribute = attribute;
            this.uniforms = uniforms;
        }
    }
}
