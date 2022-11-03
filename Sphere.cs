using System;

namespace rt
{
    public class Sphere : Geometry
    {
        private Vector Center { get; set; }
        private double Radius { get; set; }

        public Sphere(Vector center, double radius, Material material, Color color) : base(material, color)
        {
            Center = center;
            Radius = radius;
        }

        public override Intersection GetIntersection(Line line, double minDist, double maxDist)
        {
            #region ADD CODE HERE: Calculate the intersection between the given line and this sphere
            var a = line.Dx.Length2();
            var v = line.X0 - Center;
            var b = 2 * (line.Dx * v);
            var c = v.Length2() - Radius * Radius;
            var delta = b * b - 4 * a * c;

            if (delta < 0)
            {
                return new Intersection() { Valid = false };
            }

            if (delta == 0)
            {
                var t = -b / (2 * a);
                return new Intersection(true, t >= minDist && t <= maxDist, this, line, t);
            }

            var t1 = (-b + Math.Sqrt(delta)) / (2 * a);
            var t2 = (-b - Math.Sqrt(delta)) / (2 * a);
            var tMin = Math.Min(t1, t2);
            
            return new Intersection(true, tMin >= minDist && tMin <= maxDist, this, line, tMin);

            #endregion
        }

        public override Vector Normal(Vector v)
        {
            var n = v - Center;
            n.Normalize();
            return n;
        }
    }
}