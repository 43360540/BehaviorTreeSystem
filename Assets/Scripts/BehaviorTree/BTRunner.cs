using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree
{
    public class BTRunner : MonoBehaviour
    {
        [SerializeField] private NodeStatus _status = NodeStatus.Failure;
        [SerializeField] private BlackBoardMono _bb = null;

        private List<INode> _children = new();

        private SelectorNode _root = null;

        private void Awake()
        {
            foreach (Transform c in transform)
            {
                _children.Add(c.GetComponent<INode>());
            }

            _root = new SelectorNode(_children.ToArray());
        }

        private void Start()
        {
            _root.Reset();
        }

        private void Update()
        {
            _status = _root.Tick(_bb, Time.deltaTime);
        }
    }
}