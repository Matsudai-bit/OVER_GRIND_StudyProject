using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 指定Layerとの衝突を検知して通知します。
/// </summary>
[DisallowMultipleComponent]
public sealed class CollisionSensor : MonoBehaviour
{
    // 現在接触している対象Collider
    private readonly HashSet<Collider> m_contactColliders =
        new HashSet<Collider>();

    // 衝突を検知するLayer
    [SerializeField]
    private LayerMask m_targetLayerMask;

    /// <summary>
    /// 対象との衝突を通知します。
    /// </summary>
    public event Action<Vector3> CollisionDetected;

    /// <summary>
    /// 対象と現在接触しているか取得します。
    /// </summary>
    public bool IsDetectingTarget =>
        m_contactColliders.Count > 0;

    /// <summary>
    /// 現在の接触位置を取得します。
    /// </summary>
    public Vector3 CurrentHitPosition { get; private set; }

    private void OnDisable()
    {
        m_contactColliders.Clear();
    }

    /// <summary>
    /// Triggerとの接触開始を検知します。
    /// </summary>
    /// <param name="other">接触したCollider。</param>
    private void OnTriggerEnter(Collider other)
    {
        if (!IsTargetLayer(other.gameObject.layer))
        {
            return;
        }

        Vector3 hitPosition =
            other.ClosestPointOnBounds(transform.position);

        RegisterContact(
            other,
            hitPosition);
    }

    /// <summary>
    /// Triggerとの接触中に接触位置を更新します。
    /// </summary>
    /// <param name="other">接触中のCollider。</param>
    private void OnTriggerStay(Collider other)
    {
        if (!m_contactColliders.Contains(other))
        {
            return;
        }

        CurrentHitPosition =
            other.ClosestPointOnBounds(
                transform.position);
    }

    /// <summary>
    /// Triggerとの接触終了を検知します。
    /// </summary>
    /// <param name="other">接触終了したCollider。</param>
    private void OnTriggerExit(Collider other)
    {
        UnregisterContact(other);
    }

    /// <summary>
    /// Collisionとの接触開始を検知します。
    /// </summary>
    /// <param name="collision">衝突情報。</param>
    private void OnCollisionEnter(
        Collision collision)
    {
        Collider other =
            collision.collider;

        if (!IsTargetLayer(
                other.gameObject.layer))
        {
            return;
        }

        Vector3 hitPosition =
            GetCollisionHitPosition(
                collision);

        RegisterContact(
            other,
            hitPosition);
    }

    /// <summary>
    /// Collisionとの接触中に接触位置を更新します。
    /// </summary>
    /// <param name="collision">衝突情報。</param>
    private void OnCollisionStay(
        Collision collision)
    {
        Collider other =
            collision.collider;

        if (!m_contactColliders.Contains(other))
        {
            return;
        }

        CurrentHitPosition =
            GetCollisionHitPosition(
                collision);
    }

    /// <summary>
    /// Collisionとの接触終了を検知します。
    /// </summary>
    /// <param name="collision">衝突情報。</param>
    private void OnCollisionExit(
        Collision collision)
    {
        UnregisterContact(
            collision.collider);
    }

    /// <summary>
    /// 対象との接触を登録します。
    /// </summary>
    /// <param name="collider">接触したCollider。</param>
    /// <param name="hitPosition">接触位置。</param>
    private void RegisterContact(
        Collider collider,
        Vector3 hitPosition)
    {
        CurrentHitPosition =
            hitPosition;

        /*
         * 同じColliderから重複してEnter通知された場合は
         * CollisionDetectedを重複発行しません。
         */
        if (!m_contactColliders.Add(collider))
        {
            return;
        }

        CollisionDetected?.Invoke(
            hitPosition);
    }

    /// <summary>
    /// 対象との接触登録を解除します。
    /// </summary>
    /// <param name="collider">接触終了したCollider。</param>
    private void UnregisterContact(
        Collider collider)
    {
        if (collider == null)
        {
            return;
        }

        m_contactColliders.Remove(
            collider);
    }

    /// <summary>
    /// Collisionから接触位置を取得します。
    /// </summary>
    /// <param name="collision">衝突情報。</param>
    /// <returns>接触位置。</returns>
    private Vector3 GetCollisionHitPosition(
        Collision collision)
    {
        if (collision.contactCount <= 0)
        {
            return transform.position;
        }

        return collision
            .GetContact(0)
            .point;
    }

    /// <summary>
    /// 対象Layerか確認します。
    /// </summary>
    /// <param name="layer">確認するLayer。</param>
    /// <returns>
    /// true：対象Layerです。
    /// false：対象Layerではありません。
    /// </returns>
    private bool IsTargetLayer(int layer)
    {
        return (m_targetLayerMask.value &
                (1 << layer)) != 0;
    }
}