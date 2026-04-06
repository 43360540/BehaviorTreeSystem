using UnityEngine;

namespace BehaviorTree
{
    public abstract class MonoBTRunner<TContext> : MonoBehaviour
    {
        [SerializeField] private Rate _tickRate = Rate.Update;
        [SerializeField] private TContext _context;

        public Rate TickRate => _tickRate;
        public TContext Context => _context;
        public INode<TContext> Tree => _bTRunner?.Tree;

        protected abstract INode<TContext> CreateTree();

        private BTRunner<TContext> _bTRunner;

        protected virtual void Awake()
        {
            if (_context == null)
                Debug.LogError($"[{GetType().Name}] Context not set. Use Inspector or SetContext() before Start(). ({gameObject.name})");
        }

        protected virtual void Start()
        {
            _bTRunner = new (_context, CreateTree());
        }

        protected virtual void Update()
        {
            if (TickRate != Rate.Update)
                return;
            _bTRunner?.Tick(Time.deltaTime);
        }

        protected virtual void FixedUpdate()
        {
            if (TickRate != Rate.FixedUpdate)
                return;
            _bTRunner?.Tick(Time.fixedDeltaTime);
        }

        protected virtual void OnDisable()
        {
            _bTRunner?.Abort();
        }
        // Set context programmatically if not assigned via Inspector
        // !! Must use it before Start() !! 
        protected void SetContext(TContext context)
        {
            if (_context == null)
                _context = context;
        }

        public enum Rate
        {
            Update,
            FixedUpdate,
        }
    }
}