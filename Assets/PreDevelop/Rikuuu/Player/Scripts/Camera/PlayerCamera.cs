using UnityEngine;
using Unity.Cinemachine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    private float sensitivity = 1.0f;
}
