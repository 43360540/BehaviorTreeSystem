using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTree.ClassFirst
{
    /// <summary>
    /// Blackboard for the Class-First NPC stack.
    /// Pure C# data holder — no MonoBehaviour, no Unity serialization.
    /// Built by BaseNPCRunner.Awake() and handed to every Action / Condition.
    /// </summary>
    public sealed class BTContext
    {
        // ---- Identity / Unity refs (set once at construction) -------------
        public Transform Self { get; }
        public IDamageable SelfAsDamageable { get; }
        public NavMeshAgent Agent { get; }
        public Animator Anim { get; }
        public LayerMask SensorLayer { get; }
        public Faction Faction { get; }

        // ---- Mutable runtime state ----------------------------------------
        /// <summary>Current sense target. Null when no one is in range.</summary>
        public IDamageable Target { get; set; }
        /// <summary>Wounded ally to heal (Healer only). Null when none in range.</summary>
        public IHealable Ally { get; set; }
        /// <summary>Transform of current Ally (kept separate for NavMesh / distance lookups).</summary>
        public Transform AllyTransform { get; set; }
        public Vector3 HomePosition { get; set; }
        public float Hp { get; set; }

        // ---- Tunables (set from Inspector via BaseNPCRunner) --------------
        public float WalkSpeed { get; }
        public float AttackRange { get; }
        public float SensorRadius { get; }
        public float MaxHp { get; }
        public float AttackDamage { get; }
        public Vector3[] PatrolPoints { get; }
        /// <summary>
        /// Unit-vector toward the enemy main line. Used by MarchForward when no
        /// target is in sense. Zero = stay put (no march). Set by Runner from
        /// Inspector field _enemyDirection.
        /// </summary>
        public Vector3 EnemyDirection { get; }

        // ---- Scratch / cached ---------------------------------------------
        public Collider[] OverlapBuffer { get; }
        public int AttackTriggerHash { get; }
        public int IsAttackingHash { get; }

        // ---- Derived helpers ----------------------------------------------
        public bool HasTarget => Target != null && Target.IsAlive;

        public Vector3 TargetPosition =>
            HasTarget ? Target.Transform.position : Self.position;

        public float TargetDistance =>
            HasTarget ? Vector3.Distance(Self.position, Target.Transform.position)
                      : float.PositiveInfinity;

        public Vector3 TargetDirectionFlat
        {
            get
            {
                if (!HasTarget) return Self.forward;
                Vector3 d = Target.Transform.position - Self.position;
                d.y = 0f;
                return d.sqrMagnitude > 1e-6f ? d.normalized : Self.forward;
            }
        }

        public float HpRatio => MaxHp > 0f ? Mathf.Clamp01(Hp / MaxHp) : 0f;

        public bool HasAlly => Ally != null && AllyTransform != null;
        public float AllyDistance =>
            HasAlly ? Vector3.Distance(Self.position, AllyTransform.position) : float.PositiveInfinity;

        public BTContext(
            Transform self,
            IDamageable selfAsDamageable,
            NavMeshAgent agent,
            Animator anim,
            LayerMask sensorLayer,
            Faction faction,
            float walkSpeed,
            float attackRange,
            float sensorRadius,
            float maxHp,
            float attackDamage,
            Vector3 enemyDirection,
            Vector3[] patrolPoints = null,
            int overlapBufferSize = 16)
        {
            Self = self;
            SelfAsDamageable = selfAsDamageable;
            Agent = agent;
            Anim = anim;
            SensorLayer = sensorLayer;
            Faction = faction;

            WalkSpeed = walkSpeed;
            AttackRange = attackRange;
            SensorRadius = sensorRadius;
            MaxHp = maxHp;
            AttackDamage = attackDamage;
            EnemyDirection = enemyDirection.sqrMagnitude > 1e-6f ? enemyDirection.normalized : Vector3.zero;
            Hp = maxHp;
            PatrolPoints = patrolPoints ?? System.Array.Empty<Vector3>();
            HomePosition = self != null ? self.position : Vector3.zero;

            OverlapBuffer = new Collider[overlapBufferSize];

            // Animator hashes — match EnemyAnimationCtrl / Enemy.controller
            AttackTriggerHash = Animator.StringToHash("AttackTrigger");
            IsAttackingHash = Animator.StringToHash("IsAttacking");
        }
    }
}
