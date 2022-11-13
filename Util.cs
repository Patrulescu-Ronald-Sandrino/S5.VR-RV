namespace rt
{
    public static class Util
    {
        public static bool IsInRange(this int value, int min, int maxExclusive)
        {
            return value >= min && value < maxExclusive;
        }
    }
}