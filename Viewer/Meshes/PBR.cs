using AKG.ObjReader;
using AKG.Rendering;
using Rendering;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using AKG.Rendering.ShaderIO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using System.ComponentModel.DataAnnotations;

namespace AKG.Viewer.Meshes
{
    public class PBR : Mesh
    {
        private struct CustomUniforms
        {
            public List<LightBox> lights;
            public Matrix4x4 MVP;
            public Matrix4x4 M;
            public Matrix4x4 TIM;
            public Vector3 ambientColor;
            public Vector3 camPos;

            public Image<Rgba32>? albedo;
            public Image<Rgba32>? bump;
            public Image<Rgba32>? metallic;
            public Image<Rgba32>? roughness;
            public Image<Rgba32>? ao;
            
            public CustomUniforms(Uniforms uniforms, ObjModel<Attributes> objModel)
            {
                lights = uniforms.lights;
                MVP = uniforms.MVP;
                M = uniforms.M;

                Matrix4x4.Invert(M, out TIM);
                TIM = Matrix4x4.Transpose(TIM);

                ambientColor = uniforms.ambientColor;
                camPos = uniforms.camera.Position;

                albedo = objModel.mapAlbedo;
                bump = objModel.mapBump;
                metallic = objModel.mapMetallic;
                roughness = objModel.mapRoughness;
                ao = objModel.mapAO;
            }
        }

        private struct Attributes
        {
            public Vector4 position;
            public Vector3 texCords;
            public Vector3 normal;
        }

        public static readonly List<ObjModelAttr> Layout = new() {
            ObjModelAttr.Position,
            ObjModelAttr.TexCords,
            ObjModelAttr.Normal,
        };

        public static readonly ObjModelBuildConfig ModelBuildConfig = new(Layout, pbr: true);

        private Renderer<Attributes, CustomUniforms> _renderer;

        private ObjModel<Attributes>[] _modelParts;

        public PBR(ObjModelBuilder builder)
        {
            _modelParts = builder.BuildByConfig<Attributes>(ModelBuildConfig);

            _renderer = new Renderer<Attributes, CustomUniforms>(new(VertexShader, FragmentShader));
        }

        private static VertexShaderOutput VertexShader(VertexShaderInput<Attributes, CustomUniforms> vi)
        {
            var positionMVP = Vector4.Transform(vi.attribute.position, vi.uniforms.MVP);

            float[] varying = new float[3 + 3 + 3];

            var positionM = Vector4.Transform(vi.attribute.position, vi.uniforms.M);
            var normalM = Vector3.Transform(vi.attribute.normal, vi.uniforms.TIM);

            positionM.CopyTo(varying, 0);
            vi.attribute.texCords.CopyTo(varying, 3);
            normalM.CopyTo(varying, 6);

            return new(positionMVP, varying);
        }

        private static FragmentShaderOutput FragmentShader(FragmentShaderInput<CustomUniforms> fi)
        {
            var span = new ReadOnlySpan<float>(fi.varying);

            var worldPos = new Vector3(span.Slice(0, 3));
            var texCords = new Vector3(span.Slice(3, 3));
            var N        = new Vector3(span.Slice(6, 3));

            var albedo = GetFromTexture(fi.uniforms.albedo, texCords);
            float metallic = GetFromTexture(fi.uniforms.metallic, texCords).X;
            float roughness = GetFromTexture(fi.uniforms.roughness, texCords).X;

            N = Vector3.Normalize(N) * GetFromTexture(fi.uniforms.bump, texCords);

            var ctw = fi.uniforms.camPos - worldPos;
            var V = Vector3.Normalize(ctw);

            var F0 = Mix(new Vector3(0.04f), albedo, metallic);

            var Lo = new Vector3();
            
            for (int i = 0; i < fi.uniforms.lights.Count; i++)
            {
                var light = fi.uniforms.lights[i];

                var ltw = light.Position - worldPos;
                var L = Vector3.Normalize(ltw);
                var H = Vector3.Normalize(V + L);

                float distance = ltw.Length();
                float attenuation = 1.0f / (distance * distance / 1000);
                var radiance = light.ColorDiffuse * attenuation;

                float NDF = DistributionGGX(N, H, roughness);
                float G = GeometrySmith(N, V, L, roughness);
                var F = FresnelSchlick(Math.Max(Vector3.Dot(H, V), 0.0f), F0);

                //Cook-Torrance BRDF
                var numerator = NDF * G * F;
                float denominator = 4.0f * Math.Max(Vector3.Dot(N, V), 0.0f) * Math.Max(Vector3.Dot(N, L), 0.0f) + 0.0001f;
                var specular = numerator / denominator;

                var ks = F;
                var kd = Vector3.One - ks;

                kd *= 1.0f - metallic;

                float NdotL = Math.Max(Vector3.Dot(N, L), 0.0f);
                Lo += (kd * albedo / (float)Math.PI + specular) * radiance * NdotL;
            }

            //Ambient
            var ao = GetFromTexture(fi.uniforms.ao, texCords);

            var ambient = fi.uniforms.ambientColor * albedo * ao;

            var color = Lo + ambient;

            color.X = Math.Min(color.X, 1.0f);
            color.Y = Math.Min(color.Y, 1.0f);
            color.Z = Math.Min(color.Z, 1.0f);

            //HDR?
            //color = color / (color + Vector3.One);
            //color.X = (float)Math.Pow(color.X, 1.0f / 2.2f);
            //color.Y = (float)Math.Pow(color.Y, 1.0f / 2.2f);
            //color.Z = (float)Math.Pow(color.Z, 1.0f / 2.2f);

            return new(new(color.X, color.Y, color.Z, 1.0f));
        }

        //vec3 fresnelSchlick(float cosTheta, vec3 F0)
        //{
        //    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
        //}

        private static Vector3 FresnelSchlick(float cosTheta, Vector3 F0)
        {
            float a = Math.Clamp(1.0f - cosTheta, 0.0f, 1.0f);
            float a2 = a * a;
            return F0 + (Vector3.One - F0) * a2 * a2 * a;
        }

        //float DistributionGGX(vec3 N, vec3 H, float roughness)
        //{
        //    float a = roughness * roughness;
        //    float a2 = a * a;
        //    float NdotH = max(dot(N, H), 0.0);
        //    float NdotH2 = NdotH * NdotH;

        //    float num = a2;
        //    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
        //    denom = PI * denom * denom;

        //    return num / denom;
        //}

        private static float DistributionGGX(Vector3 N, Vector3 H, float roughness)
        {
            float a = roughness * roughness;
            float a2 = a * a;
            float NdotH = Math.Max(Vector3.Dot(N, H), 0.0f);
            float NdotH2 = NdotH * NdotH;

            float denom = NdotH2 * (a2 - 1.0f) + 1.0f;
            denom = (float)Math.PI * denom * denom;

            return a2 / denom;
        }

        //float GeometrySchlickGGX(float NdotV, float roughness)
        //{
        //    float r = (roughness + 1.0);
        //    float k = (r * r) / 8.0;

        //    float num = NdotV;
        //    float denom = NdotV * (1.0 - k) + k;

        //    return num / denom;
        //}

        private static float GeometrySchlickGGX(float NdotV, float roughness)
        {
            float r = roughness + 1.0f;
            float k = r * r / 8.0f;

            float denom = NdotV * (1.0f - k) + k;

            return NdotV / denom;
        }

        //float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
        //{
        //    float NdotV = max(dot(N, V), 0.0);
        //    float NdotL = max(dot(N, L), 0.0);
        //    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
        //    float ggx1 = GeometrySchlickGGX(NdotL, roughness);

        //    return ggx1 * ggx2;
        //}

        private static float GeometrySmith(Vector3 N, Vector3 V, Vector3 L, float roughness)
        {
            float NdotV = Math.Max(Vector3.Dot(N, V), 0.0f);
            float NdotL = Math.Max(Vector3.Dot(N, L), 0.0f);

            float ggx2 = GeometrySchlickGGX(NdotV, roughness);
            float ggx1 = GeometrySchlickGGX(NdotL, roughness);

            return ggx1 * ggx2;
        }

        private static Vector3 Mix(Vector3 x, Vector3 y, float a)
        {
            return (1.0f - a) * x + a * y;
        }

        private static Vector3 GetFromTexture(Image<Rgba32>? image, Vector3 texCords)
        {
            if (image is null)
                return Vector3.One;

            int x = (int)((image.Width - 1) * texCords.X);
            int y = (int)((image.Height - 1) * (1 - texCords.Y));

            var rgba32 = image[x, y].ToScaledVector4();

            return new Vector3(rgba32.X, rgba32.Y, rgba32.Z);
        }

        public void Draw(Canvas canvas, Uniforms uniforms, RenderingOptions options)
        {
            foreach (var part in _modelParts)
                _renderer.Draw(canvas, part.attributes, new(uniforms, part), options);
        }

        public int GetVerticesNumber()
        {
            return _modelParts.Sum(m => m.attributes.Length);
        }
    }
}
