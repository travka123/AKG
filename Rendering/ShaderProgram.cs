using AKG.Rendering.ShaderIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Rendering
{
    public struct ShaderProgram<A, U>
    {
        public Func<VertexShaderInput<A, U>, VertexShaderOutput> vertexShader;
        public Func<FragmentShaderInput<U>, FragmentShaderOutput> fragmentShader;
    }
}
