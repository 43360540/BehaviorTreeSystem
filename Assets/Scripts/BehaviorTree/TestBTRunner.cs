using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree
{
    public class TestBTRunner : MonoBehaviour
    {
        [SerializeField] private NodeStatus _status = NodeStatus.Failure;
        [SerializeField] private BlackBoardMono _bb = null;

        private float _time = 0;
        private INode _root = null;

        private void Awake()
        {
            _root =
            new SelectorNode();
        }

        private void Update()
        {
            _time = Time.deltaTime;
            _status = _root.Tick(_bb, _time);
        }
    }
}