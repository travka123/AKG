﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AKG.ObjReader
{
    public class ObjModelConfig
    {
        public List<ObjModelAttr> Attributes { get; } = new(); 

        public bool Is(ObjModelBuildConfig bc)
        {
            return bc.layout.All((comp) => Attributes.Contains(comp));
        }
    }
}
