using UnityEngine;

namespace BehaviorTree
{
    public abstract class MonoBTRunner<TContext> : MonoBehaviour
    {
        [SerializeField] private bool _debugMode = false;
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
            if (_debugMode)
                Debug.LogWarning(_bTRunner.PrintTree(Tree as IReadOnlyNode));
        }

        protected virtual void Update()
        {
            if (_debugMode)
                Debug.LogWarning(_bTRunner.PrintTree(Tree as IReadOnlyNode));
                
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
        // !! Must be use before Start() !! 
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