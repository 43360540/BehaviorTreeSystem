using UnityEngine;

namespace BehaviorTree.ClassFirst.Actions
{
    /// <summary>
    /// Loops the NPC through ctx.PatrolPoints, pausing briefly at each.
    /// Always Running while patrolling; Failure when there are no points
    /// configured. Designed to be used as the "default" branch of a Selector —
    /// when sensing picks up an enemy a higher-priority branch should preempt.
    /// </summary>
    public sealed class PatrollerPatrol : ActionBase<BTContext>
    {
        private readonly float _waitAtPoint;
        private int _index;
        private float _waitElapsed;
        private bool _waiting;
        private bool _initialised;

        public PatrollerPatrol(float waitAtPoint = 1.5f)
        {
            _waitAtPoint = waitAtPoint;
        }

        public override void Start(BTContext ctx)
        {
            _waiting = false;
            _waitElapsed = 0f;
            if (ctx.PatrolPoints.Length == 0)
            {
                _initialised = false;
                return;
            }

            // Pick the nearest point as our first destination so we don't
            // backtrack on the very first tick.
            int closest = 0;
            float closestSqr = float.PositiveInfinity;
            for (int i = 0; i < ctx.PatrolPoints.Length; i++)
            {
                float sqr = (ctx.PatrolPoints[i] - ctx.Self.position).sqrMagnitude;
                if (sqr < closestSqr) { closestSqr = sqr; closest = i; }
            }
            _index = closest;
            _initialised = true;

            ctx.Agent.speed = ctx.WalkSpeed;
            ctx.Agent.isStopped = false;
            ctx.Agent.SetDestination(ctx.PatrolPoints[_index]);
        }

        public override NodeStatus Tick(BTContext ctx, float dt)
        {
            if (!_initialised || ctx.PatrolPoints.Length == 0)
                return NodeStatus.Failure;

            if (_waiting)
            {
                _waitElapsed += dt;
                if (_waitElapsed >= _waitAtPoint)
                {
                    _waiting = false;
                    _waitElapsed = 0f;
                    _index = (_index + 1) % ctx.PatrolPoints.Length;
                    ctx.Agent.SetDestination(ctx.PatrolPoints[_index]);
                }
                return NodeStatus.Running;
            }

            if (ctx.Agent.pathPending) return NodeStatus.Running;

            if (!ctx.Agent.hasPath
                || ctx.Agent.remainingDistance <= ctx.Agent.stoppingDistance + 0.05f)
            {
                _waiting = true;
                return NodeStatus.Running;
            }
            return NodeStatus.Running;
        }

        public override void Reset()
        {
            _waiting = false;
            _waitElapsed = 0f;
            _initialised = false;
        }

        public override void Stop(BTContext ctx, NodeStatus stopStatus)
        {
            if (ctx.Agent != null && ctx.Agent.isOnNavMesh)
                ctx.Agent.ResetPath();
        }
    }
}
