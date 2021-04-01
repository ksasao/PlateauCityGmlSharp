using PlateauCityGml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CityGMLTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();


            string path = @"53393653_bldg_6697_op2.gml";
            var parser = new CityGMLParser();
            var buildings = parser.GetBuildings(path);

            for(int i=0; i < buildings.Length; i++)
            {
              string status = buildings[i].Surfaces==null ? "No LOD2 Polygon" : $"{buildings[i].Surfaces?.Length} polygons";
            }

            Building building = buildings[12];
            ModelGenerator mg = new ModelGenerator(building);
            mg.SaveAsObj(building.Id + ".obj");
            Console.WriteLine($"{stopwatch.ElapsedMilliseconds} ms");

        }
    }
}
