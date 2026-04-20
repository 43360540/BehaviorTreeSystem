using System;

namespace Sean.Statistic
{
    public interface IReadOnlyStatistic<T>
    {
        event Action<int> StatisticChanged;

        T GetStatType(int index);
        StatField GetStatField(T statType);
        int GetStatIndex(T statType);
        float GetMax(T statType);
        float GetCurrent(T statType);
        float GetNormalizedCurrent(T statType);
        bool IsCurrentZero(T statType);
    }

    public interface IStatisticService<in T>
    {
        float Increase(T statType, float amount);
        float Decrease(T statType, float amount);
        
        bool TryIncrease(T statType, float amount);
        bool TryDecrease(T statType, float amount);
    }
}