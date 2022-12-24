using System;
using System.Text;

namespace rt
{
    class RayTracer
    {
        private const double Frequency = 1;
        
        private readonly Matrix _matrix;
        private readonly Func<int, Color> _colorMapper;
        private readonly Light[] _lights;

        private readonly StringBuilder _outputSb = new StringBuilder();

        public RayTracer(Matrix matrix, Func<int, Color> colorMapper, Light[] lights)
        {
            _matrix = matrix;
            _colorMapper = colorMapper;
            _lights = lights;
        }

        private static double ImageToViewPlane(int n, int imgSize, double viewPlaneSize)
        {
            var u = n * viewPlaneSize / imgSize;
            u -= viewPlaneSize / 2;
            return u;
        }

//         private Intersection FindFirstIntersection(Line ray, double minDist, double maxDist)
//         {
//             var intersection = new Intersection {Valid = false};
//
//             /*foreach (var geometry in geometries)
//             {
//                 var intr = geometry.GetIntersection(ray, minDist, maxDist);
//
//                 if (!intr.Valid || !intr.Visible) continue;
//
//                 if (!intersection.Valid || !intersection.Visible)
//                 {
//                     intersection = intr;
//                 }
//                 else if (intr.T < intersection.T)
//                 {
//                     intersection = intr;
//                 }
//             }*/
//
//             return intersection;
//         }

        // private bool IsLit(Vector point, Light light)
        // {
        //     // ADD CODE HERE: Detect whether the given point has a clear line of sight to the given light
        //     var intersection = FindFirstIntersection(new Line(light.Position, point), 0.0, (light.Position - point).Length() - 0.01);
        //     
        //     return !intersection.Visible;
        // }

        // aabb vs ray collision/intersection
        // source: https://link.springer.com/content/pdf/10.1007/978-1-4842-7185-8.pdf (Ray Tracing Gems II) pag 556
        // https://gdbooks.gitbooks.io/3dcollisions/content/Chapter3/raycast_aabb.html
        private double FindFirstIntersection(Line cameraRay, double minDist, double maxDist)
        {
            // bounding box 
            var vMin = new Vector(0, 0, 0);
            var vMax = new Vector(_matrix.length, _matrix.width, _matrix.height);

            var tMinX = (vMin.X - cameraRay.X0.X) / cameraRay.Dx.X;
            var tMaxX = (vMax.X - cameraRay.X0.X) / cameraRay.Dx.X;
            var tMinY = (vMin.Y - cameraRay.X0.Y) / cameraRay.Dx.Y;
            var tMaxY = (vMax.Y - cameraRay.X0.Y) / cameraRay.Dx.Y;
            var tMinZ = (vMin.Z - cameraRay.X0.Z) / cameraRay.Dx.Z;
            var tMaxZ = (vMax.Z - cameraRay.X0.Z) / cameraRay.Dx.Z;
            
            var tMin = Math.Max(Math.Max(Math.Min(tMinX, tMaxX), Math.Min(tMinY, tMaxY)), Math.Min(tMinZ, tMaxZ));
            var tMax = Math.Min(Math.Min(Math.Max(tMinX, tMaxX), Math.Max(tMinY, tMaxY)), Math.Max(tMinZ, tMaxZ));

            if (tMax.LessThan(0) || tMin.GreaterThan(tMax))
            {
                return -1;
            }
            
            var t = tMin.LessThan(0) ? tMax : tMin;
            
            if (t.LessThan(minDist) || t.GreaterThan(maxDist))
            {
                return -1;
            }

            return t;
        }
        
        private Color GetMatrixPointColor(Vector point)
        {
            var x = (int) Math.Floor(point.X);
            var y = (int) Math.Floor(point.Y);
            var z = (int) Math.Floor(point.Z);
            // Console.WriteLine($"x: {x}, y: {y}, z: {z}");
            
            if (x < 0 || x >= _matrix.length || y < 0 || y >= _matrix.width || z < 0 || z >= _matrix.height)
            {
                return new Color();
            }

            return _colorMapper(_matrix.data[x, y, z]);
        }

        private Color FindColor(int i, int j, Line cameraRay, double minDist, double maxDist)
        {
            var t = FindFirstIntersection(cameraRay, minDist, maxDist);
            if (t.Equals(-1)) return new Color();
            _outputSb.Append($"");
            
            var intersectionPoint = cameraRay.X0 + cameraRay.Dx * t;
            var color = GetMatrixPointColor(intersectionPoint);
            var passedThroughWhile = 0;

            // debug print
            if (false && color.Alpha.GreaterThanOrEquals(0.75))
            {
                Console.WriteLine($"" +
                                  $"i: {i}, " +
                                  $"j: {j}, " +
                                  $"x: {intersectionPoint.X}, " +
                                  $"y: {intersectionPoint.Y}, " +
                                  $"z: {intersectionPoint.Z}, " +
                                  $"t: {t}, " +
                                  $"color: {color}, " +
                                  $"passedThroughWhile: {passedThroughWhile}");
            }

            while (color.Alpha.LessThanOrEquals(1) && color.Alpha.GreaterThanOrEquals(0))
            {
                passedThroughWhile++;
                if (i == 400 && j == 300)
                {
                    Console.Write("");
                }
                
                t = FindFirstIntersection(cameraRay, t + Frequency - 0.001, maxDist);
                if (t.Equals(-1))
                {
                    return color;
                }
                Console.WriteLine("here");
                
                if (passedThroughWhile > 0)
                    Console.WriteLine($"pixel i = {i}, j = {j} passedThroughWhile: {passedThroughWhile}");
                intersectionPoint = cameraRay.X0 + cameraRay.Dx * t;
                var newColor = GetMatrixPointColor(intersectionPoint);
                color = color.Blend(newColor);
            }

            
            return color;
        }

        // frames at /mnt/e/UBB_IE_2020-2023/S5.VR/volume-renderer-Patrulescu-Ronald-Sandrino/bin/Debug/netcoreapp3.1/frames/
        public void Render(Camera camera, int width, int height, string filename)
        {
            var image = new Image(width, height);

            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    var x1 = // the ray from the origin through the pixel
                        camera.Position + camera.Direction * camera.ViewPlaneDistance 
                                        + camera.Up * ImageToViewPlane(j, height, camera.ViewPlaneHeight)
                                        + (camera.Up ^ camera.Direction) * ImageToViewPlane(i, width, camera.ViewPlaneWidth);
                    var cameraRay = new Line(camera.Position, x1); // the line from the camera through the pixel

                    #region ADD CODE HERE: Implement pixel color calculation

                    image.SetPixel(i, j, FindColor(i, j, cameraRay, camera.FrontPlaneDistance, camera.BackPlaneDistance));
                    
                    #endregion
                }
            }

            image.Store(filename);
        }
    
        public string GetOutput()
        {
            return _outputSb.ToString();
        }
    }
}