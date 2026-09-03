using UnityEngine;

/// <summary>
/// デバッグ表示する判定の種類。
/// </summary>
public enum HitboxDebugType
{
    AUTO,
    ATTACK,
    HURT,
    GUARD,
    INTERACTION,
    OTHER,
}

/// <summary>
/// デバッグ表示のタイミング。
/// </summary>
public enum HitboxDebugDisplayMode
{
    ALWAYS,
    SELECTED_ONLY
}

/// <summary>
/// Colliderのデバッグ表示を管理します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class HitboxDebugVisualizer : MonoBehaviour
{
    /// <summary>
    /// 表示対象のCollider。
    /// </summary>
    [SerializeField]
    private Collider m_targetCollider;

    /// <summary>
    /// 判定の種類。
    /// </summary>
    [SerializeField]
    private HitboxDebugType m_debugType = HitboxDebugType.AUTO;

    /// <summary>
    /// 表示するタイミング。
    /// </summary>
    [SerializeField]
    private HitboxDebugDisplayMode m_displayMode =
        HitboxDebugDisplayMode.ALWAYS;

    /// <summary>
    /// 無効なColliderを表示するか。
    /// </summary>
    [SerializeField]
    private bool m_showDisabledCollider = true;

    /// <summary>
    /// 無効時の透明度倍率。
    /// </summary>
    [SerializeField]
    [Range(0.05f, 1.0f)]
    private float m_disabledAlphaMultiplier = 0.25f;

    /// <summary>
    /// 攻撃判定の色。
    /// </summary>
    [SerializeField]
    private Color m_attackColor =
        new Color(1.0f, 0.15f, 0.15f, 1.0f);

    /// <summary>
    /// 被攻撃判定の色。
    /// </summary>
    [SerializeField]
    private Color m_hurtColor =
        new Color(0.15f, 1.0f, 0.15f, 1.0f);

    /// <summary>
    /// ガード判定の色。
    /// </summary>
    [SerializeField]
    private Color m_guardColor =
        new Color(0.15f, 0.5f, 1.0f, 1.0f);

    /// <summary>
    /// インタラクション判定の色。
    /// </summary>
    [SerializeField]
    private Color m_interactionColor =
        new Color(1.0f, 0.85f, 0.15f, 1.0f);

    /// <summary>
    /// その他の判定の色。
    /// </summary>
    [SerializeField]
    private Color m_otherColor = Color.white;

    /// <summary>
    /// 常時表示の場合にColliderを描画します。
    /// </summary>
    private void OnDrawGizmos()
    {
        if (m_displayMode != HitboxDebugDisplayMode.ALWAYS)
        {
            return;
        }

        DrawCollider();
    }

    /// <summary>
    /// 選択時表示の場合にColliderを描画します。
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (m_displayMode !=
            HitboxDebugDisplayMode.SELECTED_ONLY)
        {
            return;
        }

        DrawCollider();
    }

    /// <summary>
    /// Colliderの境界を描画します。
    /// </summary>
    private void DrawCollider()
    {
        CacheCollider();

        if (m_targetCollider == null)
        {
            return;
        }

        if (!m_targetCollider.enabled &&
            !m_showDisabledCollider)
        {
            return;
        }

        // Gizmosの状態を保存します。
        Color previousColor = Gizmos.color;
        Matrix4x4 previousMatrix = Gizmos.matrix;

        Gizmos.color = GetDisplayColor();
        Gizmos.matrix = Matrix4x4.identity;

        DrawColliderShape(m_targetCollider);

        // Gizmosの状態を元に戻します。
        Gizmos.color = previousColor;
        Gizmos.matrix = previousMatrix;
    }

    /// <summary>
    /// Colliderの種類に応じて境界を描画します。
    /// </summary>
    /// <param name="targetCollider">表示対象のCollider。</param>
    private void DrawColliderShape(Collider targetCollider)
    {
        if (targetCollider is BoxCollider boxCollider)
        {
            DrawBoxCollider(boxCollider);
            return;
        }

        if (targetCollider is SphereCollider sphereCollider)
        {
            DrawSphereCollider(sphereCollider);
            return;
        }

        if (targetCollider is CapsuleCollider capsuleCollider)
        {
            DrawCapsuleCollider(capsuleCollider);
            return;
        }

        if (targetCollider is MeshCollider meshCollider)
        {
            DrawMeshCollider(meshCollider);
            return;
        }

        // 未対応Colliderはワールド空間のBoundsで表示します。
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawWireCube(
            targetCollider.bounds.center,
            targetCollider.bounds.size);
    }

    /// <summary>
    /// BoxColliderを描画します。
    /// </summary>
    /// <param name="boxCollider">表示対象。</param>
    private void DrawBoxCollider(BoxCollider boxCollider)
    {
        Gizmos.matrix =
            boxCollider.transform.localToWorldMatrix;

        Gizmos.DrawWireCube(
            boxCollider.center,
            boxCollider.size);
    }

    /// <summary>
    /// SphereColliderを描画します。
    /// </summary>
    /// <param name="sphereCollider">表示対象。</param>
    private void DrawSphereCollider(
        SphereCollider sphereCollider)
    {
        Transform targetTransform = sphereCollider.transform;

        // SphereColliderは最大スケールを半径へ適用します。
        Vector3 scale = GetAbsoluteScale(
            targetTransform.lossyScale);

        float radiusScale = Mathf.Max(
            scale.x,
            Mathf.Max(scale.y, scale.z));

        Vector3 worldCenter =
            targetTransform.TransformPoint(
                sphereCollider.center);

        float worldRadius =
            sphereCollider.radius * radiusScale;

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawWireSphere(worldCenter, worldRadius);
    }

    /// <summary>
    /// CapsuleColliderを描画します。
    /// </summary>
    /// <param name="capsuleCollider">表示対象。</param>
    private void DrawCapsuleCollider(
        CapsuleCollider capsuleCollider)
    {
        Transform targetTransform =
            capsuleCollider.transform;

        Vector3 scale = GetAbsoluteScale(
            targetTransform.lossyScale);

        Vector3 capsuleAxis;
        float heightScale;
        float radiusScale;

        // CapsuleColliderの方向に合わせてスケールを取得します。
        switch (capsuleCollider.direction)
        {
            case 0:
                capsuleAxis = targetTransform.right;
                heightScale = scale.x;
                radiusScale = Mathf.Max(scale.y, scale.z);
                break;

            case 2:
                capsuleAxis = targetTransform.forward;
                heightScale = scale.z;
                radiusScale = Mathf.Max(scale.x, scale.y);
                break;

            default:
                capsuleAxis = targetTransform.up;
                heightScale = scale.y;
                radiusScale = Mathf.Max(scale.x, scale.z);
                break;
        }

        capsuleAxis.Normalize();

        Vector3 worldCenter =
            targetTransform.TransformPoint(
                capsuleCollider.center);

        float worldRadius =
            capsuleCollider.radius * radiusScale;

        float worldHeight = Mathf.Max(
            capsuleCollider.height * heightScale,
            worldRadius * 2.0f);

        float halfLineLength = Mathf.Max(
            0.0f,
            worldHeight * 0.5f - worldRadius);

        // 高さが直径以下の場合は球として表示します。
        if (halfLineLength <= Mathf.Epsilon)
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireSphere(
                worldCenter,
                worldRadius);

            return;
        }

        Vector3 topCenter =
            worldCenter + capsuleAxis * halfLineLength;

        Vector3 bottomCenter =
            worldCenter - capsuleAxis * halfLineLength;

        GetPerpendicularAxes(
            capsuleAxis,
            out Vector3 tangent,
            out Vector3 bitangent);

        Gizmos.matrix = Matrix4x4.identity;

        // 上下の丸みを表示します。
        Gizmos.DrawWireSphere(topCenter, worldRadius);
        Gizmos.DrawWireSphere(bottomCenter, worldRadius);

        // カプセル側面を表示します。
        Gizmos.DrawLine(
            topCenter + tangent * worldRadius,
            bottomCenter + tangent * worldRadius);

        Gizmos.DrawLine(
            topCenter - tangent * worldRadius,
            bottomCenter - tangent * worldRadius);

        Gizmos.DrawLine(
            topCenter + bitangent * worldRadius,
            bottomCenter + bitangent * worldRadius);

        Gizmos.DrawLine(
            topCenter - bitangent * worldRadius,
            bottomCenter - bitangent * worldRadius);
    }

    /// <summary>
    /// MeshColliderを描画します。
    /// </summary>
    /// <param name="meshCollider">表示対象。</param>
    private void DrawMeshCollider(
        MeshCollider meshCollider)
    {
        if (meshCollider.sharedMesh == null)
        {
            Gizmos.matrix = Matrix4x4.identity;

            Gizmos.DrawWireCube(
                meshCollider.bounds.center,
                meshCollider.bounds.size);

            return;
        }

        Gizmos.matrix =
            meshCollider.transform.localToWorldMatrix;

        Gizmos.DrawWireMesh(meshCollider.sharedMesh);
    }

    /// <summary>
    /// 判定種類に対応した色を取得します。
    /// </summary>
    /// <returns>デバッグ表示に使用する色。</returns>
    private Color GetDisplayColor()
    {
        HitboxDebugType resolvedType =
            ResolveDebugType();

        Color displayColor;

        switch (resolvedType)
        {
            case HitboxDebugType.ATTACK:
                displayColor = m_attackColor;
                break;

            case HitboxDebugType.HURT:
                displayColor = m_hurtColor;
                break;

            case HitboxDebugType.GUARD:
                displayColor = m_guardColor;
                break;

            case HitboxDebugType.INTERACTION:
                displayColor = m_interactionColor;
                break;

            default:
                displayColor = m_otherColor;
                break;
        }

        // 無効なColliderは半透明で表示します。
        if (m_targetCollider != null &&
            !m_targetCollider.enabled)
        {
            displayColor.a *= m_disabledAlphaMultiplier;
        }

        return displayColor;
    }

    /// <summary>
    /// 自動判定を含めた表示種類を取得します。
    /// </summary>
    /// <returns>表示に使用する判定種類。</returns>
    private HitboxDebugType ResolveDebugType()
    {
        if (m_debugType != HitboxDebugType.AUTO)
        {
            return m_debugType;
        }

        // 同じオブジェクトの攻撃判定を優先します。
        if (GetComponent<AttackHitbox>() != null)
        {
            return HitboxDebugType.ATTACK;
        }

        // Hurtbox配下のColliderは被攻撃判定として扱います。
        if (GetComponentInParent<Hurtbox>() != null)
        {
            return HitboxDebugType.HURT;
        }

        if (GetComponentInParent<AttackHitbox>() != null)
        {
            return HitboxDebugType.ATTACK;
        }

        return HitboxDebugType.OTHER;
    }

    /// <summary>
    /// Colliderを保持します。
    /// </summary>
    private void CacheCollider()
    {
        if (m_targetCollider != null)
        {
            return;
        }

        m_targetCollider = GetComponent<Collider>();
    }

    /// <summary>
    /// 軸に垂直な2つの方向を取得します。
    /// </summary>
    /// <param name="axis">基準となる軸。</param>
    /// <param name="tangent">1つ目の垂直方向。</param>
    /// <param name="bitangent">2つ目の垂直方向。</param>
    private void GetPerpendicularAxes(
        Vector3 axis,
        out Vector3 tangent,
        out Vector3 bitangent)
    {
        tangent = Vector3.Cross(axis, Vector3.up);

        // 軸が上方向と平行な場合は右方向を使用します。
        if (tangent.sqrMagnitude < 0.0001f)
        {
            tangent = Vector3.Cross(axis, Vector3.right);
        }

        tangent.Normalize();

        bitangent = Vector3.Cross(
            axis,
            tangent).normalized;
    }

    /// <summary>
    /// スケールを正の値へ変換します。
    /// </summary>
    /// <param name="scale">変換するスケール。</param>
    /// <returns>絶対値へ変換したスケール。</returns>
    private Vector3 GetAbsoluteScale(Vector3 scale)
    {
        return new Vector3(
            Mathf.Abs(scale.x),
            Mathf.Abs(scale.y),
            Mathf.Abs(scale.z));
    }

    /// <summary>
    /// Inspector設定時にColliderを自動取得します。
    /// </summary>
    private void Reset()
    {
        m_targetCollider = GetComponent<Collider>();
        m_debugType = HitboxDebugType.AUTO;
        m_displayMode = HitboxDebugDisplayMode.ALWAYS;
        m_showDisabledCollider = true;
    }

    /// <summary>
    /// Inspector設定を検証します。
    /// </summary>
    private void OnValidate()
    {
        CacheCollider();

        m_disabledAlphaMultiplier = Mathf.Clamp(
            m_disabledAlphaMultiplier,
            0.05f,
            1.0f);
    }
}