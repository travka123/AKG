using System.Numerics;

namespace Rendering
{
    public static class ObjFileParser
    {
        public static ObjModelBuilder Parse(Stream ms)
        {
            ObjModelBuilder builder = new ObjModelBuilder();

            var parsers = new Dictionary<string, Action<string>>()
            {
                { "v", (s) => builder.AddGeometricVertices(ParseVector4(s)) },
                { "vn", (s) => builder.AddNormalVertices(ParseVector4(s)) },
                { "f", (s) => builder.AddFaces(ParseFaces(s)) },
                { "g", (s) => builder.SetGroup(s) },
                { "usemtl", (s) => builder.SetMtl(s) },
                { "#", (s) => { } },
            };

            var reader = new StreamReader(ms);
            string? line = reader.ReadLine();
            while (line is not null)
            {
                line = line.Trim();

                int wp = line.IndexOf(' ');
                if (wp > 0)
                {
                    parsers[line[..wp]](line[(wp + 1)..]);
                }

                line = reader.ReadLine();
            }

            return builder;
        }

        private static Vector4 ParseVector4(string str)
        {
            var values = str.Replace('.', ',').Split(' ').Where((s) => s.Length > 0).ToArray();
            return new Vector4(float.Parse(values[0]), float.Parse(values[1]),
                float.Parse(values[2]), values.Length == 4 ? float.Parse(values[3]) : 1.0f);
        }

        private static List<(int, int, int)> ParseFaces(string str)
        {
            var values = str.Split(' ').Where((s) => s.Length > 0).Select((f) => f.Split('/'));
            return values.Select((f) => (
                int.Parse(f[0]),
                (f.Length >= 2) && (f[1].Length > 0) ? int.Parse(f[1]) : 0,
                f.Length == 3 ? int.Parse(f[2]) : 0)).ToList();
        }

        public static ObjModelBuilder Parse(string path)
        {
            using (var file = File.OpenRead(path))
            {
                return Parse(file);
            }
        }
    }
}