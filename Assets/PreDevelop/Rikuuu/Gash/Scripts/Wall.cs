using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 壁オブジェクトにアタッチし、武器のトリガー衝突を検知してダメージデカールを生成します。
/// </summary>
public class Wall : MonoBehaviour
{
    // Rayの方向ベクトルが有効か判定するための最小値です。
    private const float RAY_DIRECTION_EPSILON = 0.0001f;

    // 武器の移動速度が有効か判定するための最小値です。
    private const float SWING_DELTA_EPSILON = 0.0001f;

    // 平面へ投影した振り方向が有効か判定するための最小値です。
    private const float PROJECTED_SWING_EPSILON = 0.001f;

    // デカールのZ方向ピボットをサイズの半分に設定するための比率です。
    private const float DECAL_PIVOT_Z_RATIO = 0.5f;

    // ゼロ除算を防ぐために使用する最小サイズです。
    private const float BOX_SIZE_EPSILON = 0.0001f;

    // デカールを生成するためのプレハブです。
    [SerializeField] private GameObject m_decalPrefab;

    // デカールを壁面から浮かせる距離です。
    [SerializeField] private float m_surfaceOffset = 0.02f;

    // 武器として判定するオブジェクトのタグです。
    [SerializeField] private string m_weaponTag = "Chainsaw";

    // 生成したデカールを壁の子オブジェクトにするかどうかです。
    [SerializeField] private bool m_parentDecalToSelf = true;

    // 同じコライダーへの連続ヒットを防ぐための待機時間です（現状未使用）。
    [SerializeField] private float m_hitCooldown = 0.15f;

    /// <summary>
    /// 接触したオブジェクト。
    /// </summary>
    private Collider m_contactCollider;

    /// <summary>
    /// 生成するダメージデカールのプレハブを取得します。
    /// </summary>
    public GameObject DecalPrefab => m_decalPrefab;

    /// <summary>
    /// デカールを壁面から浮かせる距離を取得します。
    /// </summary>
    public float SurfaceOffset => m_surfaceOffset;

    /// <summary>
    /// 武器として判定するオブジェクトのタグを取得します。
    /// </summary>
    public string WeaponTag => m_weaponTag;

    /// <summary>
    /// 接触したオブジェクト
    /// </summary>

    /// <summary>
    /// トリガーへの侵入を検知してデカール生成処理を行います。
    /// </summary>
    /// <param name="other">トリガーへ侵入したコライダー。</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
        {
            return;
        }

        if (!other.CompareTag(m_weaponTag))
        {
            return;
        }

        // 接触したColliderを保持します。
        m_contactCollider = other;
    }

    /// <summary>
    /// コライダーが離れたら、そのコライダーのヒット済み状態をリセットします。
    /// </summary>
    /// <param name="other">トリガーから離れたコライダー。</param>
    private void OnTriggerExit(Collider other)
    {
        if (other == null)
        {
            return;
        }

        // 保持しているColliderと離れたColliderが
        // 同一の場合のみデカール生成処理を行います。
        if (other != m_contactCollider)
        {
            return;
        }

        // 保持していたColliderを使ってデカールを生成します。
        HandleTriggerContact(other);

        // 接触状態を解除します。
        m_contactCollider = null;
    }

    /// <summary>
    /// 対象タグかどうかとヒット済みかどうかを判定し、未ヒットならデカール生成処理を呼び出します。
    /// </summary>
    /// <param name="other">判定対象のコライダー。</param>
    private void HandleTriggerContact(Collider other)
    {
        // nullチェックとタグ一致チェック（対象外なら何もしません）。
        if (other == null || !other.CompareTag(m_weaponTag))
        {
            return;
        }

        // コライダーごとの識別にInstanceIDを使用します。
        int id = other.GetInstanceID();

        // デカール生成処理を実行します。
        SpawnDecalFromTrigger(other, id);
    }

    /// <summary>
    /// トリガー衝突情報から壁面の衝突点、法線、振り方向を取得します。
    /// </summary>
    /// <param name="other">接触した武器のコライダー。</param>
    /// <param name="id">武器コライダーの識別ID。</param>
    private void SpawnDecalFromTrigger(Collider other, int id)
    {
        // デカールプレハブが設定されていない場合は警告を出して処理を終了します。
        if (m_decalPrefab == null)
        {
            Debug.LogWarning($"{name}: DecalPrefab が設定されていません。", this);
            return;
        }

        // Wallに設定されているコライダーを取得します。
        Collider wallCollider = GetComponent<Collider>();

        // 武器の現在位置を取得します（bounds.centerではなくtransform.positionを使用します）。
        Vector3 weaponPosition = other.transform.position;

        // 壁面上の最近接点を取得します（Convexなコライダーである必要があります）。
        Vector3 approxPoint = wallCollider != null
            ? wallCollider.ClosestPoint(weaponPosition)
            : weaponPosition;

        // 衝突点の初期値を設定します。
        Vector3 hitPoint = approxPoint;

        // 法線の初期値を壁の正面方向で設定します。
        Vector3 hitNormal = transform.forward;

        if (wallCollider is BoxCollider boxCollider)
        {
            // BoxColliderの場合は面情報から正確な法線を取得します。
            hitNormal = GetBoxFaceNormal(boxCollider, approxPoint);
        }
        else if (wallCollider != null)
        {
            // BoxCollider以外（円柱などの凸形状）は、
            // 「武器位置→表面最近接点」の逆方向を外向き法線として使用します。
            // 過去の位置履歴やRaycastに依存しないため、攻撃をまたいだ状態汚染が起きません。
            Vector3 outwardDirection = weaponPosition - approxPoint;

            // ベクトルの長さが十分にある場合のみ法線として採用します。
            if (outwardDirection.sqrMagnitude > RAY_DIRECTION_EPSILON)
            {
                hitNormal = outwardDirection.normalized;
            }
        }

        // 武器の振り方向を取得するための初期方向を設定します。
        Vector3 swingDirection = Vector3.up;

        // 武器のコライダー自身から速度トラッカーを取得します。
        AttackHitboxVelocityTracker velocityTracker =
            other.GetComponent<AttackHitboxVelocityTracker>();

        // コライダー自身になければ親オブジェクトから探します。
        if (velocityTracker == null)
        {
            velocityTracker = other.GetComponentInParent<AttackHitboxVelocityTracker>();
        }

        // 速度トラッカーが見つかり、かつ速度が十分にある場合は振り方向として採用します。
        if (velocityTracker != null &&
            velocityTracker.Velocity.sqrMagnitude > SWING_DELTA_EPSILON)
        {
            swingDirection = velocityTracker.Velocity.normalized;
        }

        // 取得した衝突情報を利用してデカールを生成します。
        SpawnDecal(hitPoint, hitNormal, swingDirection);
    }

    /// <summary>
    /// 衝突点、法線、振り方向を利用してデカールを生成します。
    /// </summary>
    /// <param name="hitPoint">壁面上の衝突点。</param>
    /// <param name="hitNormal">壁面の外向き法線。</param>
    /// <param name="swingDirection">武器の振り方向。</param>
    private void SpawnDecal(
        Vector3 hitPoint,
        Vector3 hitNormal,
        Vector3 swingDirection)
    {
        // 衝突点から法線方向へ少し離した位置をデカールの生成位置にします。
        Vector3 spawnPosition = hitPoint + hitNormal * m_surfaceOffset;

        // 振り方向を壁面に沿う方向へ投影します。
        Vector3 projectedSwing =
            Vector3.ProjectOnPlane(swingDirection, hitNormal);

        // 投影したベクトルが十分な長さを持っているか確認します。
        if (projectedSwing.sqrMagnitude > PROJECTED_SWING_EPSILON)
        {
            // 投影結果を正規化してデカールの上方向として使用します。
            projectedSwing.Normalize();
        }
        else
        {
            // 投影結果がほぼゼロの場合は上方向を仮の上方向として使用します。
            projectedSwing = Vector3.up;

            // 法線と上方向がほぼ平行な場合は別の方向を使用します。
            if (Mathf.Abs(Vector3.Dot(hitNormal, projectedSwing)) > 1.0f - PROJECTED_SWING_EPSILON)
            {
                // 上方向と平行にならないよう右方向を使用します。
                projectedSwing = Vector3.right;
            }

            // 仮の上方向を壁面へ投影して正規化します。
            projectedSwing =
                Vector3.ProjectOnPlane(projectedSwing, hitNormal).normalized;
        }

        // デカールの正面を法線の反対方向へ向け、上方向を振り方向に合わせます。
        Quaternion decalRotation =
            Quaternion.LookRotation(-hitNormal, projectedSwing);

        // デカールプレハブを指定した位置と回転で生成します。
        GameObject decal =
            Instantiate(m_decalPrefab, spawnPosition, decalRotation);

        // 生成したデカールからDecalProjectorを取得します。
        DecalProjector projector =
            decal.GetComponentInChildren<DecalProjector>();

        // DecalProjectorが存在する場合だけピボットを変更します。
        if (projector != null)
        {
            // 現在のピボット値を取得します。
            Vector3 pivot = projector.pivot;

            // Z方向のピボットをデカールの奥行き半分に設定します。
            pivot.z = projector.size.z * DECAL_PIVOT_Z_RATIO;

            // 変更したピボットをDecalProjectorへ設定します。
            projector.pivot = pivot;
        }

        // デカールをWallの子オブジェクトにする設定の場合に処理します。
        if (m_parentDecalToSelf)
        {
            // 生成したデカールをWallの子オブジェクトに設定します。
            decal.transform.SetParent(transform);
        }
    }

    /// <summary>
    /// BoxColliderの形状から指定位置に最も近い面の法線を取得します。
    /// </summary>
    /// <param name="box">対象のBoxCollider。</param>
    /// <param name="worldPoint">法線を求めたいワールド座標。</param>
    /// <returns>ワールド空間での外向き法線。</returns>
    private Vector3 GetBoxFaceNormal(
        BoxCollider box,
        Vector3 worldPoint)
    {
        // ワールド座標をBoxColliderのローカル座標へ変換します。
        Vector3 localPoint =
            box.transform.InverseTransformPoint(worldPoint);

        // BoxColliderのCenterを基準とした座標へ変換します。
        localPoint -= box.center;

        // BoxColliderの各軸方向の半分のサイズを取得します。
        Vector3 halfSize = box.size * 0.5f;

        // X軸方向の境界への近さを比率として計算します。
        float xRatio =
            Mathf.Abs(localPoint.x) /
            Mathf.Max(halfSize.x, BOX_SIZE_EPSILON);

        // Y軸方向の境界への近さを比率として計算します。
        float yRatio =
            Mathf.Abs(localPoint.y) /
            Mathf.Max(halfSize.y, BOX_SIZE_EPSILON);

        // Z軸方向の境界への近さを比率として計算します。
        float zRatio =
            Mathf.Abs(localPoint.z) /
            Mathf.Max(halfSize.z, BOX_SIZE_EPSILON);

        // 算出した比率を比較するためのローカル法線を宣言します。
        Vector3 localNormal;

        // X軸方向の比率が最も大きい場合はX面を法線として使用します。
        if (xRatio >= yRatio && xRatio >= zRatio)
        {
            // X座標の符号から左右どちらの面かを判定します。
            float sign = localPoint.x >= 0.0f ? 1.0f : -1.0f;

            // X方向の法線を設定します。
            localNormal = new Vector3(sign, 0.0f, 0.0f);
        }
        // Y軸方向の比率が最も大きい場合はY面を法線として使用します。
        else if (yRatio >= xRatio && yRatio >= zRatio)
        {
            // Y座標の符号から上下どちらの面かを判定します。
            float sign = localPoint.y >= 0.0f ? 1.0f : -1.0f;

            // Y方向の法線を設定します。
            localNormal = new Vector3(0.0f, sign, 0.0f);
        }
        else
        {
            // Z座標の符号から前後どちらの面かを判定します。
            float sign = localPoint.z >= 0.0f ? 1.0f : -1.0f;

            // Z方向の法線を設定します。
            localNormal = new Vector3(0.0f, 0.0f, sign);
        }

        // ローカル空間の法線をワールド空間へ変換します。
        Vector3 worldNormal =
            box.transform.TransformDirection(localNormal);

        // 法線を正規化して返します。
        return worldNormal.normalized;
    }
}