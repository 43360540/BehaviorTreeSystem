using System;
using System.Collections.Generic;

namespace Sean.Statistic
{
    public class ReadOnlyStatistic<T> : IReadOnlyStatistic<T>
    {
        public ReadOnlyStatistic(Statistic<T> statistic)
        {
            this._statistic = statistic;
            this._statIndices =  statistic.StatIndices;
        }
        
        private readonly Statistic<T> _statistic;
        private readonly Dictionary<T, int> _statIndices;

        public event Action<int> StatisticChanged
        {
            add => _statistic.StatisticChanged += value;
            remove => _statistic.StatisticChanged -= value;
        }

        public T GetStatType(int index)
        {
            foreach (var i in _statIndices)
            {
                if (i.Value == index)
                    return i.Key;
            }
            return default;
        }
        
        public int GetStatIndex(T statType)
        {
            return _statIndices[statType];
        }

        public StatField GetStatField(T statType)
        {
            var index = GetStatIndex(statType);
            return _statistic.StatFields[index];
        }
        
        public float GetMax(T statType)
        {
            var index = GetStatIndex(statType);
            return _statistic.StatFields[index].Max;
        }
        
        public float GetCurrent(T statType)
        {
            var index = GetStatIndex(statType);
            return _statistic.StatFields[index].Current;
        }
        
        public float GetNormalizedCurrent(T statType)
        {
            var index = GetStatIndex(statType);
            return _statistic.StatFields[index].Current / _statistic.StatFields[index].Max;
        }
        
        public bool IsCurrentZero(T statType)
        {
            var index = GetStatIndex(statType);
            return _statistic.StatFields[index].Current <= 1e-4f;
        }    
    }
}