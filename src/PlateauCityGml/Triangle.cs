using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PlateauCityGml
{
    public class Triangle
    {
        public Vertex P0 { get; set; }
        public Vertex P1 { get; set; }
        public Vertex P2 { get; set; }

        public Vector3 Normal { get
            {
                Vector3 n = Vector3.Cross(P2.Value - P1.Value, P0.Value - P1.Value);
                n = Vector3.Normalize(n);
                return n;
            }
        }

        public bool HasTexture { get; set; } = false;
    }
}
