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
                string status = b.LOD2Solid == null ? "No LOD2 Polygon" : $"{b.LOD2Solid?.Length} polygons";
                Console.WriteLine($"{b.Id}\t{b.Name}\t{status}");
            }

            for (int i = 0; i < buildings.Length; i++)
            {
                try
                {
                    Building building = buildings[i];
                    if (building.LOD2Solid == null && building.LOD1Solid != null)
                    {
                        building.LOD2Solid = building.LOD1Solid;
                    }
                    if (building.LOD2Solid != null)
                    {
                        //building.LOD2Solid = building.LOD1Solid;
                        ModelGenerator mg = new ModelGenerator(building);
                        mg.SaveAsObj(Path.Combine(outputPath, building.Id + ".obj"));
                    }
                }
                catch
                {

                }

            }
            Console.WriteLine($"{stopwatch.ElapsedMilliseconds} ms");

        }
    }
}
