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
        public Vertex[] Vertices { get; private set; }
        public string TextureFile { get; private set; }

        public Position LowerCorner { get; private set; }
        public Position UpperCorner { get; private set; }
        public Position Origin { get; private set; }

        private List<string> model = new List<string>();
        private Building _building;

        public ModelGenerator(Building building, Position origin)
        {
            ModelInitialize(building, origin);

        }
        public ModelGenerator(Building building)
        {
            ModelInitialize(building, building.LowerCorner);
        }
        private void ModelInitialize(Building building, Position origin)
        {
            _building = building;
            List<Triangle> tris = new List<Triangle>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vertex> vtx = new List<Vertex>();
            LowerCorner = building.LowerCorner;
            UpperCorner = building.UpperCorner;

            Origin = origin;
            int count = 0;
            int offset = 0;
            string textureFile = null;
            Surface[] surfaces = building.LOD2Solid;
            if(surfaces == null)
            {
                surfaces = building.LOD1Solid;
            }
            for (int i = 0; i < surfaces.Length; i++, count++)
            {
                if (surfaces[i].Positions != null)
                {
                    Triangulator tr = new Triangulator();
                    (Vertex[] vertex, Triangle[] triangle) = tr.Convert(surfaces[i].Positions, surfaces[i].UVs, offset, Origin);
                    vtx.AddRange(vertex);
                    tris.AddRange(triangle);
                    offset += vertex.Length;
                    if (surfaces[i].UVs != null)
                    {
                        uvs.AddRange(surfaces[i].UVs);
                    }
                    else
                    {
                        for (int j = 0; j < vertex.Length; j++)
                        {
                            uvs.Add(new Vector2(-1, -1));
                        }
                    }
                    if (surfaces[i].TextureFile != null)
                    {
                        textureFile = surfaces[i].TextureFile;
                    }
                }
            }
            Triangles = tris.ToArray();
            Vertices = vtx.ToArray();
            UV = uvs.ToArray();
            string current = Path.GetDirectoryName(building.GmlPath);
            if (textureFile != null)
            {
                TextureFile = Path.Combine(current, textureFile);
            }
        }


        public void SaveAsObj(string filename)
        {
            model.Clear();

            string fullpath = Path.GetFullPath(filename);
            string current = Path.GetDirectoryName(fullpath);
            Directory.CreateDirectory(current);

            string mtlName = Path.GetFileNameWithoutExtension(filename) + ".mtl";
            model.Add($"# Origin: {Origin.Latitude:F7},{Origin.Longitude:F7},{Origin.Altitude}");
            model.Add($"# Lower : {LowerCorner.Latitude:F7},{LowerCorner.Longitude:F7},{LowerCorner.Altitude}");
            model.Add($"# Upper : {UpperCorner.Latitude:F7},{UpperCorner.Longitude:F7},{UpperCorner.Altitude}");
            model.Add($"mtllib {Path.GetFileName(mtlName)}");
            model.Add("g model");

            for(int i=0; i < Vertices.Length; i++)
            {
                var v = Vertices[i].Value;
                model.Add($"v {v.X} {v.Y} {v.Z}");
            }
            // UV を生成
            for(int i=0; i<UV.Length; i++)
            {
                model.Add($"vt {UV[i].X} {UV[i].Y}");
            }

            // 法線ベクトルを生成
            //for(int i=0; i < Triangles.Length; i++)
            //{
            //    Vector3 n = Triangles[i].Normal;
            //    model.Add($"vn {n.X} {n.Y} {n.Z}");
            //}
            // 面を生成(順序を要確認)
            model.Add("usemtl Material");
            for(int i=0; i < Triangles.Length; i++)
            {
                Triangle t = Triangles[i];
                if (t.HasTexture)
                {
                    model.Add($"f {t.P0.Index + 1}/{t.P0.Index + 1} " // /{i + 1}
                        + $"{t.P1.Index + 1}/{t.P1.Index + 1} "
                        + $"{t.P2.Index + 1}/{t.P2.Index + 1}");
                }
                else
                {
                    model.Add($"f {t.P0.Index + 1} "
                        + $"{t.P1.Index + 1} "
                        + $"{t.P2.Index + 1}");
                }
            }

            // .objファイル書き出し
            File.WriteAllLines(fullpath, model.ToArray());

            // .obj ファイルのローダー互換性のためテクスチャを .mtl と同じディレクトリにコピー 
            string textureLocal = "";
            if (TextureFile != null)
            {
                textureLocal = Path.GetFileName(TextureFile);
                File.Copy(TextureFile, Path.Combine(current, textureLocal), true);
            }

            model.Clear();
            model.Add("newmtl Material");
            model.Add("Ka 0.000000 0.000000 0.000000");
            if (textureLocal != "")
            {
                model.Add("Kd 1.0 1.0 1.0");
            }
            else
            {
                model.Add("Kd 0.45 0.5 0.5");
            }

            model.Add("Ks 0.000000 0.000000 0.000000");
            model.Add("Ns 2.000000");
            model.Add("d 1.000000");
            model.Add("Tr 0.000000");
            model.Add("Pr 0.333333");
            model.Add("Pm 0.080000");
            if(textureLocal != "")
            {
                model.Add($"map_Kd {textureLocal}");
            }

            // .mtl ファイル書き出し
            File.WriteAllLines(Path.Combine(current,mtlName), model.ToArray());
        }
    }
}
