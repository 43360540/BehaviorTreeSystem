using UnityEngine;

namespace BehaviorTree
{
    public abstract class MonoBTRunner<TContext> : MonoBehaviour
    {
        [SerializeField] private bool _debugMode = false;
        [SerializeField] private float _debugDuration = 3f;
        [SerializeField] private Rate _tickRate = Rate.Update;
        [SerializeField] private TContext _context;

        public Rate TickRate => _tickRate;
        public TContext Context => _context;
        public INode<TContext> Tree => _bTRunner?.Tree;

        protected abstract INode<TContext> CreateTree();

        private BTRunner<TContext> _bTRunner;
        private IReadOnlyNode _rTree;
        private float _debugTimer = 0f; 

        protected virtual void Awake()
        {
            if (_context == null)
                Debug.LogError($"[{GetType().Name}] Context not set. Use Inspector or SetContext() before Start(). ({gameObject.name})");
        }

        protected virtual void Start()
        {
            _bTRunner = new (_context, CreateTree());
            _rTree = _bTRunner.Tree as IReadOnlyNode;

            if (_rTree == null)
                Debug.LogError($"You may be using a customized Node. Please make sure it has implemented {nameof(IReadOnlyNode)}.");
        }

        protected virtual void Update()
        {
            if (TickRate != Rate.Update)
                return;
            _bTRunner?.Tick(Time.deltaTime);
        }

        protected virtual void LateUpdate()
        {
            if (_debugMode && _debugTimer >= _debugDuration)
            {
                Debug.LogWarning(BTDebugger.DrawTree(_rTree));
                _debugTimer = 0f;
            }
            _debugTimer += Time.deltaTime;
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