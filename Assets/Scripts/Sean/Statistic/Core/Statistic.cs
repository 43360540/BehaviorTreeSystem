using System;
using System.Collections.Generic;

namespace Sean.Statistic
{
    public class Statistic<T>
    {
        public Statistic(List<StatInfo<T>> statInfos)
        {
            for (var i = 0; i < statInfos.Count; i++)
            {
                var statInfo = statInfos[i];

                StatFields.Add(new StatField(statInfo.Max, statInfo.Current));
                StatIndices.Add(statInfo.Type, i);
            }
        }

        public List<StatField> StatFields { get; } = new();
        public Dictionary<T, int> StatIndices { get; } = new();
        public event Action<int> StatisticChanged;

        public float StatIncrease(int index, float amount)
        {
            var actualIncrease = StatFields[index].Increase(amount);
            StatisticChanged?.Invoke(index);
            return actualIncrease;
        }

        public float StatDecrease(int index, float amount)
        {
            var actualDecreased = StatFields[index].Decrease(amount);
            StatisticChanged?.Invoke(index);
            return actualDecreased;
        }

        // private Statistic(List<StatField> statFields)
        // {
        //     StatFields = statFields;
        // }

        // public Statistic<T> Clone() =>
        //     new Statistic<T>(StatFields);
        //
        // public void CopyFrom(Statistic<T> other) {}
    }
}
