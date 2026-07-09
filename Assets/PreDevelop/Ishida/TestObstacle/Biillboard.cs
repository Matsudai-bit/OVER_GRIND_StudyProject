using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                          _mainCamera.transform.rotation * Vector3.up);
    }
}