using AKG.ObjReader;
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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Rendering
{
    public class ObjModelBuilder
    {
        public string Path { get; set; }
        public string FileName { get; set; }

        private List<Vector4> _gVertices = new();
        private List<Vector3> _nVertices = new();
        private List<Vector3> _tVertices = new();

        private struct Region
        {
            public string group;
            public string mtl;
            public List<List<(int v, int tv, int nv)>> faceLines;
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
            public string? mapKa;
            public string? mapKd;
            public string? mapKs;
            public string? mapBump;
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

        bool useTextures = false;

        public void SetMapKa(string file)
        {
            useTextures = true;
            _currentMaterial.mapKa = file;
        }

        public void SetMapKd(string file)
        {
            useTextures = true;
            _currentMaterial.mapKd = file;
        }

        public void SetMapKs(string file)
        {
            useTextures = true;
            _currentMaterial.mapKs = file;
        }

        bool useBumpFile = false;

        public void SetMapBump(string file)
        {
            useBumpFile = true;
            _currentMaterial.mapBump = file;
        }

        public ObjModelConfig BuildConfig()
        {
            PushRegion();

            var result = new ObjModelConfig();

            result.Attributes.Add(ObjModelAttr.Position);

            if (regions[0].faceLines[0][0].nv != 0) result.Attributes.Add(ObjModelAttr.Normal);

            if (regions[0].faceLines[0][0].tv != 0) result.Attributes.Add(ObjModelAttr.TexCords);

            if (useKa) result.Attributes.Add(ObjModelAttr.Ka);

            if (useKd) result.Attributes.Add(ObjModelAttr.Kd);

            if (useKs) result.Attributes.Add(ObjModelAttr.Ks);

            if (useIllum) result.Attributes.Add(ObjModelAttr.Illum);

            if (useNs) result.Attributes.Add(ObjModelAttr.Ns);

            result.containTextures = useTextures;

            result.containBump = useBumpFile;

            return result;
        }

        private delegate void PipelineMethod(List<float> floats, (int v, int tv, int nv) faces, Material material);

        public T[,] BuildFlatByConfig<T>(ObjModelBuildConfig conf)
        {
            PushRegion();

            var floats = new List<float>();
            PipelineMethod pipeline;

            var floatProviders = new Dictionary<ObjModelAttr, PipelineMethod>()
            {
                { ObjModelAttr.Position, AddPositions },
                { ObjModelAttr.Normal, AddNormals },
                { ObjModelAttr.TexCords, AddTexCords },
                { ObjModelAttr.Ka, AddKa },
                { ObjModelAttr.Kd, AddKd },
                { ObjModelAttr.Ks, AddKs },
                { ObjModelAttr.Illum, AddIllum },
                { ObjModelAttr.Ns, AddNs },
            };

            var keys = floatProviders.Keys.ToArray();

            pipeline = floatProviders[conf.layout[0]];

            for (int i = 1; i < conf.layout.Count; i++)
                pipeline += floatProviders[conf.layout[i]];

            return RunFlatPipe<T>(floats, pipeline);
        }

        public ObjModel<T>[] BuildByConfig<T>(ObjModelBuildConfig conf)
        {
            PushRegion();

            var floats = new List<float>();
            PipelineMethod pipeline;

            var floatProviders = new Dictionary<ObjModelAttr, PipelineMethod>()
            {
                { ObjModelAttr.Position, AddPositions },
                { ObjModelAttr.Normal, AddNormals },
                { ObjModelAttr.TexCords, AddTexCords },
                { ObjModelAttr.Ka, AddKa },
                { ObjModelAttr.Kd, AddKd },
                { ObjModelAttr.Ks, AddKs },
                { ObjModelAttr.Illum, AddIllum },
                { ObjModelAttr.Ns, AddNs },
            };

            var keys = floatProviders.Keys.ToArray();

            pipeline = floatProviders[conf.layout[0]];

            for (int i = 1; i < conf.layout.Count; i++)
                pipeline += floatProviders[conf.layout[i]];

            return RunPipe<T>(pipeline);
        }

        private void AddPositions(List<float> floats, (int v, int tv, int nv) face, Material material)
        {
            var gv = _gVertices[face.v - 1];
            floats.Add(gv.X);
            floats.Add(gv.Y);
            floats.Add(gv.Z);
            floats.Add(gv.W);
        }

        private void AddNormals(List<float> floats, (int v, int tv, int nv) face, Material material)
        {
            var nv = _nVertices[face.nv - 1];
            floats.Add(nv.X);
            floats.Add(nv.Y);
            floats.Add(nv.Z);
        }

        private void AddTexCords(List<float> floats, (int v, int tv, int nv) face, Material material)
        {
            var tv = _tVertices[face.tv - 1];
            floats.Add(tv.X);
            floats.Add(tv.Y);
            floats.Add(tv.Z);
        }

        private void AddKa(List<float> floats, (int v, int tv, int nv) face, Material material)
        {
            floats.Add(material.Ka.X);
            floats.Add(material.Ka.Y);
            floats.Add(material.Ka.Z);
        }

        private void AddKd(List<float> floats, (int v, int tv, int nv) face, Material material)
        {
            floats.Add(material.Kd.X);
            floats.Add(material.Kd.Y);
            floats.Add(material.Kd.Z);
        }

        private void AddKs(List<float> floats, (int v, int tv, int nv) face, Material material)
        {
            floats.Add(material.Ks.X);
            floats.Add(material.Ks.Y);
            floats.Add(material.Ks.Z);
        }

        private void AddIllum(List<float> floats, (int v, int tv, int nv) faces, Material material)
        {
            floats.Add(material.illum);
        }

        private void AddNs(List<float> floats, (int v, int tv, int nv) faces, Material material)
        {
            floats.Add(material.Ns);
        }

        private T[,] RunFlatPipe<T>(List<float> floats, PipelineMethod pipeline)
        {
            foreach (var objFaces in regions)
            {
                var material = objFaces.mtl is not null ? _materials[objFaces.mtl] : _materials.Count > 0 ? _materials.First().Value : null;

                foreach (var faces in objFaces.faceLines)
                {
                    for (int i = 2; i < faces.Count; i++)
                    {
                        pipeline(floats, faces[0], material!);
                        pipeline(floats, faces[i - 1], material!);
                        pipeline(floats, faces[i], material!);
                    }
                }
            }

            return TriangleArray(Cast<T>(floats.ToArray()));
        }

        private ObjModel<T>[] RunPipe<T>(PipelineMethod pipeline)
        {
            var textures = new Dictionary<string, Image<Rgba32>>();

            var models = new List<ObjModel<T>>();

            foreach (var objFaces in regions)
            {
                var model = new ObjModel<T>();

                var material = objFaces.mtl is not null ? _materials[objFaces.mtl] : _materials.Count > 0 ? _materials.First().Value : null;

                model.name = objFaces.group;

                model.mapKa = BufferLoad(material?.mapKa, textures);
                model.mapKd = BufferLoad(material?.mapKd, textures);
                model.mapKs = BufferLoad(material?.mapKs, textures);
                model.mapBump = BufferLoad(material?.mapBump, textures);

                var floats = new List<float>();

                foreach (var faces in objFaces.faceLines)
                {
                    for (int i = 2; i < faces.Count; i++)
                    {
                        pipeline(floats, faces[0], material!);
                        pipeline(floats, faces[i - 1], material!);
                        pipeline(floats, faces[i], material!);
                    }
                }

                model.attributes = TriangleArray(Cast<T>(floats.ToArray()));

                models.Add(model);
            }

            return models.ToArray();
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

        private Image<Rgba32>? BufferLoad(string? file, Dictionary<string, Image<Rgba32>> buffer)
        {
            if (file is null)
                return null;

            var image = buffer.GetValueOrDefault(file);

            if (image is null)
            {
                image = Image.Load<Rgba32>(Path + '\\' + file);
                buffer[file] = image;
            }

            return image;
        }

        private T[,] TriangleArray<T>(T[] array)
        {
            var tArr = new T[array.Length / 3, 3];

            int len = tArr.GetLength(0);

            for (int i = 0, j = 0; i < len; i++, j += 3)
            {
                tArr[i, 0] = array[j];
                tArr[i, 1] = array[j + 1];
                tArr[i, 2] = array[j + 2];            
            }

            return tArr;
        }
    }
}
