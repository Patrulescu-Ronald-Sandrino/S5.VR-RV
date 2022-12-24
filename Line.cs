using System;

namespace rt
{
    public class Line
    {
        public Vector X0 { get; set; }
        public Vector Dx { get; set; }

        public Line(Vector x0, Vector x1)
        {
            X0 = new Vector(x0);
            Dx = new Vector(x1 - x0);
            Dx.Normalize();
        }

        public Vector CoordinateToPosition(double t)
        {
            return new Vector(Dx * t + X0);
        }
        
        public double PositionToCoordinate(Vector v)
        {
            var xCoordinate = (v.X - X0.X) / Dx.X;
            var yCoordinate = (v.Y - X0.Y) / Dx.Y;
            var zCoordinate = (v.Z - X0.Z) / Dx.Z;
            const double tolerance = 0.001;

            if (!(Math.Abs(xCoordinate - yCoordinate) < tolerance && Math.Abs(yCoordinate - zCoordinate) < tolerance))
            {
                // throw new ArgumentException("The point is not on the line");
            }
            
            return xCoordinate;
        }
    }
}