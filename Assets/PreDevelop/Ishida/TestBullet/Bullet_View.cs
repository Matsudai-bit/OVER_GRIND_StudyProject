using UnityEngine;
using System;

public class Bullet_View : MonoBehaviour
{
    // Presenterへ「当たったこと」を知らせるイベント
    public event Action<int> OnDamage;

    [SerializeField] private ParticleSystem _destroyEffect;

    private float _speed;
    private Vector3 _direction = Vector3.forward;

    private void Update()
    {
        // Modelが持つSpeedを使って移動する（Presenterから渡された値を保持）
        transform.position += _direction * _speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<ObstMove>(out var damageable))
        {
       //     OnDamage?.Invoke(damageable.attack); 
        }
    }

    // Presenterから呼ばれて初期化される
    public void Initialize(float speed, Vector3 direction)
    {
        _speed = speed;
        _direction = direction.normalized;
    }

    public void FlashRed(){}


}