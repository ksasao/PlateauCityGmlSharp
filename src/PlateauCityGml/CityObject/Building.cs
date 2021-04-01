using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateauCityGml
{
    public class Building
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public Surface[] Surfaces { get; set; }
        public Position LowerCorner { get; set; }
        public Position UpperCorner { get; set; }
    }
}
