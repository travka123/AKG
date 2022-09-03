using Rendering;
using System.Numerics;
using Xunit;

namespace AKG.ObjReader.Testing
{
    public class BuildFlatTests
    {
        struct TestLayout1
        {
            public Vector4 position;
        }

        [Fact]
        public void Triangle()
        {
            using (var ms = new MemoryStream())
            {
                var sw = new StreamWriter(ms);

                sw.WriteLine("g triangle");
                sw.WriteLine("v 0.0 0.0 0.0");
                sw.WriteLine("v 0.0 1.0 0.0");
                sw.WriteLine("v 1.0 0.0 0.0");
                sw.WriteLine("f 1 2 3");
                sw.Flush();

                ms.Seek(0, SeekOrigin.Begin);

                TestLayout1[] data = ObjFileParser.Parse(ms).BuildFlat<TestLayout1>();

                Assert.Equal(new Vector4(0.0f, 0.0f, 0.0f, 1.0f), data[0].position);
                Assert.Equal(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), data[1].position);
                Assert.Equal(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), data[2].position);
            }
        }

        [Fact]
        public void Square()
        {
            using (var ms = new MemoryStream())
            {
                var sw = new StreamWriter(ms);

                sw.WriteLine("g square");
                sw.WriteLine("v 0.0 0.0 0.0");
                sw.WriteLine("v 0.0 1.0 0.0");
                sw.WriteLine("v 1.0 0.0 0.0");
                sw.WriteLine("v 1.0 1.0 0.0");
                sw.WriteLine("f 1 2 3 4");
                sw.Flush();

                ms.Seek(0, SeekOrigin.Begin);

                TestLayout1[] data = ObjFileParser.Parse(ms).BuildFlat<TestLayout1>();

                Assert.Equal(new Vector4(0.0f, 0.0f, 0.0f, 1.0f), data[0].position);
                Assert.Equal(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), data[1].position);
                Assert.Equal(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), data[2].position);

                Assert.Equal(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), data[3].position);
                Assert.Equal(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), data[4].position);
                Assert.Equal(new Vector4(1.0f, 1.0f, 0.0f, 1.0f), data[5].position);
            }
        }

        [Fact]
        public void TriangleAndSquare()
        {
            using (var ms = new MemoryStream())
            {
                var sw = new StreamWriter(ms);

                sw.WriteLine("g triangle");
                sw.WriteLine("v 0.0 0.0 0.0");
                sw.WriteLine("v 0.0 1.0 0.0");
                sw.WriteLine("v 1.0 0.0 0.0");
                sw.WriteLine("f 1 2 3");
                sw.WriteLine("v 0.0 0.0 0.0");
                sw.WriteLine("v 0.0 1.0 0.0");
                sw.WriteLine("v 1.0 0.0 0.0");
                sw.WriteLine("v 1.0 1.0 0.0");
                sw.WriteLine("f 4 5 6 7");
                sw.Flush();

                ms.Seek(0, SeekOrigin.Begin);

                TestLayout1[] data = ObjFileParser.Parse(ms).BuildFlat<TestLayout1>();

                Assert.Equal(new Vector4(0.0f, 0.0f, 0.0f, 1.0f), data[0].position);
                Assert.Equal(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), data[1].position);
                Assert.Equal(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), data[2].position);

                Assert.Equal(new Vector4(0.0f, 0.0f, 0.0f, 1.0f), data[3].position);
                Assert.Equal(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), data[4].position);
                Assert.Equal(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), data[5].position);

                Assert.Equal(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), data[6].position);
                Assert.Equal(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), data[7].position);
                Assert.Equal(new Vector4(1.0f, 1.0f, 0.0f, 1.0f), data[8].position);
            }
        }
    }
}