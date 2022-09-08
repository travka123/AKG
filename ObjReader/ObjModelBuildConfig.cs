using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AKG.ObjReader
{
    public struct ObjModelBuildConfig
    {
        public List<ObjModelAttr> layout;

        public ObjModelBuildConfig(List<ObjModelAttr> layout)
        {
            this.layout = layout;
        }
    }
}
