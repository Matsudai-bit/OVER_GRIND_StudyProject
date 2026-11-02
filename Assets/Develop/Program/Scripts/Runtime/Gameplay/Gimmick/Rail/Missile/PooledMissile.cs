using System;
using UnityEngine;

/// <summary>
/// ミサイル本体をプールから取得・返却するための共通コンポーネントです。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class PooledMissile : MonoBehaviour
{
    // 返却先
    private Action<PooledMissile> m_releaseCallback;

    // Rigidbody
    private Rigidbody m_rigidbody;

    // 貸出中か
    private bool m_isSpawned;

    /// <summary>現在プールから貸し出されているか取得します。</summary>
    public bool IsSpawned => m_isSpawned;

    private void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody>();
    }

    /// <summary>返却先を設定します。</summary>
    public void Prepare(Action<PooledMissile> releaseCallback)
    {
        m_releaseCallback = releaseCallback;
    }

    /// <summary>物理状態を初期化してミサイルを有効化します。</summary>
    public bool Spawn(Vector3 position, Quaternion rotation)
    {
        if (m_isSpawned || m_rigidbody == null || m_releaseCallback == null)
        {
            return false;
        }

        // 前回の飛行速度を次の発射に持ち越しません。
        m_rigidbody.linearVelocity = Vector3.zero;
        m_rigidbody.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(position, rotation);

        m_isSpawned = true;
        gameObject.SetActive(true);
        return true;
    }

    /// <summary>ミサイルをプールに返却します。</summary>
    public void Release()
    {
        if (!m_isSpawned)
        {
            return;
        }

        // 再入や二重返却を防止します。
        m_isSpawned = false;
        m_rigidbody.linearVelocity = Vector3.zero;
        m_rigidbody.angularVelocity = Vector3.zero;
        gameObject.SetActive(false);
        m_releaseCallback?.Invoke(this);
    }
}
