using System.Collections;
using UnityEngine;

/// <summary>
/// CollisionSensorの検知時にGameplay Cueの開始要求を送信します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CollisionSensor))]
public sealed class CollisionCueRequestNotifier : MonoBehaviour
{
    [SerializeField, Header("Cueリクエストイベント")]
    private GameplayCueRequestEvent m_cueRequestEvent;

    [SerializeField, Header("検知時に開始するCue")]
    private GameplayCueAsset m_cue;

    [SerializeField, Header("物理クエリ補完対象Layer")]
    private LayerMask m_fallbackTargetLayerMask;

    private CollisionSensor m_collisionSensor;
    private Collider m_sensorCollider;
    private bool m_hasLoggedTriggerStay;
    private bool m_hasRaisedForCurrentOverlap;

    /// <summary>
    /// CollisionSensorを取得し、検知イベントを購読します。
    /// </summary>
    private void Awake()
    {
        m_collisionSensor = GetComponent<CollisionSensor>();
        m_sensorCollider = GetComponent<Collider>();

        Collider collider = m_sensorCollider;
        if (collider != null && !collider.providesContacts)
        {
            collider.providesContacts = true;

            Debug.Log(
                $"[{nameof(CollisionCueRequestNotifier)}] " +
                "ColliderのProvides Contactsを有効化しました。",
                this);
        }
    }

    /// <summary>
    /// 検知イベントの購読を開始します。
    /// </summary>
    private void OnEnable()
    {
        m_hasLoggedTriggerStay = false;

        if (m_collisionSensor == null)
        {
            m_collisionSensor = GetComponent<CollisionSensor>();
        }

        Collider collider = GetComponent<Collider>();
        Debug.Log(
            $"[{nameof(CollisionCueRequestNotifier)}] 有効化されました。" +
            $" GameObject: {gameObject.name}," +
            $" ActiveInHierarchy: {gameObject.activeInHierarchy}," +
            $" Collider: {collider?.GetType().Name ?? "なし"}," +
            $" IsTrigger: {collider?.isTrigger.ToString() ?? "不明"}," +
            $" CollisionSensor: {m_collisionSensor != null}",
            this);

        StartCoroutine(LogPhysicsStateAfterFixedUpdate());

        if (m_collisionSensor == null)
        {
            Debug.LogWarning(
                $"{nameof(CollisionSensor)}が設定されていません。",
                this);
            return;
        }

        m_collisionSensor.CollisionDetected += OnCollisionDetected;
    }

    /// <summary>
    /// 検知イベントの購読を解除します。
    /// </summary>
    private void OnDisable()
    {
        StopAllCoroutines();
        m_hasRaisedForCurrentOverlap = false;

        if (m_collisionSensor != null)
        {
            m_collisionSensor.CollisionDetected -= OnCollisionDetected;
        }
    }

    /// <summary>
    /// CollisionSensorのTrigger通知を補完するため、物理範囲内の対象を確認します。
    /// </summary>
    private void FixedUpdate()
    {
        if (m_sensorCollider == null || !m_sensorCollider.enabled)
        {
            return;
        }

        int targetLayerMask = m_fallbackTargetLayerMask.value;
        if (targetLayerMask == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer < 0)
            {
                return;
            }

            targetLayerMask = 1 << playerLayer;
        }

        Bounds bounds = m_sensorCollider.bounds;
        Collider[] overlaps = Physics.OverlapBox(
            bounds.center,
            bounds.extents,
            transform.rotation,
            targetLayerMask,
            QueryTriggerInteraction.Collide);

        bool hasTarget = false;
        foreach (Collider other in overlaps)
        {
            if (other == null || other == m_sensorCollider)
            {
                continue;
            }

            hasTarget = true;
            if (!m_hasRaisedForCurrentOverlap)
            {
                OnCollisionDetected(
                    other.ClosestPointOnBounds(transform.position));
            }

            break;
        }

        if (!hasTarget)
        {
            FinishCurrentCue();
            m_hasRaisedForCurrentOverlap = false;
        }
    }

    /// <summary>
    /// 対象範囲から出たときに、現在のCueを終了します。
    /// </summary>
    private void FinishCurrentCue()
    {
        if (!m_hasRaisedForCurrentOverlap ||
            m_cueRequestEvent == null ||
            m_cue == null)
        {
            return;
        }

        m_cueRequestEvent.RaiseFinish(m_cue);
        Debug.Log(
            $"[{nameof(CollisionCueRequestNotifier)}] " +
            $"対象範囲を退出しました。 Cue: {m_cue.name}",
            this);
    }

    /// <summary>
    /// 初回物理更新後の実位置とOverlap状態を確認します。
    /// </summary>
    private IEnumerator LogPhysicsStateAfterFixedUpdate()
    {
        yield return new WaitForFixedUpdate();

        Collider sensorCollider = GetComponent<Collider>();
        if (sensorCollider == null)
        {
            yield break;
        }

        Bounds bounds = sensorCollider.bounds;
        Collider[] overlaps = Physics.OverlapBox(
            bounds.center,
            bounds.extents,
            transform.rotation,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide);

        string overlapNames = overlaps.Length == 0
            ? "なし"
            : string.Join(", ", GetColliderNames(overlaps));

        Debug.Log(
            $"[{nameof(CollisionCueRequestNotifier)}] 物理状態。" +
            $" SensorPosition: {transform.position}," +
            $" BoundsCenter: {bounds.center}," +
            $" BoundsSize: {bounds.size}," +
            $" OverlapColliders: {overlapNames}," +
            $" Default-Player Collision: " +
            $"{Physics.GetIgnoreLayerCollision(gameObject.layer, 8) == false}",
            this);
    }

    /// <summary>
    /// OverlapしたCollider名の一覧を作成します。
    /// </summary>
    /// <param name="colliders">OverlapしたCollider。</param>
    /// <returns>Collider名の一覧。</returns>
    private static string[] GetColliderNames(Collider[] colliders)
    {
        string[] names = new string[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            names[i] = $"{colliders[i].name}(Layer:{colliders[i].gameObject.layer})";
        }

        return names;
    }

    /// <summary>
    /// CollisionSensorと同じTrigger通知を受け取り、物理判定の到達を確認します。
    /// </summary>
    /// <param name="other">Triggerへ侵入したCollider。</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
        {
            return;
        }

        Debug.Log(
            $"[{nameof(CollisionCueRequestNotifier)}] " +
            $"Trigger callbackを受信しました。" +
            $" Other: {other.name}," +
            $" Layer: {LayerMask.LayerToName(other.gameObject.layer)} " +
            $"({other.gameObject.layer})," +
            $" Attached Rigidbody: " +
            $"{other.attachedRigidbody?.name ?? "なし"}",
            this);
    }

    /// <summary>
    /// 侵入開始通知を取り逃して範囲内にいる場合の診断を一度だけ行います。
    /// </summary>
    /// <param name="other">Trigger内に存在するCollider。</param>
    private void OnTriggerStay(Collider other)
    {
        if (m_hasLoggedTriggerStay || other == null)
        {
            return;
        }

        m_hasLoggedTriggerStay = true;
        Debug.Log(
            $"[{nameof(CollisionCueRequestNotifier)}] " +
            $"Trigger stayを受信しました。" +
            $" Other: {other.name}," +
            $" Layer: {LayerMask.LayerToName(other.gameObject.layer)} " +
            $"({other.gameObject.layer})",
            this);
    }

    /// <summary>
    /// 対象範囲への侵入時にCue開始要求を送信します。
    /// </summary>
    /// <param name="hitPosition">検知した衝突位置。</param>
    private void OnCollisionDetected(Vector3 hitPosition)
    {
        if (m_hasRaisedForCurrentOverlap)
        {
            return;
        }

        m_hasRaisedForCurrentOverlap = true;

        Debug.Log(
            $"[{nameof(CollisionCueRequestNotifier)}] " +
            $"対象範囲を検知しました。" +
            $" GameObject: {gameObject.name}," +
            $" Position: {hitPosition}," +
            $" Cue: {m_cue?.name ?? "未設定"}",
            this);

        if (m_cueRequestEvent == null)
        {
            Debug.LogWarning(
                $"{nameof(GameplayCueRequestEvent)}が設定されていません。",
                this);
            return;
        }

        if (m_cue == null)
        {
            Debug.LogWarning(
                $"{nameof(GameplayCueAsset)}が設定されていません。",
                this);
            return;
        }

        m_cueRequestEvent.RaiseStart(m_cue);
    }
}
