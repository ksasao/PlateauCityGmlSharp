using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateauCityGml
{
    public class Relief
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public Surface[] LOD1Solid { get; set; }
        public Position LowerCorner { get; set; }
        public Position UpperCorner { get; set; }
        public string GmlPath { get; set; }
    }
}
