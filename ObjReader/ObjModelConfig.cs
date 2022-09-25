using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AKG.ObjReader
{
    public class ObjModelConfig
    {
        public List<ObjModelAttr> Attributes { get; } = new(); 
        public bool ContainTextures { get; set; }
        public bool ContainBump { get; set; }
        public bool PBR { get; set; }

        public bool Is(ObjModelBuildConfig bc)
        {
            return bc.layout.All((comp) => Attributes.Contains(comp)) && 
                (!bc.texturesRequired || ContainTextures) &&
                (!bc.bumpMap || ContainBump) &&
                (!bc.pbr || PBR);
        }
    }
}
