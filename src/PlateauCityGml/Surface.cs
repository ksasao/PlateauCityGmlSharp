using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PlateauCityGml
{
    public class Surface
    {
        public string Id { get; set; }
        public Position[] Positions { get; private set; }
        public Vector2[] UVs { get; set; }
        public Position LowerCorner { get; private set; }
        public Position UpperCorner { get; private set ; }
        public string TextureFile { get; set; }

        public Surface()
        {
        }
        public void SetPositions(Position[] positions)
        {
            Positions = positions;
            LowerCorner = Position.GetLowerCorner(Positions);
            UpperCorner = Position.GetUpperCorner(Positions);
        }
    }
}
