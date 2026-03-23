using UnityEngine;

public class Move : MonoBehaviour
{
    [SerializeField] private Vector3 _want = Vector3.zero;
    [SerializeField] private float _speed = 0.1f;

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
