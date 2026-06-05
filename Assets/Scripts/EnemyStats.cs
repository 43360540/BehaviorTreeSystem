using System.Collections.Generic;
using Sean.Statistic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyStats : StatisticBase<string>
{
    [SerializeField, Range(0f, 100f)] private float _currentHp = 100f;
    [SerializeField] private Image _ui;

    protected override void OnStatisticReady()
    {
        base.OnStatisticReady();
        ReadOnlyStatistic.StatisticChanged += UiUpdate;
    }

    protected override List<StatInfo<string>> ProvideStatInfos()
    {
        List<StatInfo<string>> want = new();
        want.Add(new StatInfo<string>("Health", 100f, 100f));
        return want;
    }

    private void UiUpdate(int amount)
    {
        _ui.fillAmount = ReadOnlyStatistic.GetCurrent("Health") / ReadOnlyStatistic.GetMax("Health");
    }

    private void Update()
    {
        var x = ReadOnlyStatistic.GetCurrent("Health") - _currentHp;
        if (x >= 1e-6f || x <= 1e-6f)
        {
            if (x < 0)
            {
                StatisticSvc.Increase("Health", Mathf.Abs(x));
                return;
            }
            StatisticSvc.Decrease("Health", Mathf.Abs(x));
        }
    }
}
