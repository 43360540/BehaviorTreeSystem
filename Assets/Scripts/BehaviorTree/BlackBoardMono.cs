using System;
using UnityEngine;
using UnityEngine.Events;

public class BlackBoardMono : MonoBehaviour
{
    [SerializeField] private UnityEvent<Vector3> _move = null;

    public void MoveAction(Vector3 dir)
    {
        _move?.Invoke(dir);
    }
}