using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AKG.ObjReader
{
    public struct ObjModel<T>
    {
        public string? name;
        public T[,] attributes;
        public Image<Rgba32>? mapKa;
        public Image<Rgba32>? mapKd;
        public Image<Rgba32>? mapKs;
        public Image<Rgba32>? mapBump;
        public Image<Rgba32>? mapAlbedo;
        public Image<Rgba32>? mapMetallic;
        public Image<Rgba32>? mapRoughness;
        public Image<Rgba32>? mapAO;
        public Image<Rgba32>? mapHeight;
    }
}
