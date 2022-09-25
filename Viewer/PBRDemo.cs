using AKG.Rendering;
using AKG.Viewer.Meshes;
using Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Viewer
{
    public class PBRDemo : Mesh
    {
        public Entity[] _entities;

        public PBRDemo(List<string> models)
        {
            _entities = new Entity[models.Count];

            float cSide = (float)Math.Ceiling(Math.Sqrt(models.Count)); 
            float hSide = (cSide - 1) / 2; 

            for (int i = 0; i < models.Count; i++)
            {
                var entity = new Entity();

                var builder = ObjFileParser.Parse(models[i]);

                entity.SetMesh(new PBR(builder));

                float xOffset = i % cSide;
                float yOffset = (i - xOffset) / cSide;

                entity.SetPosition(4.0f *  new Vector3(xOffset - hSide, hSide - yOffset, 0));

                _entities[i] = entity;
            }
        }

        public void Draw(Canvas canvas, Uniforms uniforms, RenderingOptions options)
        {
            foreach (var entity in _entities)
            {
                entity.Draw(canvas, uniforms, options);
            }
        }

        public int GetVerticesNumber()
        {
            return _entities.Sum(e => e.GetVerticesNumber());
        }
    }
}
