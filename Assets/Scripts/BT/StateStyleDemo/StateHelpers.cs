using BehaviorTree.ClassFirst; // borrow IDamageable / Faction (shared combat layer)
using UnityEngine;

namespace BehaviorTree.StateStyleDemo
{
    /// <summary>
    /// Small grab-bag of pure functions shared by the State-Style NPCs.
    /// We use static helpers (not a base class) because State-Style already
    /// forces inheritance from StateStyleBase&lt;TSelf, TStates&gt;; adding another
    /// abstract layer would over-complicate the demo.
    /// </summary>
    public static class StateHelpers
    {
        /// <summary>
        /// OverlapSphere → filter self / same-faction / dead → closest survivor.
        /// </summary>
        public static IDamageable FindClosestEnemy(
            Transform self, LayerMask sensorLayer, float radius,
            Collider[] buffer, Faction myFaction)
        {
            int hit = Physics.OverlapSphereNonAlloc(self.position, radius, buffer, sensorLayer);
            IDamageable best = null;
            float bestSqr = float.PositiveInfinity;
            Vector3 selfPos = self.position;
            for (int i = 0; i < hit; i++)
            {
                var d = buffer[i].GetComponentInParent<IDamageable>();
                if (d == null || !d.IsAlive) continue;
                if (ReferenceEquals(d.Transform, self)) continue;
                if (d.Faction == myFaction) continue;
                float sqr = (d.Transform.position - selfPos).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = d; }
            }
            return best;
        }

        /// <summary>NavMesh tick for "walk to a Vector3", returns BT-style status.</summary>
        public static NodeStatus WalkTo(UnityEngine.AI.NavMeshAgent agent, Vector3 destination, float arriveSlack = 0.05f)
        {
            if (!agent.isOnNavMesh) return NodeStatus.Failure;
            if ((agent.destination - destination).sqrMagnitude > 0.0625f)
                agent.SetDestination(destination);
            if (agent.pathPending) return NodeStatus.Running;
            if (agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
                return NodeStatus.Failure;
            if (!agent.hasPath && agent.remainingDistance <= agent.stoppingDistance)
                return NodeStatus.Success;
            if (agent.remainingDistance <= agent.stoppingDistance + arriveSlack)
                return NodeStatus.Success;
            return NodeStatus.Running;
        }
    }
}
