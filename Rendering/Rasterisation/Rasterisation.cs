using AKG.Rendering.ShaderIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Rendering.Rasterisation
{
    public abstract class Rasterisation<A, U>
    {
        public abstract void Rasterize(Canvas canvas, List<VertexShaderOutput[]> voTriangles, ShaderProgram<A, U> shader, U uniforms, RenderingOptions options);
    }
}
