using PlateauCityGml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CityGMLTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Drag & Drop a .gml file.");
            }
            else
            {
                try
                {
                    Assembly exePath = Assembly.GetEntryAssembly();
                    string path = Path.Combine(Path.GetDirectoryName(exePath.Location), "output");
                    CreateModel(args[0], path);
                }catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            Console.WriteLine("Press any key.");
            Console.ReadKey();
        }

        static void CreateModel(string path,string outputPath)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var parser = new CityGMLParser();
            var buildings = parser.GetBuildings(path);

            for (int i = 0; i < buildings.Length; i++)
            {
                var b = buildings[i];
                string status = b.Surfaces == null ? "No LOD2 Polygon" : $"{b.Surfaces?.Length} polygons";
                Console.WriteLine($"{b.Id}\t{b.Name}\t{status}");
            }

            for (int i = 0; i < buildings.Length; i++)
            {
                Building building = buildings[i];
                if (building.Surfaces != null)
                {
                    ModelGenerator mg = new ModelGenerator(building);
                    mg.SaveAsObj(Path.Combine(outputPath, building.Id + ".obj"));
                }
            }
            Console.WriteLine($"{stopwatch.ElapsedMilliseconds} ms");

        }
    }
}
