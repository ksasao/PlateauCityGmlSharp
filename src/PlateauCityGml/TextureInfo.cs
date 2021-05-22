using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PlateauCityGml
{
    public class TextureInfo
    {
        public List<string> Files;
        public Dictionary<string, (int index, Vector2[] uv)> Map { get; set; }
    }
}
