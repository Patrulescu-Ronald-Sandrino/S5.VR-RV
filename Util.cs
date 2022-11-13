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
        
        public static bool GreaterThan(this double value, double other)
        {
            return value - other > Tolerance;
        }
        
        public static bool LessThan(this double value, double other)
        {
            return other - value > Tolerance;
        }
        
        public static bool GreaterThanOrEquals(this double value, double other)
        {
            return value.GreaterThan(other) || value.Equals(other);
        }
        
        public static bool LessThanOrEquals(this double value, double other)
        {
            return value.LessThan(other) || value.Equals(other);
        }
    }
}