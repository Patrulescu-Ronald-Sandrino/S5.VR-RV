using System;
using System.IO;
using System.Threading.Tasks;

namespace rt
{
    public class Program
    {
        // {0=x, 1=y, 2=z, 3=file.dat, 4=color mapper, 5=x0, 6=y0, 7=z0, 8=x1, 9=y1, 10=z1, 11=dist, 12=name}    
        private static readonly object[] HeadInput = { 181, 217, 181, "data/head-181x217x181.dat", ReadColorMappingsFile("data/head-color-mappings.txt"), 0f, 0f, 0f, 160f, 217f, 181f, 200.0, "head" };
        private static readonly object[] VertebraInput = { 47, 512, 512,  "data/vertebra-47x512x512.dat", ReadColorMappingsFile("data/vertebra-color-mappings.txt"), 0f, 120f, 150f, 47f, 248f, 362f, 150.0, "vertebra" };

        private static readonly object[][] Input = { HeadInput, VertebraInput };

        private static void Run(object[] input)
        {
            #region cleanup

            var frames = "frames" + $"/{input[12]}";
            if (Directory.Exists(frames))
            {
                var d = new DirectoryInfo(frames);
                foreach (var file in d.EnumerateFiles("*.png")) {
                    file.Delete();
                }
            }
            Directory.CreateDirectory(frames);

            #endregion

            // Scene
            
            #region lights

            var lights = new[]
            {
                new Light(
                    new Vector(-50.0, 0.0, 0.0),
                    new Color(0.8, 0.8, 0.8, 1.0),
                    new Color(0.8, 0.8, 0.8, 1.0),
                    new Color(0.8, 0.8, 0.8, 1.0),
                    1.0
                ),
                new Light(
                    new Vector(20.0, 20.0, 0.0),
                    new Color(0.8, 0.8, 0.8, 1.0),
                    new Color(0.8, 0.8, 0.8, 1.0),
                    new Color(0.8, 0.8, 0.8, 1.0),
                    1.0
                ),
                new Light(
                    new Vector(0.0, 0.0, 300.0),
                    new Color(0.8, 0.8, 0.8, 1.0),
                    new Color(0.8, 0.8, 0.8, 1.0),
                    new Color(0.8, 0.8, 0.8, 1.0),
                    1.0
                )
            };

            #endregion
            
            var matrix = Matrix.Create((int)input[0], (int)input[1], (int)input[2], input[3] as string);
            var colorMapper = input[4] as Func<int, Color>;
            var rt = new RayTracer(matrix, colorMapper, lights);

            const int width = 800;
            const int height = 600;

            // Go around an approximate middle of the scene and generate frames
            var middle = new Vector(((float)input[5] + (float)input[8])/2, ((float)input[6] + (float)input[9])/2, ((float)input[7] + (float)input[10])/2);
            var up = new Vector(0.01, -1.01, 0.01).Normalize();
            var first = (middle ^ up).Normalize();
            var dist = (double)input[11];
            const int n = 90;
            const double step = 360.0 / n;

            var tasks = new Task[n];
            for (var i = 0; i < n; i++)
            {
                var ind = new[]{i};
                tasks[i] = Task.Run(() =>
                {
                    var k = ind[0];
                    var a = (step * k) * Math.PI / 180.0;
                    var ca =  Math.Cos(a);
                    var sa =  Math.Sin(a);

                    var dir = first * ca + (up ^ first) * sa + up * (up * first) * (1.0 - ca);
    
                    var camera = new Camera(
                        middle + dir * dist,
                        dir * -1.0,
                        up,
                        65.0,
                        160.0,
                        120.0,
                        0.0,
                        1000.0
                    );

                    var filename = frames+"/" + $"{input[12]}-{k + 1:000}" + ".png";

                    rt.Render(camera, width, height, filename);
                    Console.WriteLine($"{input[12]}: Frame {k+1}/{n} completed");
                });
            }
            Task.WaitAll(tasks);
        }

        public static void Main(string[] args)
        {
            foreach (var input in Input)
            {
                Run(input);
            }
        }

        private static Func<int, Color> ReadColorMappingsFile(string path) {
            var lines = File.ReadAllLines(path);
            var stringToFloat = new Func<string, float>(float.Parse);
            var stringToColor = new Func<string, Color>(line => new Color(double.Parse(line.Split(' ')[0]), double.Parse(line.Split(' ')[1]), double.Parse(line.Split(' ')[2]), double.Parse(line.Split(' ')[3])));
            var colorMappings = new Color[int.Parse(lines[^1])];
            for (var i = 0; i < lines.Length - 1; i+=2)
            {
                var intervalStartIncl = int.Parse(lines[i]);
                var color = stringToColor(lines[i + 1]);
                var intervalEndExcl = int.Parse(lines[i + 2]);
                for (var j = intervalStartIncl; j < intervalEndExcl; j++)
                {
                    colorMappings[j] = color;
                }
            }
            return i => colorMappings[i];
        }
    }
}