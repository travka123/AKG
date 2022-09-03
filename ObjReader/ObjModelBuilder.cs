using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Rendering
{
    public class ObjModelBuilder
    {
        private List<Vector4> _gVertices = new();

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

        public void AddGeometricVertices(Vector4 vert)
        {
            _gVertices.Add(vert);
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
                regionCurrent = new();
                regionCurrent.faceLines = new();
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

        public T[] BuildFlat<T>()
        {
            PushRegion();

            List<float> floats = new();

            Action<(int v, int, int)> pipeline;

            pipeline = (face) =>
            {
                var gv = _gVertices[face.v - 1];
                floats.Add(gv.X);
                floats.Add(gv.Y);
                floats.Add(gv.Z);
                floats.Add(gv.W);
            };

            foreach (var objFaces in regions)
            {
                foreach (var faces in objFaces.faceLines)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        pipeline(faces[i]);
                    }

                    for (int i = 3; i < faces.Count; i++)
                    {
                        pipeline(faces[i - 2]);
                        pipeline(faces[i - 1]);
                        pipeline(faces[i]);
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
