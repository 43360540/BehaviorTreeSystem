using UnityEngine;
using UnityEngine.InputSystem;

public class Move : MonoBehaviour
{
    [SerializeField] private Vector3 _want = Vector3.zero;
    [SerializeField] private float _speed = 0.1f;

    private InputAction _walkAction;

    private void Awake()
    {
        _walkAction = InputSystem.actions.FindAction("Move");
    }

    private void Update()
    {
        transform.position += _speed * Time.deltaTime * _want;
        _want = Vector3.zero;
    }

    public void WantDirection(Vector3 dir)
    {
        _want = dir;
    }
}
