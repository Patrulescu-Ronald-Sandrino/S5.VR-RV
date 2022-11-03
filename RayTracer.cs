using System;
using System.Runtime.InteropServices;

namespace rt
{
    class RayTracer
    {
        private Geometry[] geometries;
        private Light[] lights;

        public RayTracer(Geometry[] geometries, Light[] lights)
        {
            this.geometries = geometries;
            this.lights = lights;
        }

        private double ImageToViewPlane(int n, int imgSize, double viewPlaneSize)
        {
            var u = n * viewPlaneSize / imgSize;
            u -= viewPlaneSize / 2;
            return u;
        }

        private Intersection FindFirstIntersection(Line ray, double minDist, double maxDist)
        {
            var intersection = new Intersection {Valid = false};

            foreach (var geometry in geometries)
            {
                var intr = geometry.GetIntersection(ray, minDist, maxDist);

                if (!intr.Valid || !intr.Visible) continue;

                if (!intersection.Valid || !intersection.Visible)
                {
                    intersection = intr;
                }
                else if (intr.T < intersection.T)
                {
                    intersection = intr;
                }
            }

            return intersection;
        }

        private bool IsLit(Vector point, Light light)
        {
            // ADD CODE HERE: Detect whether the given point has a clear line of sight to the given light
            var intersection = FindFirstIntersection(new Line(light.Position, point), 0.0, (light.Position - point).Length() - 0.01);
            
            return !intersection.Visible;
        }

        public void Render(Camera camera, int width, int height, string filename)
        {
            var image = new Image(width, height);

            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    #region ADD CODE HERE: Implement pixel color calculation
                    var x1 =
                        camera.Position + camera.Direction * camera.ViewPlaneDistance 
                        + camera.Up * ImageToViewPlane(j, height, camera.ViewPlaneHeight)
                        + (camera.Up ^ camera.Direction) * ImageToViewPlane(i, width, camera.ViewPlaneWidth);
                    
                    var intersection = FindFirstIntersection(
                        new Line(camera.Position, x1), 
                        camera.FrontPlaneDistance, 
                        camera.BackPlaneDistance
                        );

                    if (intersection.Visible && intersection.Valid)
                    {
                        var position = intersection.Position;
                        var sphere = (Sphere) intersection.Geometry;
                        var material = sphere.Material;
                        var imageColor = new Color(); // sphere.Color
                        
                        foreach (var light in lights)
                        {
                            var color = material.Ambient * light.Ambient;
                            if (IsLit(position, light))
                            {
                                var n = sphere.Normal(position);
                                // var n = (position - sphere.Center).Normalize();
                                var t = (light.Position - position).Normalize();
                                var n_Times_t = n * t;
                                var e = (camera.Position - position).Normalize();
                                var r = (n * n_Times_t * 2 - t).Normalize();
                                var e_Times_r = e * r;
                                
                                if (n_Times_t > 0)
                                {
                                    color += material.Diffuse * light.Diffuse * n_Times_t;
                                }

                                if (e_Times_r > 0)
                                {
                                    color += material.Specular * light.Specular * Math.Pow(e_Times_r, material.Shininess);
                                }

                                color *= light.Intensity;
                            }

                            imageColor += color;
                        }
                        image.SetPixel(i, j, imageColor);
                    }
                    else
                    {
                        image.SetPixel(i, j, new Color());
                    }
                    #endregion
                }
            }

            image.Store(filename);
        }
    }
}