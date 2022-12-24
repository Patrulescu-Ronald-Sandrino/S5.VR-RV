using System;
using System.Collections.Generic;
using System.IO;

namespace rt
{
    class RayTracer : IDisposable
    {
        private static readonly Color Background = new Color( /*178, 190, 181, 1*/);
        private const double Frequency = 1;

        private readonly Matrix _matrix;
        private readonly Func<int, Color> _colorMapper;
        private readonly Light[] _lights;

        private static int _instance = 1;

        private readonly TextWriter _file = TextWriter.Synchronized(new StreamWriter($"output-{_instance++}.txt", false,
            System.Text.Encoding.ASCII, 8192));


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

        private Vector FindFirstIntersection2(Line ray)
        {
            // Check if the ray intersects with any of the six planes that define the AABB.
            var tMin = double.NegativeInfinity;
            var tMax = double.PositiveInfinity;
            var boundsMin = new Vector(0, 0, 0);
            var boundsMax = new Vector(_matrix.length, _matrix.width, _matrix.height);

            // if (Math.Abs(ray.Dx.X) < double.Epsilon)
            // {
            //     if (ray.X0.X < boundsMin.X || ray.X0.X > boundsMax.X)
            //     {
            //         return null;
            //     }
            // }
            // else
            {
                var invD = 1.0 / ray.Dx.X;
                var t1 = (boundsMin.X - ray.X0.X) * invD;
                var t2 = (boundsMax.X - ray.X0.X) * invD;

                if (t1 > t2)
                {
                    (t1, t2) = (t2, t1);
                }

                tMin = Math.Max(tMin, t1);
                tMax = Math.Min(tMax, t2);

                if (tMin > tMax)
                {
                    return null;
                }
            }

            // if (Math.Abs(ray.Dx.Y) < float.Epsilon)
            // {
            //     if (ray.X0.Y < boundsMin.Y || ray.X0.Y > boundsMax.Y)
            //     {
            //         return null;
            //     }
            // }
            // else
            {
                var invD = 1.0f / ray.Dx.Y;
                var t1 = (boundsMin.Y - ray.X0.Y) * invD;
                var t2 = (boundsMax.Y - ray.X0.Y) * invD;

                if (t1 > t2)
                {
                    (t1, t2) = (t2, t1);
                }

                tMin = Math.Max(tMin, t1);
                tMax = Math.Min(tMax, t2);

                if (tMin > tMax)
                {
                    return null;
                }
            }

            // if (Math.Abs(ray.Dx.Z) < double.Epsilon)
            // {
            //     if (ray.X0.Z < boundsMin.Z || ray.X0.Z > boundsMax.Z)
            //     {
            //         return null;
            //     }
            // }
            // else
            {
                var invD = 1.0f / ray.Dx.Z;
                var t1 = (boundsMin.Z - ray.X0.Z) * invD;
                var t2 = (boundsMax.Z - ray.X0.Z) * invD;

                if (t1 > t2)
                {
                    (t1, t2) = (t2, t1);
                }

                tMin = Math.Max(tMin, t1);
                tMax = Math.Min(tMax, t2);

                if (tMin > tMax)
                {
                    return null;
                }
            }

            // Check if the intersection point is inside the AABB.
            var intersectionPoint = ray.CoordinateToPosition(tMin);
            if (intersectionPoint.X < boundsMin.X || intersectionPoint.X > boundsMax.X ||
                intersectionPoint.Y < boundsMin.Y || intersectionPoint.Y > boundsMax.Y ||
                intersectionPoint.Z < boundsMin.Z || intersectionPoint.Z > boundsMax.Z)
            {
                return null;
            }

            return intersectionPoint;
        }

        private Vector FindFirstIntersection3(Line ray)

        {
            var intersections = new List<Vector>();
            var xm = ray.X0.X - (ray.Dx.X / ray.Dx.Z) * ray.X0.Z;
            var ym = ray.X0.Y - (ray.Dx.Y / ray.Dx.Z) * ray.X0.Z;
            if (xm >= _matrix.subBox[0] && xm <= _matrix.subBox[3] && ym >= _matrix.subBox[1] && ym <= _matrix.subBox[4])
                intersections.Add(new Vector(xm, ym, _matrix.subBox[2]));
            xm = ray.X0.X - (ray.Dx.X / ray.Dx.Y) * ray.X0.Y;
            var zm = ray.X0.Z - (ray.Dx.Z / ray.Dx.Y) * ray.X0.Y;
            if (xm >= _matrix.subBox[0] && xm <= _matrix.subBox[3] && zm >= _matrix.subBox[2] && zm <= _matrix.subBox[5])
                intersections.Add(new Vector(xm, _matrix.subBox[1], zm));
            ym = ray.X0.Y - (ray.Dx.Y / ray.Dx.X) * (ray.X0.X - _matrix.subBox[3]);
            zm = ray.X0.Z - (ray.Dx.Z / ray.Dx.X) * (ray.X0.X - _matrix.subBox[3]);
            if (ym >= _matrix.subBox[1] && ym <= _matrix.subBox[4] && zm >= _matrix.subBox[2] && zm <= _matrix.subBox[5])
                intersections.Add(new Vector(_matrix.subBox[3], ym, zm));
            ym = ray.X0.Y - (ray.Dx.Y / ray.Dx.X) * ray.X0.X;
            zm = ray.X0.Z - (ray.Dx.Z / ray.Dx.X) * ray.X0.X;
            if (ym >= _matrix.subBox[1] && ym <= _matrix.subBox[4] && zm >= _matrix.subBox[2] && zm <= _matrix.subBox[5])
                intersections.Add(new Vector(_matrix.subBox[0], ym, zm));
            xm = ray.X0.X - (ray.Dx.X / ray.Dx.Y) * (ray.X0.Y - _matrix.subBox[4]);
            zm = ray.X0.Z - (ray.Dx.Z / ray.Dx.Y) * (ray.X0.Y - _matrix.subBox[4]);
            if (xm >= _matrix.subBox[0] && xm <= _matrix.subBox[3] && zm >= _matrix.subBox[2] && zm <= _matrix.subBox[5])
                intersections.Add(new Vector(xm, _matrix.subBox[4], zm));
            xm = ray.X0.X - (ray.Dx.X / ray.Dx.Z) * (ray.X0.Z - _matrix.subBox[5]);
            ym = ray.X0.Y - (ray.Dx.Y / ray.Dx.Z) * (ray.X0.Z - _matrix.subBox[5]);
            if (xm >= _matrix.subBox[0] && xm <= _matrix.subBox[3] && ym >= _matrix.subBox[1] && ym <= _matrix.subBox[4])
                intersections.Add(new Vector(xm, ym, _matrix.subBox[5]));
            return intersections.Count switch
            {
                2 =>
                    Math.Sqrt((intersections[0].X - ray.X0.X) * (intersections[0].X - ray.X0.X) +
                              (intersections[0].Y - ray.X0.Y) * (intersections[0].Y - ray.X0.Y) +
                              (intersections[0].Z - ray.X0.Z) * (intersections[0].Z - ray.X0.Z)) <
                    Math.Sqrt((intersections[1].X - ray.X0.X) * (intersections[1].X - ray.X0.X) +
                              (intersections[1].Y - ray.X0.Y) * (intersections[1].Y - ray.X0.Y) +
                              (intersections[1].Z - ray.X0.Z) * (intersections[1].Z - ray.X0.Z))
                        ? intersections[0]
                        : intersections[1],
                _ => null
            };
        }
        
        private Color GetMatrixPointColor(Vector point)
        {
            var x = (int)Math.Floor(point.X);
            var y = (int)Math.Floor(point.Y);
            var z = (int)Math.Floor(point.Z);
            // Console.WriteLine($"x: {x}, y: {y}, z: {z}");

            if (x == _matrix.length) x--;
            if (y == _matrix.width) y--;
            if (z == _matrix.height) z--;

            // TODO: switch between this or next block
            // if (x == -1) x = 0;
            // if (y == -1) y = 0;
            // if (z == -1) z = 0;

            // if (x < 0 || y < 0 || z < 0)
            // {
            //     return new Color();
            // }

            // const int diff = 100;
            // if (x < diff && y < diff && z < diff)
            // {
            //     return new Color(255, 0, 0, 1);
            // }
            //
            // if (x > _matrix.length - diff && y > _matrix.width - diff && z > _matrix.height - diff)
            // {
            //     return new Color(0, 255, 0, 1);
            // }

            return _colorMapper(_matrix.data[x, y, z]);
        }

        private Vector GetNormal(int x, int y, int z)
        {
            if (x == _matrix.length) x--;
            if (y == _matrix.width) y--;
            if (z == _matrix.height) z--;

            double a = x + 1 < _matrix.length ? _matrix.data[x + 1, y, z] : 0;
            double b = x - 1 >= 0 ? _matrix.data[x - 1, y, z] : 0;
            double c = y + 1 < _matrix.width ? _matrix.data[x, y + 1, z] : 0;
            double d = y - 1 >= 0 ? _matrix.data[x, y - 1, z] : 0;
            double e = z + 1 < _matrix.height ? _matrix.data[x, y, z + 1] : 0;
            double f = z - 1 >= 0 ? _matrix.data[x, y, z - 1] : 0;

            return new Vector(a - b, c - d, e - f).Normalize();
        }

        private Color FindColor(int i, int j, Line cameraRay, double minDist, double maxDist)
        {
            var t = FindFirstIntersection(cameraRay, minDist, maxDist);
            if (t.Equals(-1)) return new Color();
            _file.WriteLine($"{cameraRay.X0},{cameraRay.Dx},{t:F3},{i},{j}");

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


        private bool IsRayPointInSubBox(Line ray, double point)
        {
            var position = ray.CoordinateToPosition(point);
            var subBox = _matrix.subBox;

            return position.X.IsInRange(subBox[0], subBox[3]) &&
                   position.Y.IsInRange(subBox[1], subBox[4]) &&
                   position.Z.IsInRange(subBox[2], subBox[5]);
            return position.X.IsInRange(0, _matrix.length) &&
                   position.Y.IsInRange(0, _matrix.width) &&
                   position.Z.IsInRange(0, _matrix.height);
        }

        private void PixelColorCalculation(Camera camera, Line cameraRay, Image image, int i, int j)
        {
            // image.SetPixel(i, j, FindColor(i, j, cameraRay, camera.FrontPlaneDistance, camera.BackPlaneDistance));
            // var t = FindFirstIntersection(cameraRay, camera.FrontPlaneDistance, camera.BackPlaneDistance);
            var intersection = FindFirstIntersection3(cameraRay);

            if (intersection == null)
            {
                image.SetPixel(i, j, Background);
                return;
            }


            const double step = 0.4;
            var sample = cameraRay.PositionToCoordinate(intersection);
            var color = new Color();
            Vector first = null;

            sample += 0.2;
            while (IsRayPointInSubBox(cameraRay, sample))
            {
                var samplePosition = cameraRay.CoordinateToPosition(sample);
                var addColor = GetMatrixPointColor(samplePosition);

                if (color.Alpha != 0 && first == null) first = samplePosition;
                color = color.Blend(addColor);
                if (color.Alpha > 0.99) break;

                sample += step;
            }

            if (first == null)
            {
                image.SetPixel(i, j, color);
                return;
            }


            // light
            var x = (int)Math.Floor(first.X);
            var y = (int)Math.Floor(first.Y);
            var z = (int)Math.Floor(first.Z);
            var n = GetNormal(x, y, z);
            var materialColor = GetMatrixPointColor(first);
            var material = new Material(materialColor, materialColor, materialColor, 100 / 2);

            foreach (var light in _lights)
            {
                var lightColor = new Color();
                lightColor += material.Ambient * new Color(light.Ambient.Red / 10, light.Ambient.Green / 10,
                    light.Ambient.Blue / 10, light.Ambient.Alpha);
                
                var e = (camera.Position - first).Normalize();
                var t = (light.Position - first).Normalize();
                var nDotT = n * t;
                var r = (n * (2 * nDotT) - t).Normalize();
                var eDotR = e * r;
                
                if (nDotT > 0) lightColor += material.Diffuse * light.Diffuse * nDotT;
                if (eDotR > 0) lightColor += material.Specular * light.Specular * Math.Pow(eDotR, material.Shininess);
                lightColor *= light.Intensity;
                
                color += lightColor;
            }

            image.SetPixel(i, j, color);
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
                                        + (camera.Up ^ camera.Direction) *
                                        ImageToViewPlane(i, width, camera.ViewPlaneWidth);
                    var cameraRay = new Line(camera.Position, x1); // the line from the camera through the pixel

                    PixelColorCalculation(camera, cameraRay, image, i, j);
                }
            }

            image.Store(filename);
        }

        public void Dispose()
        {
            _file.Flush();
            _file?.Dispose();
        }
    }
}