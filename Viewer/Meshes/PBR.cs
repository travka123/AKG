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
            public Matrix4x4 VP;
            public Matrix4x4 TIM;
            public Vector3 ambientColor;
            public Vector3 camPos;

            public Image<Rgba32>? albedo;
            public Image<Rgba32>? bump;
            public Image<Rgba32>? metallic;
            public Image<Rgba32>? roughness;
            public Image<Rgba32>? ao;
            public Image<Rgba32>? height;
            
            public CustomUniforms(Uniforms uniforms, ObjModel<Attributes> objModel)
            {
                lights = uniforms.lights;
                MVP = uniforms.MVP;
                M = uniforms.M;
                VP = uniforms.camera.VP;

                Matrix4x4.Invert(M, out TIM);
                TIM = Matrix4x4.Transpose(TIM);

                ambientColor = uniforms.ambientColor;
                camPos = uniforms.camera.Position;

                albedo = objModel.mapAlbedo;
                bump = objModel.mapBump;
                metallic = objModel.mapMetallic;
                roughness = objModel.mapRoughness;
                ao = objModel.mapAO;
                height = objModel.mapHeight;
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

        private struct ExtendedVertexShaderOutput
        {
            public VertexShaderOutput vo;
            public Vector3 positionM;
            public Vector3 normal;
            public float height;

            public ExtendedVertexShaderOutput(VertexShaderOutput vo, Vector3 position, Vector3 normal, float height)
            {
                this.vo = vo;
                this.positionM = position;
                this.normal = normal;
                this.height = height;
            }
        }

        //private static List<VertexShaderOutput[]> GeometryShader(GeometryShaderInput<CustomUniforms> go)
        //{
        //    if (go.uniforms.height is null)
        //        return new() { go.vo };

        //    var s1 = new ReadOnlySpan<float>(go.vo[0].varying);
        //    var s2 = new ReadOnlySpan<float>(go.vo[1].varying);
        //    var s3 = new ReadOnlySpan<float>(go.vo[2].varying);

        //    var position = new Vector3[] { new Vector3(s1.Slice(0, 3)), new Vector3(s2.Slice(0, 3)), new Vector3(s3.Slice(0, 3)) };
        //    var texCords = new Vector3[] { new Vector3(s1.Slice(3, 3)), new Vector3(s2.Slice(3, 3)), new Vector3(s3.Slice(3, 3)) };
        //    var normals = new Vector3[] { new Vector3(s1.Slice(6, 3)), new Vector3(s2.Slice(6, 3)), new Vector3(s3.Slice(6, 3)) };

        //    var triangles = new List<VertexShaderOutput[]>();

        //    for (int i = 0; i < 3; i++)
        //        normals[i] = Vector3.Normalize(normals[i]) * GetFromTexture(go.uniforms.bump, texCords[i]);

        //    Span<float> h = stackalloc float[3];
        //    for (int i = 0; i < 3; i++)
        //        h[i] = GetFromTexture(go.uniforms.height, texCords[i]).X;

        //    for (int i = 0; i < 3; i++)
        //    {
        //        position[i] += 0.1f * h[i] * normals[i];

        //        var positionMVP = Vector4.Transform(new Vector4(position[i], 1.0f), go.uniforms.VP);

        //        go.vo[i].W = positionMVP.W;
        //        go.vo[i].position = positionMVP / positionMVP.W;
        //        position[i].CopyTo(go.vo[i].varying, 0);
        //    }

        //    var evo = new ExtendedVertexShaderOutput[3];
        //    for (int i = 0; i < 3; i++)
        //        evo[i] = new ExtendedVertexShaderOutput(go.vo[i], position[i], normals[i], h[i]);

        //    DivideTriangle(evo, go, triangles, 0);

        //    return triangles;
        //}

        //private static void DivideTriangle(ExtendedVertexShaderOutput[] evo, GeometryShaderInput<CustomUniforms> go, List<VertexShaderOutput[]> triangles, int itr)
        //{
        //    var mvp1 = new Vector3(evo[0].vo.position.X, evo[0].vo.position.Y, evo[0].vo.position.Z);
        //    var mvp2 = new Vector3(evo[1].vo.position.X, evo[1].vo.position.Y, evo[1].vo.position.Z);
        //    var mvp3 = new Vector3(evo[2].vo.position.X, evo[2].vo.position.Y, evo[2].vo.position.Z);

        //    if ((itr > 5) || (float)Math.Abs(Vector3.Cross(mvp1 - mvp3, mvp2 - mvp3).Length()) < 0.0001f)
        //    {
        //        triangles.Add(new VertexShaderOutput[] { evo[0].vo, evo[1].vo, evo[2].vo });
        //        return;
        //    }

        //    float[] centerVaryings = new float[evo[0].vo.varying.Length];

        //    for (int i = 0; i < evo[0].vo.varying.Length; i++)
        //        centerVaryings[i] = (evo[0].vo.varying[i] + evo[1].vo.varying[i] + evo[2].vo.varying[i]) / 3;

        //    var centerPosition = new Vector3(centerVaryings[0], centerVaryings[1], centerVaryings[2]);
        //    var centerTexCords = new Vector3(centerVaryings[3], centerVaryings[4], centerVaryings[5]);

        //    var centerNormal = new Vector3(centerVaryings[6], centerVaryings[7], centerVaryings[8]);
        //    centerNormal = Vector3.Normalize(centerNormal) * GetFromTexture(go.uniforms.bump, centerTexCords);

        //    var centerH = GetFromTexture(go.uniforms.height, centerTexCords).X;

        //    centerPosition += 0.1f * centerH * centerNormal;

        //    var centerPositionMVP = Vector4.Transform(new Vector4(centerPosition, 1.0f), go.uniforms.VP);

        //    var cvo = new VertexShaderOutput(centerPositionMVP / centerPositionMVP.W, centerVaryings, centerPositionMVP.W);

        //    var centerEvo = new ExtendedVertexShaderOutput(cvo, centerPosition, centerNormal, centerH);

        //    var nEvo = new ExtendedVertexShaderOutput[3];

        //    itr++;

        //    nEvo[0] = centerEvo;
        //    nEvo[1] = evo[0];
        //    nEvo[2] = evo[1];
        //    DivideTriangle(nEvo, go, triangles, itr);

        //    nEvo[0] = centerEvo;
        //    nEvo[1] = evo[1];
        //    nEvo[2] = evo[2];
        //    DivideTriangle(nEvo, go, triangles, itr);

        //    nEvo[0] = centerEvo;
        //    nEvo[1] = evo[2];
        //    nEvo[2] = evo[0];
        //    DivideTriangle(nEvo, go, triangles, itr);
        //}

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

            //HDR
            color = color / (color + Vector3.One);
            color.X = (float)Math.Pow(color.X, 1.0f / 2.2f);
            color.Y = (float)Math.Pow(color.Y, 1.0f / 2.2f);
            color.Z = (float)Math.Pow(color.Z, 1.0f / 2.2f);

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
