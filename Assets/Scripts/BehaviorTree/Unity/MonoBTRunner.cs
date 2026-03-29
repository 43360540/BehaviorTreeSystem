using UnityEngine;

namespace BehaviorTree
{
    public abstract class MonoBTRunner<TContext> : MonoBehaviour
    {
        public TContext Context { get; private set; }
        public INode<TContext> Tree { get; private set; }
        public float Duration { get; private set; }

        protected abstract TContext CreateContext();
        protected abstract INode<TContext> CreateTree();

        protected virtual void Awake()
        {
            Context = CreateContext();
            Tree = CreateTree();
        }

        protected virtual void Update()
        {
            Duration = Time.deltaTime;
            Tree?.Tick(Context, Duration);
        }

        protected virtual void OnDisable()
        {
            Tree?.Abort(Context);
        }
    }
}