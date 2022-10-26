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
using static System.Windows.Forms.DataFormats;

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
            public Image<Rgba32>? emissive;

            public int tessellationLvl;

            public CustomUniforms(Uniforms uniforms, ObjModel<Attributes> objModel, int tessellationLvl)
            {
                lights = uniforms.lights;
                MVP = uniforms.MVP;
                M = uniforms.M;
                VP = uniforms.camera.VP;
                TIM = ShaderHelper.TransposeInverseMatrix(M);

                ambientColor = uniforms.ambientColor;
                camPos = uniforms.camera.Position;

                albedo = objModel.mapAlbedo;
                bump = objModel.mapBump;
                metallic = objModel.mapMetallic;
                roughness = objModel.mapRoughness;
                ao = objModel.mapAO;
                height = objModel.mapHeight;
                emissive = objModel.mapEmissive;

                this.tessellationLvl = tessellationLvl;
            }
        }

        private struct Attributes
        {
            public Vector4 position;
            public Vector3 texCords;
            public Vector3 normal;
            public Vector3 tangent;
            public Vector3 bitangent;
        }

        public static readonly List<ObjModelAttr> Layout = new() {
            ObjModelAttr.Position,
            ObjModelAttr.TexCords,
            ObjModelAttr.Normal,
            ObjModelAttr.TanBitan,
        };

        public static readonly ObjModelBuildConfig ModelBuildConfig = new(Layout, pbr: true);

        private Renderer<Attributes, CustomUniforms> _renderer;

        private ObjModel<Attributes>[] _modelParts;

        private Vector4 _objModelCenter;

        public PBR(ObjModelBuilder builder)
        {
            _modelParts = builder.BuildByConfig<Attributes>(ModelBuildConfig);

            _objModelCenter = new Vector4();
            foreach (var part in _modelParts)
            {
                foreach (var attr in part.attributes)
                {
                    _objModelCenter += attr.position;
                }
            }
            _objModelCenter /= _objModelCenter.W;

            _renderer = new Renderer<Attributes, CustomUniforms>(new(VertexShader, FragmentShader, GeometryShader));
        }

        private static VertexShaderOutput VertexShader(VertexShaderInput<Attributes, CustomUniforms> vi)
        {
            var positionMVP = Vector4.Transform(vi.attribute.position, vi.uniforms.MVP);

            float[] varying = new float[3 + 3 + 3 + 9];

            var positionM = Vector4.Transform(vi.attribute.position, vi.uniforms.M);
            var normalM = Vector3.Transform(vi.attribute.normal, vi.uniforms.TIM);

            positionM.CopyTo(varying, 0);

            vi.attribute.texCords.CopyTo(varying, 3);

            normalM.CopyTo(varying, 6);

            vi.attribute.normal.CopyTo(varying, 9);
            vi.attribute.tangent.CopyTo(varying, 12);
            vi.attribute.bitangent.CopyTo(varying, 15);

            return new(positionMVP, varying);
        }

        private struct ExtendedVertexShaderOutput
        {
            public VertexShaderOutput vo;
            public Vector3 defNormal;
            public Vector3 defPosition;
            public Vector4 defPositionMVP;
        }

        private const float hK = 0.2f;

        private static List<VertexShaderOutput[]> GeometryShader(GeometryShaderInput<CustomUniforms> go)
        {
            if (go.uniforms.height is null || (go.uniforms.tessellationLvl == -1))
                return new() { go.vo };

            var triangles = new List<VertexShaderOutput[]>();

            var evo = new ExtendedVertexShaderOutput[3];
                
            for (int i = 0; i < 3; i++)
                evo[i] = GetHEVO(go, new Vector3(new ReadOnlySpan<float>(go.vo[i].varying)), 
                    Vector3.Normalize(new Vector3(new ReadOnlySpan<float>(go.vo[i].varying).Slice(6))), go.vo[i].varying);

            Tessellation(evo, go, triangles, 0);

            return triangles;
        }

        private static void Tessellation(ExtendedVertexShaderOutput[] evo, GeometryShaderInput<CustomUniforms> go, List<VertexShaderOutput[]> triangles, int lvl)
        {
            if (lvl < go.uniforms.tessellationLvl)
            {
                var nEVO = new ExtendedVertexShaderOutput[3];
                nEVO[0] = GetHEVO(go, evo[0], evo[1]);
                nEVO[1] = GetHEVO(go, evo[1], evo[2]);
                nEVO[2] = GetHEVO(go, evo[2], evo[0]);

                lvl++;

                Tessellation(nEVO, go, triangles, lvl);
                Tessellation(new ExtendedVertexShaderOutput[] { nEVO[0], evo[1], nEVO[1] }, go, triangles, lvl);
                Tessellation(new ExtendedVertexShaderOutput[] { nEVO[1], evo[2], nEVO[2] }, go, triangles, lvl);
                Tessellation(new ExtendedVertexShaderOutput[] { nEVO[2], evo[0], nEVO[0] }, go, triangles, lvl);
            }
            else
            {
                triangles.Add(new VertexShaderOutput[] { evo[0].vo, evo[1].vo, evo[2].vo });
            }
        }

        private static ExtendedVertexShaderOutput GetHEVO(GeometryShaderInput<CustomUniforms> go, ExtendedVertexShaderOutput a, ExtendedVertexShaderOutput b)
        {
            var varying = new float[a.vo.varying.Length];

            for (int i = 0; i < a.vo.varying.Length; i++)
                varying[i] = (a.vo.varying[i] + b.vo.varying[i]) / 2;

            var hcos = Vector3.Dot(a.defNormal, Vector3.Normalize(a.defNormal + b.defNormal));
            float hdist = (a.defPosition - b.defPosition).Length() / 2;
            float r = hdist / (float)Math.Abs(Math.Sqrt(1 - hcos * hcos));
            var center = (a.defPosition - (r * a.defNormal) + b.defPosition - (r * b.defNormal)) / 2;

            var defNormal = Vector3.Normalize(new Vector3(new ReadOnlySpan<float>(varying).Slice(6)));

            var defPosition = center + defNormal * r;

            return GetHEVO(go, defPosition, defNormal, varying);
        }

        private static ExtendedVertexShaderOutput GetHEVO(GeometryShaderInput<CustomUniforms> go, Vector3 defPosition, Vector3 defNormal, float[] varying)
        {
            var result = new ExtendedVertexShaderOutput();

            result.defPosition = defPosition;
            result.defNormal = defNormal;

            result.defPositionMVP = Vector4.Transform(new Vector4(result.defPosition, 1.0f), go.uniforms.VP);
            result.defPositionMVP /= result.defPositionMVP.W;

            result.vo.varying = varying;

            var texCords = new Vector3(result.vo.varying[3], result.vo.varying[4], result.vo.varying[5]);

            var h = GetFromTexture(go.uniforms.height, texCords).X;

            var translatedPositionM = result.defPosition + hK * h * result.defNormal;

            var translatedPositionMVP = Vector4.Transform(new Vector4(translatedPositionM, 1.0f), go.uniforms.VP);

            translatedPositionM.CopyTo(result.vo.varying, 0);

            result.vo.W = translatedPositionMVP.W;
            result.vo.position = translatedPositionMVP / translatedPositionMVP.W;

            return result;
        }

        private static FragmentShaderOutput FragmentShader(FragmentShaderInput<CustomUniforms> fi)
        {
            var span = new ReadOnlySpan<float>(fi.varying);

            var worldPos = new Vector3(span.Slice(0, 3));
            var texCords = new Vector3(span.Slice(3, 3));

            //Emissive
            if (fi.uniforms.emissive is not null)
            {
                var emissive = GetFromTexture(fi.uniforms.emissive, texCords);
                if (emissive.Length() > 0.1f)
                {
                    return new(new(emissive.X, emissive.Y, emissive.Z, 1.0f));
                }
            }

            var albedo = ShaderHelper.SrgbToLinear(GetFromTexture(fi.uniforms.albedo, texCords));
            float metallic = GetFromTexture(fi.uniforms.metallic, texCords).X;
            float roughness = GetFromTexture(fi.uniforms.roughness, texCords).X;

            var N = ShaderHelper.NormalFromTexture(fi.uniforms.bump, texCords, fi.uniforms.TIM, span[9..]);

            //return new(new(Math.Clamp(N.X, 0, 1), Math.Clamp(N.Y, 0, 1), Math.Clamp(N.Z, 0, 1), 1.0f));

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
                float attenuation = 1.0f / (distance * distance) * light.Intensity;
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
            color = ShaderHelper.ACESFilm(color);
            color = ShaderHelper.LinearToSrgb(color);

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
            float dist = (Vector4.Transform(_objModelCenter, uniforms.M) - new Vector4(uniforms.camera.Position, 1.0f)).Length();

            int tlvl = dist > 10.0f ? 0 : dist > 5.0f ? 1 : dist > 3.0f ? 2 : 3;

            foreach (var part in _modelParts)
                _renderer.Draw(canvas, part.attributes, new(uniforms, part, options.UseTessellation ? tlvl : -1), options);
        }

        public int GetVerticesNumber()
        {
            return _modelParts.Sum(m => m.attributes.Length);
        }
    }
}
