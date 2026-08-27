// コレクションを扱うために使用します。
using System.Collections.Generic;

// Unityの基本機能を使用するために使用します。
using UnityEngine;

// URPのDecalProjectorを使用するために使用します。
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

    // Rayの判定距離に追加する余裕距離です。
    private const float RAY_DISTANCE_PADDING = 0.3f;

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

    // 同じコライダーへの連続ヒットを防ぐための待機時間です。
    [SerializeField] private float m_hitCooldown = 0.15f;

    // コライダーごとの前回位置を保持します。
    private readonly Dictionary<int, Vector3> m_lastPositions = new();

    // コライダーごとの前回ヒット時刻を保持します。
    private readonly Dictionary<int, float> m_lastHitTime = new();

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
    /// トリガーへの侵入を検知してデカール生成処理を行います。
    /// </summary>
    /// <param name="other">トリガーへ侵入したコライダー。</param>
    private void OnTriggerEnter(Collider other)
    {
        // 相手のコライダーが存在しない場合は処理を終了します。
        if (other == null)
        {
            return;
        }

        // 相手のタグが設定された武器タグと異なる場合は処理を終了します。
        if (!other.CompareTag(m_weaponTag))
        {
            return;
        }

        // コライダーを識別するためのInstanceIDを取得します。
        int id = other.GetInstanceID();

        // 前回のヒット時刻が存在するか確認します。
        if (m_lastHitTime.TryGetValue(id, out float lastTime))
        {
            // 前回のヒットからクールダウン時間が経過していない場合は処理を終了します。
            if (Time.time - lastTime < m_hitCooldown)
            {
                return;
            }
        }

        // 今回のヒット時刻を記録します。
        m_lastHitTime[id] = Time.time;

        // トリガー情報を利用してデカールを生成します。
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
            // どのWallで設定漏れが発生したか分かるようにオブジェクト名を表示します。
            Debug.LogWarning($"{name}: DecalPrefab が設定されていません。", this);

            // デカールを生成できないため処理を終了します。
            return;
        }

        // Wallに設定されているコライダーを取得します。
        Collider wallCollider = GetComponent<Collider>();

        // 壁面上のおおよその位置を取得します。
        Vector3 approxPoint = wallCollider != null
            ? wallCollider.ClosestPoint(other.bounds.center)
            : other.ClosestPoint(transform.position);

        // 前回位置が存在する場合はRayの始点として使用します。
        Vector3 rayOrigin = m_lastPositions.TryGetValue(id, out Vector3 prevPos)
            ? prevPos
            : other.bounds.center;

        // Rayの方向を「武器の前回位置から壁面のおおよその位置」へ向けます。
        Vector3 rayDirection = approxPoint - rayOrigin;

        // Rayの方向がほぼゼロの場合でも安全に処理できるように初期値を設定します。
        Vector3 rayDir = rayDirection.sqrMagnitude > RAY_DIRECTION_EPSILON
            ? rayDirection.normalized
            : Vector3.zero;

        // Rayが進む距離として起点から壁面までの距離を取得します。
        float rayDistance = rayDirection.magnitude + RAY_DISTANCE_PADDING;

        // Raycastが失敗した場合に使用する衝突点を仮設定します。
        Vector3 hitPoint = approxPoint;

        // Raycastが失敗した場合に使用する法線を仮設定します。
        Vector3 hitNormal = rayDir.sqrMagnitude > RAY_DIRECTION_EPSILON
            ? -rayDir
            : transform.forward;

        // 壁のコライダーがBoxColliderの場合は専用の法線計算を使用します。
        if (wallCollider is BoxCollider boxCollider)
        {
            // BoxColliderの面情報から正確な法線を取得します。
            hitNormal = GetBoxFaceNormal(boxCollider, approxPoint);
        }
        // BoxCollider以外のコライダーはRaycastによって衝突情報を取得します。
        else if (wallCollider != null &&
                 rayDir.sqrMagnitude > RAY_DIRECTION_EPSILON &&
                 Physics.Raycast(
                     rayOrigin,
                     rayDir,
                     out RaycastHit hit,
                     rayDistance) &&
                 hit.collider == wallCollider)
        {
            // Rayが実際に壁へ当たった位置を取得します。
            hitPoint = hit.point;

            // Rayが壁へ当たった位置の法線を取得します。
            hitNormal = hit.normal;
        }

        // 武器の振り方向を取得するための初期方向を設定します。
        Vector3 swingDirection = Vector3.up;

        // 武器のコライダー自身から速度トラッカーを取得します。
        AttackHitboxVelocityTracker velocityTracker =
            other.GetComponent<AttackHitboxVelocityTracker>();

        // コライダー自身に存在しない場合は親オブジェクトから取得します。
        if (velocityTracker == null)
        {
            velocityTracker =
                other.GetComponentInParent<AttackHitboxVelocityTracker>();
        }

        // 速度トラッカーが見つからない場合は警告を表示します。
        if (velocityTracker == null)
        {
            Debug.LogWarning(
                $"{other.name}: AttackHitboxVelocityTracker が見つかりません。",
                other);
        }
        else
        {
            // 現在の武器速度をデバッグログに表示します。
            Debug.Log(
                $"{other.name}: Velocity={velocityTracker.Velocity}, " +
                $"sqrMagnitude={velocityTracker.Velocity.sqrMagnitude}");
        }

        // 武器の速度が十分に大きい場合だけ振り方向として使用します。
        if (velocityTracker != null &&
            velocityTracker.Velocity.sqrMagnitude > SWING_DELTA_EPSILON)
        {
            // 速度ベクトルを正規化して振り方向に変換します。
            swingDirection = velocityTracker.Velocity.normalized;
        }

        // 次回のRay始点として使用するため現在の武器位置を保存します。
        m_lastPositions[id] = other.transform.position;

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

            // 法線と上方向が平行な場合は別の方向を使用します。
            if (Mathf.Abs(Vector3.Dot(hitNormal, projectedSwing)) > 1.0f - PROJECTED_SWING_EPSILON)
            {
                // 上方向と平行にならないよう右方向を使用します。
                projectedSwing = Vector3.right;
            }

            // 仮の上方向を壁面へ投影します。
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