using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PlateauCityGml
{
    public class CityGMLParser
    {
        enum State { None, Name, 建物ID, SurfaceMember };
        public Building[] GetBuildings(string gmlPath)
        {
            const string bldgBuilding = "bldg:Building";
            List<Building> buildings = new List<Building>();

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;

            using (var fileStream = File.OpenText(gmlPath))
            using (XmlReader reader = XmlReader.Create(fileStream, settings))
            {
                Building building = null;
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if(reader.Name == bldgBuilding)
                            {
                                building = CreateBuilding(reader);
                                buildings.Add(building);
                            }
                            if(reader.Name == "app:appearanceMember")
                            {
                                AddTexture(reader, buildings);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            return buildings.ToArray();
        }

        public Building CreateBuilding(XmlReader reader)
        {
            Building building = new Building();

            XmlDocument doc = new XmlDocument();
            var r2 = reader.ReadSubtree();
            XmlNode cd = doc.ReadNode(r2);
            XmlNodeList member = cd.ChildNodes;
            Dictionary<string, Surface> surfaceDic = null;

            foreach (XmlNode node in member)
            {
                if (node.Attributes["name"]?.Value == "建物ID")
                {
                    string id = node.InnerText;
                    building.Id = id;
                }
                if (node.Name == "gml:name")
                {
                    building.Name = node.FirstChild.Value;
                }
                if(node.Name== "bldg:lod2Solid")
                {
                    surfaceDic = GetPolyList(node);
                }
                if(node.Name== "bldg:boundedBy")
                {
                    UpdateSurfaceDic(node,surfaceDic);
                }
            }
            // LOD2が指定されていない場合は null
            if(surfaceDic != null)
            {
                building.Surfaces = surfaceDic.Values.ToArray();
                double lLat = double.MaxValue;
                double lLon = double.MaxValue;
                double lAlt = double.MaxValue;
                double uLat = double.MinValue;
                double uLon = double.MinValue;
                double uAlt = double.MinValue;
                foreach (var s in building.Surfaces)
                {
                    if (s.LowerCorner.Latitude  < lLat) lLat = s.LowerCorner.Latitude;
                    if (s.LowerCorner.Longitude < lLon) lLon = s.LowerCorner.Longitude;
                    if (s.LowerCorner.Altitude  < lAlt) lAlt = s.LowerCorner.Altitude;
                    if (s.UpperCorner.Latitude  > uLat) uLat = s.UpperCorner.Latitude;
                    if (s.UpperCorner.Longitude > uLon) uLon = s.UpperCorner.Longitude;
                    if (s.UpperCorner.Altitude  > uAlt) uAlt = s.UpperCorner.Altitude;
                }
                building.LowerCorner = new Position(lLat, lLon, lAlt);
                building.UpperCorner = new Position(uLat, uLon, uAlt);
            }

            return building;
        }
        private void UpdateSurfaceDic(XmlNode node, Dictionary<string, Surface> polyDic)
        {
            // 名前に対応する頂点リストを取得する
            XmlNode n = node.FirstChild.FirstChild.FirstChild.FirstChild.FirstChild;
            string name = n.Attributes["gml:id"].Value;
            XmlNode p = n.FirstChild.FirstChild.FirstChild.FirstChild;
            Position[] positions = Position.ParseString(p.Value);
            polyDic[name].SetPositions(positions);
        }
        public Dictionary<string, Surface> GetPolyList(XmlNode node)
        {
            Dictionary<string, Surface> dic = new Dictionary<string, Surface>();

            // 多角形の名前のリストを取得
            XmlNodeList list = node.FirstChild.FirstChild.FirstChild.ChildNodes;
            for (int i = 0; i < list.Count; i++)
            {
                string name = list[i].Attributes["xlink:href"].Value.Substring(1);
                dic.Add(name, new Surface{Id = name });
            }
            return dic;
        }

        private void AddTexture(XmlReader reader, List<Building> buildings)
        {
            var map = new Dictionary<string, (int index, Vector2[] uv)>();
            XmlDocument doc = new XmlDocument();
            var r2 = reader.ReadSubtree();
            XmlNode cd = doc.ReadNode(r2);
            XmlNodeList list = cd.FirstChild.ChildNodes;
            List<string> textureFiles = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Name == "app:surfaceDataMember") // 1枚のテクスチャに紐づくUV
                {
                    XmlNodeList uv = list[i].FirstChild.ChildNodes;
                    for(int j=0; j<uv.Count; j++)
                    {
                        if (uv[j].Name == "app:target")
                        {
                            // UVデータを登録
                            string uri = uv[j].Attributes["uri"].Value.Substring(1);
                            string texString = uv[j].FirstChild.FirstChild.FirstChild.Value;
                            map.Add(uri,(textureFiles.Count-1, ConvertToUV(texString)));
                            continue;
                        }

                        if (uv[j].Name == "app:imageURI")
                        {
                            string file = uv[j].FirstChild.Value;
                            textureFiles.Add(file);
                            continue;
                        }
                    }
                }
            }
            // UVデータをビルデータにマージ
            MargeData(buildings, map, textureFiles);

        }
        private Vector2[] ConvertToUV(string uvText)
        {
            string[] items = uvText.Split(' ');
            int len = items.Length / 2 - 1; // 元の点列は始点と終点が同じ値なので終点を無視
            Vector2[] list = new Vector2[len];
            for (int i = 0; i < len; i++) 
            {
                list[i] = new Vector2
                {
                    X = Convert.ToSingle(items[i * 2]),
                    Y = Convert.ToSingle(items[i * 2 + 1]),
                };
            }
            return list;
        }
        private void MargeData(List<Building> buildings, Dictionary<string, (int Index, Vector2[] UV)> map, List<string> textureFiles)
        {
            foreach(var b in buildings)
            {
                if(b.Surfaces == null)
                {
                    continue;
                }
                var data = new List<(int Index, Vector2[] UV)>();
                for (int i=0; i<b.Surfaces.Length; i++)
                {
                    // あるビルのポリゴンのIDに一致するテクスチャがあったら割り当てる
                    if (map.ContainsKey(b.Surfaces[i].Id))
                    {
                        var d = map[b.Surfaces[i].Id];
                        b.Surfaces[i].UVs = d.UV;
                        data.Add(d);
                    }
                }
                bool singleTexture = true;
                for(int i=1; i<data.Count; i++)
                {
                    if(data[0].Index != data[i].Index)
                    {
                        singleTexture = false;
                        break;
                    }
                }
                if (singleTexture)
                {
                    for (int i = 0; i < data.Count; i++)
                    {
                        string key = b.Surfaces[i].Id;
                        if (map.ContainsKey(key))
                        {
                            b.Surfaces[i].TextureFile = textureFiles[data[0].Index];
                        }
                    }
                }
                else
                {
                    // not supported
                }
            }
        }
    }
}
