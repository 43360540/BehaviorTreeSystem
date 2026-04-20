using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sean.Statistic
{
    public abstract class StatisticBase<T> : MonoBehaviour 
    {
        private Statistic<T> _statistic;
        
        public IReadOnlyStatistic<T> ReadOnlyStatistic { get; private set; }
        public IStatisticService<T> StatisticSvc { get; private set; }
        public List<StatInfo<T>> StatInfos { get;  private set; }
        
        private void Awake()
        {
            Init();
            OnStatisticReady();
        }

        private void Init()
        {
            StatInfos = ProvideStatInfos();
            
            _statistic = new Statistic<T>(StatInfos);
            ReadOnlyStatistic = new ReadOnlyStatistic<T>(_statistic);
            StatisticSvc = new StatisticService<T>(_statistic);
        }

        protected abstract List<StatInfo<T>> ProvideStatInfos();
        
        protected virtual void OnStatisticReady() {}
    }
}