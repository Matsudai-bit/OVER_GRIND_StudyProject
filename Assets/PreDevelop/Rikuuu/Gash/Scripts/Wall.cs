using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 壁オブジェクトにアタッチし、武器（チェーンソー）の
/// トリガー衝突を検知して壁面にダメージデカールを生成します。
/// </summary>
public class Wall : MonoBehaviour
{
    // Ray判定時の方向ベクトルが有効かどうかを判定するための閾値（ゼロベクトル対策）
    private const float RAY_DIRECTION_EPSILON = 0.0001f;

    // 振り方向の差分ベクトルが有効かどうかを判定するための閾値
    private const float SWING_DELTA_EPSILON = 0.0001f;

    // 平面投影後の振り方向が有効かどうかを判定するための閾値
    private const float PROJECTED_SWING_EPSILON = 0.001f;

    // Rayの飛距離に加える余裕距離（衝突点をわずかに超えて判定するため）
    private const float RAY_DISTANCE_PADDING = 0.3f;

    // DecalProjectorのピボットZ位置をサイズの何割に設定するかの比率（0.5＝中央）
    private const float DECAL_PIVOT_Z_RATIO = 0.5f;

    // 生成するダメージデカールのプレハブ
    [SerializeField] private GameObject m_decalPrefab;

    // デカールを壁面から浮かせるオフセット距離（Zファイティング防止用）
    [SerializeField] private float m_surfaceOffset = 0.02f;

    // トリガー判定対象とする武器のタグ名
    [SerializeField] private string m_weaponTag = "Chainsaw";

    // 生成したデカールをこの壁の子オブジェクトにするかどうか
    [SerializeField] private bool m_parentDecalToSelf = true;

    // 同一コライダーに対する連続ヒットを防ぐためのクールダウン時間（秒）
    [SerializeField] private float m_hitCooldown = 0.15f;

    // コライダーごとの直前フレーム位置（Rayの起点・振り方向の推定に使用）
    private readonly Dictionary<int, Vector3> m_lastPositions = new();

    // コライダーごとの最終ヒット時刻（クールダウン判定に使用）
    private readonly Dictionary<int, float> m_lastHitTime = new();

    /// <summary>
    /// 生成するダメージデカールのプレハブ。
    /// </summary>
    public GameObject DecalPrefab => m_decalPrefab;

    /// <summary>
    /// デカールを壁面から浮かせるオフセット距離。
    /// </summary>
    public float SurfaceOffset => m_surfaceOffset;

    /// <summary>
    /// トリガー判定対象とする武器のタグ名。
    /// </summary>
    public string WeaponTag => m_weaponTag;

    /// <summary>
    /// トリガーコライダーとの接触を検知し、対象タグの場合はデカール生成処理を行います。
    /// </summary>
    /// <param name="other">接触した相手のコライダー。</param>
    private void OnTriggerEnter(Collider other)
    {
        // nullチェックとタグ一致チェック（対象外なら何もしない）
        if (other == null || !other.CompareTag(m_weaponTag))
        {
            return;
        }

        // コライダーごとの識別にInstanceIDを使用
        int id = other.GetInstanceID();

        // 前回ヒット時刻からクールダウン時間内であれば処理をスキップ（ジッター対策）
        if (m_lastHitTime.TryGetValue(id, out float lastTime) &&
            Time.time - lastTime < m_hitCooldown)
        {
            return;
        }

        // 今回のヒット時刻を記録
        m_lastHitTime[id] = Time.time;

        // デカール生成処理を実行
        SpawnDecalFromTrigger(other, id);
    }

    /// <summary>
    /// トリガー衝突情報からRayを飛ばして正確な衝突点・法線を取得し、デカール生成に渡します。
    /// </summary>
    /// <param name="other">接触した相手のコライダー。</param>
    /// <param name="id">対象コライダーの識別ID。</param>
    private void SpawnDecalFromTrigger(Collider other, int id)
    {
        // デカールプレハブが未設定の場合は警告を出して処理を中断
        if (m_decalPrefab == null)
        {
            Debug.LogWarning($"{name}: DecalPrefab が設定されていません。", this);
            return;
        }

        // 自身（壁）のコライダーを取得
        Collider wallCollider = GetComponent<Collider>();

        // 壁コライダー上で武器に最も近い点を大まかな衝突点として仮決定
        Vector3 approxPoint = wallCollider != null
            ? wallCollider.ClosestPoint(other.bounds.center)
            : other.ClosestPoint(transform.position);

        // Rayの起点は前フレームの武器位置（記録がなければ現在の武器中心）
        Vector3 rayOrigin = m_lastPositions.TryGetValue(id, out Vector3 prevPos)
            ? prevPos
            : other.bounds.center;

        // Rayの方向は起点から仮衝突点へ向かうベクトル
        Vector3 rayDir = (approxPoint - rayOrigin).normalized;

        // Rayの飛距離は起点-仮衝突点間の距離に余裕分を加算
        float rayDistance = Vector3.Distance(rayOrigin, approxPoint) + RAY_DISTANCE_PADDING;

        // 衝突点・法線の初期値（フォールバック用）
        Vector3 hitPoint = approxPoint;
        Vector3 hitNormal = -rayDir;

        // 方向ベクトルが有効な場合のみRaycastを実行
        if (rayDir.sqrMagnitude > RAY_DIRECTION_EPSILON &&
            Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, rayDistance))
        {
            // Rayが壁コライダーに当たった場合のみ、正確な衝突点・法線で上書き
            if (wallCollider == null || hit.collider == wallCollider)
            {
                hitPoint = hit.point;
                hitNormal = hit.normal;
            }
        }

        // 振り方向の初期値（デフォルトは上向き）
        Vector3 swingDirection = Vector3.up;

        // 前フレーム位置が記録されていれば、移動差分から振り方向を推定
        if (m_lastPositions.TryGetValue(id, out Vector3 lastPos))
        {
            Vector3 delta = other.transform.position - lastPos;
            if (delta.sqrMagnitude > SWING_DELTA_EPSILON)
            {
                swingDirection = delta.normalized;
            }
        }

        // 今回の武器位置を次回計算用に記録
        m_lastPositions[id] = other.transform.position;

        // デカール生成処理を呼び出し
        SpawnDecal(hitPoint, hitNormal, swingDirection);
    }

    /// <summary>
    /// 衝突点・法線・振り方向をもとにデカールを生成し、壁面に沿って配置します。
    /// </summary>
    /// <param name="hitPoint">壁面上の衝突点。</param>
    /// <param name="hitNormal">壁面の外向き法線。</param>
    /// <param name="swingDirection">武器の振り方向。</param>
    private void SpawnDecal(Vector3 hitPoint, Vector3 hitNormal, Vector3 swingDirection)
    {
        // 法線方向にオフセットさせた生成位置を計算（Zファイティング防止）
        Vector3 spawnPosition = hitPoint + hitNormal * m_surfaceOffset;

        // 振り方向を壁面（法線に垂直な平面）に投影
        Vector3 projectedSwing = Vector3.ProjectOnPlane(swingDirection, hitNormal).normalized;

        // 投影結果がほぼゼロベクトルの場合は上向きにフォールバック
        if (projectedSwing.sqrMagnitude < PROJECTED_SWING_EPSILON)
        {
            projectedSwing = Vector3.up;
        }

        // 法線を正面、投影した振り方向を上として回転を決定
        Quaternion decalRotation = Quaternion.LookRotation(-hitNormal, projectedSwing);

        // デカールを生成
        GameObject decal = Instantiate(m_decalPrefab, spawnPosition, decalRotation);

        // 子オブジェクトからDecalProjectorコンポーネントを検索
        DecalProjector projector = decal.GetComponentInChildren<DecalProjector>();
        if (projector != null)
        {
            // 現在のピボット値を取得
            Vector3 pivot = projector.pivot;

            // Z方向のピボットをサイズの半分にずらす（投影の奥行き基準位置を調整）
            pivot.z = projector.size.z * DECAL_PIVOT_Z_RATIO;

            // 変更したピボット値を反映
            projector.pivot = pivot;
        }

        // 設定に応じてデカールをこの壁の子オブジェクトにする
        if (m_parentDecalToSelf)
        {
            decal.transform.SetParent(transform);
        }
    }
}