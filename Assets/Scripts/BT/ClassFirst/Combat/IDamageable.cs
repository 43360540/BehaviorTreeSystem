using UnityEngine;

namespace BehaviorTree.ClassFirst
{
    /// <summary>
    /// Anything an NPC can sense and attack.
    /// BaseNPCRunner implements this. Player does NOT — Player is an observer.
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        Faction Faction { get; }
        Transform Transform { get; }
        void TakeDamage(float damage, IDamageable source);
    }
}
