﻿using System;
using System.IO;
using System.Threading.Tasks;

namespace rt
{
    public class Program
    {
        private static readonly Func<int, Color> HeadColorMapper = value =>
        {
            if (value.IsInRange(0, 13)) return new Color(0, 0, 0, 0);
            if (value.IsInRange(13, 100)) return new Color(1.0, 1.0, 0, 0.5);
            if (value.IsInRange(100, 200)) return new Color(1.0, 0, 1.0, 0.5);
            if (value.IsInRange(200, 256)) return new Color(0, 1.0, 1.0, 0.5);
                
            return new Color();
        };

        private static readonly Func<int, Color> VertebraColorMapper = value =>
        {
            if (value.IsInRange(0, 13)) return new Color(1.0, 1.0, 1.0, 0);
            if (value.IsInRange(13, 256)) return new Color(1.0, 1.0, 1.0, 1.0);

            return new Color();
        };

        public static void Main(string[] args)
        {
            // Cleanup
            const string frames = "frames";
            if (Directory.Exists(frames))
            {
                var d = new DirectoryInfo(frames);
                foreach (var file in d.EnumerateFiles("*.png")) {
                    file.Delete();
                }
            }
            Directory.CreateDirectory(frames);

            // Scene
            var matrix = Matrix.Create(181, 217, 181, "data/head-181x217x181.dat");
            // var matrix = Matrix.Create(47, 512, 512, "data/vertebra-47x512x512.dat");

            var colorMapper = HeadColorMapper; // HeadColorMapper or VertebraColorMapper

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
            
            var rt = new RayTracer(matrix, colorMapper, lights);

            const int width = 800;
            const int height = 600;

            // Go around an approximate middle of the scene and generate frames
            var middle = new Vector(0.0, 0.0, 100.0);
            var up = new Vector(-Math.Sqrt(0.125), -Math.Sqrt(0.75), Math.Sqrt(0.125)).Normalize();
            var first = (middle ^ up).Normalize();
            const double dist = 150.0;
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

                    var filename = frames+"/" + $"{k + 1:000}" + ".png";

                    rt.Render(camera, width, height, filename);
                    Console.WriteLine($"Frame {k+1}/{n} completed");
                });
            }

            Task.WaitAll(tasks);
        }
    }
}