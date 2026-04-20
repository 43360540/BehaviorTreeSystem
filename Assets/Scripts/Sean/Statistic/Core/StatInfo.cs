namespace Sean.Statistic
{
    public struct StatInfo<T>
    {
        public float Max { get; }
        public float Current { get; }
        public T Type { get; }

        public StatInfo(T type, float max, float current)
        {
            Type = type;
            Max = max;
            Current = current;
        }
    }
}