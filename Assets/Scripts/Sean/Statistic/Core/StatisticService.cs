using System;
using System.Collections.Generic;

namespace Sean.Statistic
{
    public class StatisticService<T> : IStatisticService<T>
    {
        public StatisticService(Statistic<T> statistic)
        {
            _statistic = statistic;
            _statIndices = statistic.StatIndices;
        }
        
        private readonly Statistic<T> _statistic;
        private readonly Dictionary<T, int> _statIndices;
        
        public float Increase(T statType, float amount)
        {
            int index = _statIndices[statType];
            
            return _statistic.StatIncrease(index, amount);
        }        
        public float Decrease(T statType, float amount)
        {
            int index = _statIndices[statType];
            
            return _statistic.StatDecrease(index, amount);
        }
        public bool TryIncrease(T statType, float amount)
        {
            int index = _statIndices[statType];
            
            if (_statistic.StatFields[index].Current + amount > _statistic.StatFields[index].Max)
                return false;
            _statistic.StatIncrease(index, amount);
            return true;
        }

        public bool TryDecrease(T statType, float amount)
        {
            int index = _statIndices[statType];
            
            if (_statistic.StatFields[index].Current - amount < 0)
                return false;
            _statistic.StatDecrease(index, amount);
            return true;
        }

        private int GetStatIndex(T statType)
        {
            return _statIndices[statType];
        }
    }
}