using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PlateauCityGml
{
    public class ModelGenerator
    {
        public Triangle[] Triangles { get; private set; }
        public Vector2[] UV { get; private set; }
        public Vector3[] Vertices { get; private set; }
        public string TextureFile { get; private set; }

        private List<string> model = new List<string>();

        public ModelGenerator(Building building)
        {
            List<Triangle> tris = new List<Triangle>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vertex> vtx = new List<Vertex>();
            Position origin = building.LowerCorner;

            int count = 0;
            int offset = 0;
            string textureFile = null;
            for (int i = 0; i < building.Surfaces.Length; i++, count++)
            {
                if (building.Surfaces[i].Positions != null && building.Surfaces[i].UVs != null)
                {
                    Triangulator tr = new Triangulator();
                    (Vertex[] vertex, Triangle[] triangle) = tr.Convert(building.Surfaces[i].Positions, offset, origin);
                    vtx.AddRange(vertex);
                    tris.AddRange(triangle);
                    offset += vertex.Length;

                    uvs.AddRange(building.Surfaces[i].UVs);
                    if(building.Surfaces[i].TextureFile != null)
                    {
                        textureFile = building.Surfaces[i].TextureFile;
                    }
                }
            }
            Triangles = tris.ToArray();
            UV = uvs.ToArray();
            TextureFile = textureFile;
        }
        public ModelGenerator(Triangle[] triangles, Vector2[] uv, string textureFile)
        {
            Triangles = triangles;
            UV = uv;
            TextureFile = textureFile;
        }
        public void SaveAsObj(string filename)
        {
            model.Clear();
            string mtlName = Path.GetFileNameWithoutExtension(filename) + ".mtl";
            model.Add($"mtllib {Path.GetFileName(mtlName)}");
            model.Add("g model");

            // 頂点リストを生成
            Dictionary<int, Vector3> vertList = new Dictionary<int, Vector3>();
            for(int i=0; i<Triangles.Length; i++)
            {
                if (!vertList.ContainsKey(Triangles[i].P0.Index))
                {
                    vertList.Add(Triangles[i].P0.Index, Triangles[i].P0.Value);
                }
                if (!vertList.ContainsKey(Triangles[i].P1.Index))
                {
                    vertList.Add(Triangles[i].P1.Index, Triangles[i].P1.Value);
                }
                if (!vertList.ContainsKey(Triangles[i].P2.Index))
                {
                    vertList.Add(Triangles[i].P2.Index, Triangles[i].P2.Value);
                }
            }
            var len = vertList.Max(c => c.Key) + 1;
            Vector3[] vlist = new Vector3[len];
            for(int i=0; i < len; i++)
            {
                if (vertList.ContainsKey(i))
                {
                    var v = vertList[i];
                    vlist[i] = new Vector3(v.X, v.Y, v.Z);
                }
                else
                {
                    vlist[i] = new Vector3();
                }
                model.Add($"v {vlist[i].X} {vlist[i].Y} {vlist[i].Z}");
            }
            // UV を生成
            for(int i=0; i<UV.Length; i++)
            {
                model.Add($"vt {UV[i].X} {UV[i].Y}");
            }

            // 法線ベクトルを生成
            for(int i=0; i < Triangles.Length; i++)
            {
                Triangle t = Triangles[i];
                Vector3 n = Vector3.Cross(t.P2.Value - t.P1.Value, t.P0.Value - t.P1.Value);
                n = Vector3.Normalize(n);
                model.Add($"vn {n.X} {n.Y} {n.Z}");
            }
            // 面を生成(順序を要確認)
            model.Add("usemtl Material");
            for(int i=0; i < Triangles.Length; i++)
            {
                Triangle t = Triangles[i];
                model.Add($"f {t.P0.Index + 1}/{t.P0.Index + 1}/{i + 1} "
                    + $"{t.P1.Index + 1}/{t.P1.Index + 1}/{i + 1} "
                    + $"{t.P2.Index + 1}/{t.P2.Index + 1}/{i + 1} ");
                //            model.Add($"f {t.P2.Index + 1}/{t.P2.Index + 1} "
                //+ $"{t.P1.Index + 1}/{t.P1.Index + 1} "
                //+ $"{t.P0.Index + 1}/{t.P0.Index + 1}");
            }

            File.WriteAllLines(filename, model.ToArray());

            model.Clear();
            model.Add("newmtl Material");
            model.Add("Ka 0.000000 0.000000 0.000000");
            model.Add("Kd 0.803922 0.803922 0.803922");
            model.Add("Ks 0.000000 0.000000 0.000000");
            model.Add("Ns 2.000000");
            model.Add("d 1.000000");
            model.Add("Tr 0.000000");
            model.Add("Pr 0.333333");
            model.Add("Pm 0.080000");
            model.Add($"map_Kd {TextureFile}");
            File.WriteAllLines(mtlName, model.ToArray());
        }
    }
}
