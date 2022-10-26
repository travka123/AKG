using System.Numerics;

namespace AKG.Viewer
{
    public interface Light
    { 
        public Vector3 ColorDiffuse { get; set; }
        public Vector3 ColorSpecular { get; set; }
        public float Intensity { get; set; }
    }
}
