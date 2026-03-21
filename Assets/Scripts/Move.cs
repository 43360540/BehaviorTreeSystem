using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Move : MonoBehaviour
{
    [SerializeField] private Vector3 _want = Vector3.zero;
    [SerializeField] private float _speed = 0.1f;
    [SerializeField] private Direction _direcrion;

    private void Update()
    {
        transform.position += _want * Time.deltaTime;
        _want = Vector3.zero;
    }

    public void WantDirection(Vector3 dir)
    {
        _want = dir;
    }
}

public enum Direction
{
    Forward,
    Back,
    Left,
    Right,
}
