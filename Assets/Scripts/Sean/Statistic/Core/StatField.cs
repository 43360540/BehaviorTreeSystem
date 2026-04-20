using System;

namespace Sean.Statistic
{
    public class StatField
    {
        public float Max { get; private set; }
        public float Current { get; private set; }

        public void SetMax(float max)
        {
            if (max <= 0f)
                return;
            this.Max = Math.Max(max, 1f);
            Current = Math.Clamp(Current, 0f, max);
        }

        public StatField(float max, float current)
        {
            this.Max = Math.Max(1f, max);
            this.Current = Math.Clamp(current, 0f, max);
        }

        public float Increase(float amount)
        {
            float before = Current;
            if (amount <= 0f)
                return 0f;
            Current = Math.Min(Current + amount, Max);
            return Current - before;
        }

        public float Decrease(float amount)
        {
            float before = Current;
            if (amount <= 0f)
                return 0f;
            Current = Math.Clamp(Current - amount, 0f, Max);
            Current = Current <= 1e-4f ? 0f : Current;
            return before - Current;
        }
    }
}