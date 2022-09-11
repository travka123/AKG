﻿using AKG.Camera;
using AKG.ObjReader;
using System.Numerics;

namespace AKG.Viewer
{
    public class Uniforms
    {
        public global::SCamera camera;
        public List<LightBox> lights;
        public Matrix4x4 MVP;
        public Matrix4x4 M;
        public Vector3 ambientColor;

        public Uniforms(global::SCamera camera, List<LightBox> lights)
        {
            this.camera = camera;
            this.lights = lights;
        }

        public Uniforms(Uniforms uniforms, Matrix4x4 M)
        {
            camera = uniforms.camera;
            lights = uniforms.lights;
            ambientColor = uniforms.ambientColor;
            this.M = M;
            MVP = M * camera.VP;
        }
    }
}