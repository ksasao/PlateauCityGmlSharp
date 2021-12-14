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
    public enum CityObjectType { Undefined, Building, Relief };
    public class CityGMLParser
    {
        enum State { None, Name, 建物ID, SurfaceMember };

        public Position LowerCorner { get; set; } =  new Position { Latitude = -100, Longitude = -200};
        public Position UpperCorner { get; set; } = new Position { Latitude = 100, Longitude = 200 };

        public CityGMLParser()
        {

        }

        public CityGMLParser(Position lower, Position upper)
        {
            LowerCorner = lower;
            UpperCorner = upper;
        }

        public CityObjectType GetCityObjectType(string gmlPath)
        {
            string fullPath = Path.GetFullPath(gmlPath);
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            const string bldgBuilding = "bldg:Building";
            const string demReliefFeature = "dem:ReliefFeature";

            CityObjectType coType = CityObjectType.Undefined;

            using (var fileStream = File.OpenText(gmlPath))
            using (XmlReader reader = XmlReader.Create(fileStream, settings))
            {
                while (reader.Read() && coType == CityObjectType.Undefined)
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name == bldgBuilding)
                            {
                                coType = CityObjectType.Building;
                            }
                            else if(reader.Name == demReliefFeature)
                            {
                                coType = CityObjectType.Relief;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            return coType;
        }

        public Relief GetRelief(string gmlPath)
        {
            const string trianglePatches = "gml:trianglePatches";
            string fullPath = Path.GetFullPath(gmlPath);

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;

            Relief relief = null;

            using (var fileStream = File.OpenText(gmlPath))
            using (XmlReader reader = XmlReader.Create(fileStream, settings))
            {
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name == trianglePatches)
                            {
                                try
                                {
                                    relief = CreateRelief(reader);
                                    if(relief != null)
                                    {
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message); // Parse error
                                }
                            }

                            break;
                        default:
                            break;
                    }
                }

            }
            return relief;
        }

        public Building[] GetBuildings(string gmlPath)
        {
            const string bldgBuilding = "bldg:Building";
            string fullPath = Path.GetFullPath(gmlPath);
            List<Building> buildings = new List<Building>();

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            TextureInfo textures = null;

            using (var fileStream = File.OpenText(gmlPath))
            using (XmlReader reader = XmlReader.Create(fileStream, settings))
            {
                Building building = null;
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name == bldgBuilding)
                            {
                                try
                                {
                                    building = CreateBuilding(reader);
                                    if(building.LOD1Solid != null || building.LOD2Solid != null)
                                    {
                                        building.GmlPath = fullPath;
                                        buildings.Add(building);
                                    }
                                }
                                catch(Exception ex)
                                {
                                    Console.WriteLine(ex.Message); // Parse error
                                }
                            }

                            if (reader.Name == "app:appearanceMember")
                            {
                                textures = ParseTextureInfo(reader, buildings);
                            }
                            break;
                        default:
                            break;
                    }
                }
                
            }
            if(textures != null)
            {
                // UVデータをビルデータにマージ
                MargeData(buildings, textures);
            }
            return buildings.ToArray();
        }


        public Relief CreateRelief(XmlReader reader)
        {
            Relief building = new Relief();

            XmlDocument doc = new XmlDocument();
            var r2 = reader.ReadSubtree();
            XmlNode cd = doc.ReadNode(r2);
            XmlNodeList member = cd.ChildNodes;
            List<Surface> surfaces = new List<Surface>();

            foreach (XmlNode node in member)
            {
                if (node.Name == "gml:Triangle")
                {
                    Surface s = new Surface();
                    string posStr = node.InnerText;
                    s.SetPositions(Position.ParseString(posStr));
                    if(LowerCorner.Latitude < s.LowerCorner.Latitude && LowerCorner.Longitude < s.LowerCorner.Longitude
                        && s.UpperCorner.Latitude < UpperCorner.Latitude && s.UpperCorner.Longitude < UpperCorner.Longitude)
                    {
                        // 法線を反転する
                        Position p = s.Positions[2];
                        s.Positions[2] = s.Positions[1];
                        s.Positions[1] = p;
                        surfaces.Add(s);
                    }
                }

            }
            building.LOD1Solid = surfaces.ToArray();

            if (building.LOD1Solid != null)
            {
                (Position lower, Position upper) = GetCorner(building.LOD1Solid);
                building.LowerCorner = lower;
                building.UpperCorner = upper;
            }

            return building;
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
                if (node.Name == "bldg:measuredHeight")
                {
                    building.Height = Convert.ToSingle(node.FirstChild.Value);
                }
                if(node.Name== "bldg:lod0RoofEdge")
                {
                    building.LOD0RoofEdge = GetLOD0Surface(node);
                }
                if (node.Name == "bldg:lod1Solid")
                {
                    building.LOD1Solid = GetLOD1Surface(node);
                }
                if (node.Name== "bldg:lod2Solid")
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
                List<Surface> sList = new List<Surface>();
                foreach (var d in surfaceDic.Keys)
                {
                    var s = surfaceDic[d];
                    if (s != null && s.LowerCorner != Position.None)
                    {
                        sList.Add(s);
                    }
                }
                building.LOD2Solid = sList.ToArray();
                (Position lower, Position upper) = GetCorner(building.LOD2Solid);

                // モデルの min側の角が領域内に入っていれば採用
                if ( LowerCorner.Latitude < lower.Latitude && upper.Latitude < UpperCorner.Latitude
                    && LowerCorner.Longitude < lower.Longitude && upper.Longitude < UpperCorner.Longitude)
                {
                    building.LowerCorner = lower;
                    building.UpperCorner = upper;
                }
                else
                {
                    building.LOD2Solid = new Surface[] { };
                }
            }
            if(building.LOD1Solid != null && building.LOD2Solid == null)
            {
                (Position lower, Position upper) = GetCorner(building.LOD1Solid);
                building.LowerCorner = lower;
                building.UpperCorner = upper;
            }

            return building;
        }
        private (Position Lower, Position Upper) GetCorner(Surface[] surfaces)
        {
            double lLat = double.MaxValue;
            double lLon = double.MaxValue;
            double lAlt = double.MaxValue;
            double uLat = double.MinValue;
            double uLon = double.MinValue;
            double uAlt = double.MinValue;
            foreach (var s in surfaces)
            {
                if(s.LowerCorner != null)
                {
                    if (s.LowerCorner.Latitude < lLat) lLat = s.LowerCorner.Latitude;
                    if (s.LowerCorner.Longitude < lLon) lLon = s.LowerCorner.Longitude;
                    if (s.LowerCorner.Altitude < lAlt) lAlt = s.LowerCorner.Altitude;
                    if (s.UpperCorner.Latitude > uLat) uLat = s.UpperCorner.Latitude;
                    if (s.UpperCorner.Longitude > uLon) uLon = s.UpperCorner.Longitude;
                    if (s.UpperCorner.Altitude > uAlt) uAlt = s.UpperCorner.Altitude;
                }
            }
            return (new Position(lLat, lLon, lAlt), new Position(uLat, uLon, uAlt));
        }
        public Surface GetLOD0Surface(XmlNode node)
        {
            var s = node.FirstChild.FirstChild.FirstChild.FirstChild.FirstChild.FirstChild.FirstChild;
            Surface surface = new Surface();
            string posStr = s.Value;
            surface.SetPositions(Position.ParseString(posStr));
            return surface;
        }

        public Surface[] GetLOD1Surface(XmlNode node)
        {
            List<Surface> surfaces = new List<Surface>();
            // 多角形の名前のリストを取得
            XmlNodeList list = node.FirstChild.FirstChild.FirstChild.ChildNodes;
            for (int i = 0; i < list.Count; i++)
            {
                Surface s = new Surface();
                string posStr = list[i].FirstChild.FirstChild.FirstChild.FirstChild.FirstChild.Value;

                s.SetPositions(Position.ParseString(posStr));
                if (LowerCorner.Latitude < s.LowerCorner.Latitude && LowerCorner.Longitude < s.LowerCorner.Longitude
                    && s.UpperCorner.Latitude < UpperCorner.Latitude && s.UpperCorner.Longitude < UpperCorner.Longitude)
                {
                    surfaces.Add(s);
                }
            }
            return surfaces.ToArray();
        }

        private void UpdateSurfaceDic(XmlNode node, Dictionary<string, Surface> polyDic)
        {
            // 名前に対応する頂点リストを取得する
            XmlNode n = node.FirstChild.FirstChild.FirstChild?.FirstChild?.FirstChild;
            string xml = node.InnerXml.Replace("gml:","");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNodeList s = doc.SelectNodes("//surfaceMember");
            foreach (XmlNode member in s)
            {
                var m2 = member.FirstChild;
                string name = m2.Attributes["id"].Value;
                XmlNode p = m2.FirstChild.FirstChild.FirstChild.FirstChild;
                Position[] positions = Position.ParseString(p.Value);
                polyDic[name].SetPositions(positions);
            }
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

        private TextureInfo ParseTextureInfo(XmlReader reader, List<Building> buildings)
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
            return new TextureInfo { Files = textureFiles, Map = map };


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
        private void MargeData(List<Building> buildings, TextureInfo textureInfo)
        {
            var map = textureInfo.Map;
            var textureFiles = textureInfo.Files;
            foreach(var b in buildings)
            {
                if(b.LOD2Solid == null)
                {
                    continue;
                }
                var data = new List<(int Index, Vector2[] UV)>();
                for (int i=0; i<b.LOD2Solid.Length; i++)
                {
                    // あるビルのポリゴンのIDに一致するテクスチャがあったら割り当てる
                    if (map.ContainsKey(b.LOD2Solid[i].Id))
                    {
                        var d = map[b.LOD2Solid[i].Id];
                        b.LOD2Solid[i].UVs = d.uv;
                        data.Add(d);
                    }
                }
                //bool singleTexture = true;
                //for(int i=1; i<data.Count; i++)
                //{
                //    if(data[0].Index != data[i].Index)
                //    {
                //        singleTexture = false;
                //        break;
                //    }
                //}

                // テクスチャファイルが複数指定されている場合、実態としては同じ画像なので
                // 最初のテクスチャファイルを割り当てる
                // * singleTexture チェックを無視

                for (int i = 0; i < data.Count; i++)
                {
                    string key = b.LOD2Solid[i].Id;
                    if (map.ContainsKey(key))
                    {
                        b.LOD2Solid[i].TextureFile = textureFiles[data[0].Index];
                    }
                }
            }
        }
    }
}
