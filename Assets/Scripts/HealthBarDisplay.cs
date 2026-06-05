using UnityEngine;

public class HealthBarDisplay : MonoBehaviour
{
    private Transform _camera;

    private void Awake()
    {
        _camera = Camera.main.transform;
    }

    private void LateUpdate()
    {
        transform.rotation = _camera.rotation;
    }
}
