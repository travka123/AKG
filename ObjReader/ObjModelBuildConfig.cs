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
        public bool texturesRequired;
        public bool bumpMap;

        public ObjModelBuildConfig(List<ObjModelAttr> layout, bool texturesRequired = false, bool bumpMap = false)
        {
            this.layout = layout;
            this.texturesRequired = texturesRequired;
            this.bumpMap = bumpMap;
        }
    }
}
