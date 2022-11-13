using System;

namespace rt
{
    class RayTracer
    {
        private static double FREQUENCY = 5;
        
        private Geometry[] geometries;
        private Matrix matrix;
        private Func<int, Color> colorMapper;
        private Light[] lights;

        public RayTracer(Matrix matrix, Func<int, Color> colorMapper, Light[] lights)
        {
            this.matrix = matrix;
            this.colorMapper = colorMapper;
            this.lights = lights;
        }

        private double ImageToViewPlane(int n, int imgSize, double viewPlaneSize)
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
            var vMin = new Vector(0, 0, 0);
            var vMax = new Vector(matrix.length, matrix.width, matrix.height);

            var tMinX = (vMin.X - cameraRay.X0.X) / cameraRay.Dx.X;
            var tMaxX = (vMax.X - cameraRay.X0.X) / cameraRay.Dx.X;
            var tMinY = (vMin.Y - cameraRay.X0.Y) / cameraRay.Dx.Y;
            var tMaxY = (vMax.Y - cameraRay.X0.Y) / cameraRay.Dx.Y;
            var tMinZ = (vMin.Z - cameraRay.X0.Z) / cameraRay.Dx.Z;
            var tMaxZ = (vMax.Z - cameraRay.X0.Z) / cameraRay.Dx.Z;
            
            var tMin = Math.Max(Math.Max(Math.Min(tMinX, tMaxX), Math.Min(tMinY, tMaxY)), Math.Min(tMinZ, tMaxZ));
            var tMax = Math.Min(Math.Min(Math.Max(tMinX, tMaxX), Math.Max(tMinY, tMaxY)), Math.Max(tMinZ, tMaxZ));
            
            if (tMax < 0 || tMin > tMax)
            {
                return -1;
            }
            
            var t = tMin < 0 ? tMax : tMin;
            
            if (t - minDist < 0.0001 || t - maxDist > 0.0001)
            {
                return -1;
            }

            return t;
        }
        
        private Color GetMatrixPointColor(Vector point)
        {
            var x = (int) Math.Round(point.X);
            var y = (int) Math.Round(point.Y);
            var z = (int) Math.Round(point.Z);
            // Console.WriteLine($"x: {x}, y: {y}, z: {z}");
            
            if (x < 0 || x >= matrix.length || y < 0 || y >= matrix.width || z < 0 || z >= matrix.height)
            {
                return new Color();
            }

            return colorMapper(matrix.data[x, y, z]);
        }

        private static Color Blend(Color color, Color other)
        {
            return new Color(
                color.Red * color.Alpha + other.Red * other.Alpha * (1 - color.Alpha),
                color.Green * color.Alpha + other.Green * other.Alpha * (1 - color.Alpha),
                color.Blue * color.Alpha + other.Blue * other.Alpha * (1 - color.Alpha),
                color.Alpha + (1 - other.Alpha) * other.Alpha
            );
        }

        private Color FindColor(Line cameraRay, double minDist, double maxDist)
        {
            var t = FindFirstIntersection(cameraRay, minDist, maxDist);
            if (t.Equals(-1))
            {
                return new Color();
            }
            var intersectionPoint = cameraRay.X0 + cameraRay.Dx * t;
            var color = GetMatrixPointColor(intersectionPoint);

            while (color.Alpha.GreaterThan(0) && color.Alpha.LessThan(1))
            {
                t = FindFirstIntersection(cameraRay, t + 0.0001 + FREQUENCY, maxDist);
                if (t.Equals(-1))
                {
                    return color;
                }
                intersectionPoint = cameraRay.X0 + cameraRay.Dx * t;
                var newColor = GetMatrixPointColor(intersectionPoint);
                color = Blend(color, newColor);
            }
            
            return color;
        }

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

                    image.SetPixel(i, j, FindColor(cameraRay, camera.FrontPlaneDistance, camera.BackPlaneDistance));
                    
                    #endregion
                }
            }

            image.Store(filename);
        }
    }
}