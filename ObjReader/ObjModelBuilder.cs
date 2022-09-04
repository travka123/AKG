using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Rendering
{
    public class ObjModelBuilder
    {
        private List<Vector4> _gVertices = new();
        private List<Vector3> _nVertices = new();
        private List<Vector3> _tVertices = new();

        private struct Region
        {
            public string group;
            public string mtl;
            public List<List<(int, int, int)>> faceLines;
        }

        List<Region> regions = new();

        Region regionCurrent = new();

        public ObjModelBuilder()
        {
            regionCurrent.faceLines = new();
        }

        public void AddGeometricVertices(Vector4 gv)
        {
            _gVertices.Add(gv);
        }

        public void AddNormalVertices(Vector3 nv)
        {
            _nVertices.Add(nv);
        }

        public void AddTextVertices(Vector3 tv)
        {
            _tVertices.Add(tv);
        }

        public void AddFaces(List<(int, int, int)> faces)
        {
            regionCurrent.faceLines.Add(faces);
        }

        public void PushRegion()
        {
            if (regionCurrent.faceLines.Count > 0)
            {
                regions.Add(regionCurrent);

                var newRegionCurrent = new Region();
                newRegionCurrent.faceLines = new();
                newRegionCurrent.mtl = regionCurrent.mtl;
                newRegionCurrent.group = regionCurrent.group;

                regionCurrent = newRegionCurrent;
            }
        }

        public void SetGroup(string group)
        {
            PushRegion();
            regionCurrent.group = group;
        }

        public void SetMtl(string mtl)
        {
            PushRegion();
            regionCurrent.mtl = mtl;
        }

        private class Material
        {
            public Vector3 Ka;
            public Vector3 Kd;
            public Vector3 Ks;
            public int illum;
            public float Ns;
        }

        private Dictionary<string, Material> _materials = new();
        private Material _currentMaterial;

        public void NewMaterial(string name)
        {
            _currentMaterial = new();
            _materials[name] = _currentMaterial;
        }

        bool useKa = false;

        public void SetKa(Vector3 ka)
        {
            _currentMaterial.Ka = ka;
            useKa = true;
        }

        bool useKd = false;

        public void SetKd(Vector3 kd)
        {
            _currentMaterial.Kd = kd;
            useKd = true;
        }

        bool useKs = false;

        public void SetKs(Vector3 ks)
        {
            _currentMaterial.Ks = ks;
            useKs = true;
        }

        bool useIllum = false;

        public void SetIllum(int illum)
        {
            _currentMaterial.illum = illum;
            useIllum = true;
        }

        bool useNs = false;

        public void SetNs(float ns)
        {
            _currentMaterial.Ns = ns;
            useNs = true;
        }

        public T[] BuildFlat<T>()
        {
            PushRegion();

            List<float> floats = new();

            Action<(int v, int tv, int nv), Material> pipeline;

            pipeline = (face, material) =>
            {
                var gv = _gVertices[face.v - 1];
                floats.Add(gv.X);
                floats.Add(gv.Y);
                floats.Add(gv.Z);
                floats.Add(gv.W);
            };

            if (_nVertices.Count > 0)
            {
                pipeline += (face, material) =>
                {
                    if (face.nv == 0) return;

                    var nv = _nVertices[face.nv - 1];
                    floats.Add(nv.X);
                    floats.Add(nv.Y);
                    floats.Add(nv.Z);
                };
            }

            if (_tVertices.Count > 0)
            {
                pipeline += (face, material) =>
                {
                    if (face.tv == 0) return;

                    var tv = _tVertices[face.tv - 1];
                    floats.Add(tv.X);
                    floats.Add(tv.Y);
                    floats.Add(tv.Z);
                };
            }

            if (useKa)
            {
                pipeline += (face, material) =>
                {
                    floats.Add(material.Ka.X);
                    floats.Add(material.Ka.Y);
                    floats.Add(material.Ka.Z);
                };
            }

            if (useKd)
            {
                pipeline += (face, material) =>
                {
                    floats.Add(material.Kd.X);
                    floats.Add(material.Kd.Y);
                    floats.Add(material.Kd.Z);
                };
            }

            if (useKs)
            {
                pipeline += (face, material) =>
                {
                    floats.Add(material.Ks.X);
                    floats.Add(material.Ks.Y);
                    floats.Add(material.Ks.Z);
                };
            }

            if (useIllum)
            {
                pipeline += (face, material) =>
                {
                    floats.Add(material.illum);
                };
            }

            if (useNs)
            {
                pipeline += (face, material) =>
                {
                    floats.Add(material.Ns);
                };
            }

            foreach (var objFaces in regions)
            {
                var material = objFaces.mtl is not null ? _materials[objFaces.mtl] : _materials.Count > 0 ? _materials.First().Value : null;

                foreach (var faces in objFaces.faceLines)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        pipeline(faces[i], material!);
                    }

                    for (int i = 3; i < faces.Count; i++)
                    {
                        for (int j = i - 1; j >= 1; j--)
                        {
                            pipeline(faces[i - j - 1], material!);
                            pipeline(faces[i - j], material!);
                            pipeline(faces[i], material!);
                        }
                    }
                }
            }

            return Cast<T>(floats.ToArray());
        }

        private T[] Cast<T>(float[] data)
        {
            

            int structSize = Marshal.SizeOf(typeof(T));
            int floatsInStruct = structSize / sizeof(float);
            IntPtr ptr = Marshal.AllocHGlobal(structSize);

            var result = new T[data.Length / floatsInStruct];

            for (int i = 0; i < data.Length; i += floatsInStruct)
            {
                Marshal.Copy(data, i, ptr, floatsInStruct);
                result[i / floatsInStruct] = (Marshal.PtrToStructure<T>(ptr)!);
            }

            return result;
        }
    }
}
