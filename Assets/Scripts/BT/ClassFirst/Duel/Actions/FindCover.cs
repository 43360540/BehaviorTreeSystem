using UnityEngine;

namespace BehaviorTree.ClassFirst.Duel.Actions
{
    /// <summary>
    /// Queries <see cref="CoverRegistry"/> for the best cover within
    /// <paramref name="searchRadius"/> that protects from the current target,
    /// and writes the result into the host Runner's <see cref="ICoverHolder.CurrentCover"/>.
    ///
    /// <para>Returns Success if a cover was found, Failure otherwise. Doesn't
    /// move the NPC — pair with MoveToCover in a Sequence.</para>
    /// </summary>
    public sealed class FindCover : ActionBase<BTContext>
    {
        private readonly ICoverHolder _holder;
        private readonly float _searchRadius;

        public FindCover(ICoverHolder holder, float searchRadius = 25f)
        {
            _holder = holder;
            _searchRadius = searchRadius;
        }

        public override NodeStatus Tick(BTContext ctx, float dt)
        {
            if (_holder == null || !ctx.HasTarget) return NodeStatus.Failure;

            var cover = CoverRegistry.FindBestCover(
                ctx.Self.position,
                ctx.Target.Transform.position,
                _searchRadius);

            _holder.CurrentCover = cover;
            return cover != null ? NodeStatus.Success : NodeStatus.Failure;
        }
    }
}
