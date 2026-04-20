using System;
using System.Collections.Generic;
using UnityEngine;
using Sean.Statistic;

public abstract class StatisticUiBase<T> : MonoBehaviour
{
    private bool _isInitialized = false;
    
    private Dictionary<T, StatFieldUi> Uis { get; set; }
    private IReadOnlyStatistic<T> Statistic { get; set; }
    
    public void Init(IReadOnlyStatistic<T> stat)
    {
        if (stat == null)
            throw new ArgumentNullException($"{name}: Init argument 'stat' is null. ({GetType()})");
        
        if (_isInitialized)
            throw new InvalidOperationException($"{name}: Init was called more than once. ({GetType()})");
        
        Uis =  ProvideStatFieldUis();
        
        Statistic = stat;
        Statistic.StatisticChanged -= OnStatChanged;
        Statistic.StatisticChanged += OnStatChanged;
        
        _isInitialized = true;
    }

    private void OnEnable()
    {
        if (!_isInitialized) return;
        Statistic.StatisticChanged -= OnStatChanged;
        Statistic.StatisticChanged += OnStatChanged;
    }

    private void OnDisable()
    {
        if (!_isInitialized) return;
        Statistic.StatisticChanged -= OnStatChanged;
    }

    protected abstract Dictionary<T, StatFieldUi> ProvideStatFieldUis();

    private void OnStatChanged(int index)
    {
        var statType = Statistic.GetStatType(index);
        
        var current = Statistic.GetCurrent(statType);
        var max = Statistic.GetMax(statType);
        
        Uis[statType].UiUpdate(current, max);
        
    }
}
