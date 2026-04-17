using UnityEngine;

public class PlayerCtrl : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5;
    private InputSystem_Actions _input;
    [SerializeField] private Rigidbody _rb;

    private void Awake()
    {
        _input = new();
        _input.Player.Enable();
    }

    private void Update()
    {
        Vector3 v = Vector3.zero;
        Vector2 moveInput = _input.Player.Move.ReadValue<Vector2>();

        if (moveInput != Vector2.zero)
        {
            v = Vector3.Normalize(
                new Vector3(moveInput.x, 0f, moveInput.y)) * _moveSpeed;
        }

        _rb.linearVelocity = v;
    }
}
