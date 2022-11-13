using System;

namespace rt
{
    public static class Util
    {
        public const double Tolerance = 0.0001;


        public static bool IsInRange(this int value, int min, int maxExclusive)
        {
            return value >= min && value < maxExclusive;
        }
        
        public static bool Equals(this double value, double other)
        {
            return Math.Abs(value - other) < Tolerance;
        }
        
    }
}