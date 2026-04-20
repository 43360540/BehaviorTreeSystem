using UnityEngine;

namespace Sean.Statistic
{
    [CreateAssetMenu(menuName = "Statistic/StatDef")]
    public class StatDef : ScriptableObject
    {
        public string StatType;
        public float Max = 100f;
        public float Current = 100f;

    }
}