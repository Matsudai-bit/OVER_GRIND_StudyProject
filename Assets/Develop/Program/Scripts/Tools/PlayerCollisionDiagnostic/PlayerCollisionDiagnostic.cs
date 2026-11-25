using UnityEngine;

/// <summary>
/// Playerの物理衝突ペアを診断ログへ出力します。
/// レール離脱時に何のColliderへ接触しているかを特定するための一時デバッグ用です。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerCollisionDiagnostic : MonoBehaviour
{
    // CollisionStayのログ出力間隔
    private const float STAY_LOG_INTERVAL = 0.1f;

    [SerializeField, Header("診断設定")]
    private bool m_enableDiagnostic = true;

    [SerializeField]
    private bool m_logCollisionStay = true;

    [SerializeField]
    private bool m_logCollisionExit = false;

    // 次にCollisionStayを出力できる時刻
    private float m_nextStayLogTime;

    /// <summary>
    /// 衝突開始時のColliderペアを出力します。
    /// </summary>
    /// <param name="collision">衝突情報。</param>
    private void OnCollisionEnter(
        Collision collision)
    {
        if (!m_enableDiagnostic)
        {
            return;
        }

        LogCollision(
            "ENTER",
            collision);
    }

    /// <summary>
    /// 衝突継続中のColliderペアを一定間隔で出力します。
    /// </summary>
    /// <param name="collision">衝突情報。</param>
    private void OnCollisionStay(
        Collision collision)
    {
        if (!m_enableDiagnostic ||
            !m_logCollisionStay ||
            Time.time < m_nextStayLogTime)
        {
            return;
        }

        m_nextStayLogTime =
            Time.time +
            STAY_LOG_INTERVAL;

        LogCollision(
            "STAY",
            collision);
    }

    /// <summary>
    /// 衝突終了時のColliderペアを出力します。
    /// </summary>
    /// <param name="collision">衝突情報。</param>
    private void OnCollisionExit(
        Collision collision)
    {
        if (!m_enableDiagnostic ||
            !m_logCollisionExit)
        {
            return;
        }

        LogCollision(
            "EXIT",
            collision);
    }

    /// <summary>
    /// Collisionに含まれる全ContactPointを診断ログへ出力します。
    /// </summary>
    /// <param name="eventName">Collisionイベント名。</param>
    /// <param name="collision">衝突情報。</param>
    private static void LogCollision(
        string eventName,
        Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        if (collision.contactCount == 0)
        {
            Debug.LogWarning(
                $"[CollisionDiagnostic][{eventName}] " +
                $"Contactなし / " +
                $"CollisionCollider={GetColliderDescription(collision.collider)}");

            return;
        }

        for (int contactIndex = 0;
             contactIndex < collision.contactCount;
             contactIndex++)
        {
            ContactPoint contact =
                collision.GetContact(
                    contactIndex);

            Collider playerCollider =
                contact.thisCollider;

            Collider otherCollider =
                contact.otherCollider;

            bool isIgnored =
                playerCollider != null &&
                otherCollider != null &&
                Physics.GetIgnoreCollision(
                    playerCollider,
                    otherCollider);

            SplineRailInfo rail =
                otherCollider != null
                    ? otherCollider.GetComponentInParent<SplineRailInfo>()
                    : null;

            bool isConfiguredRailCollider =
                IsConfiguredRailCollider(
                    rail,
                    otherCollider);

            string railName =
                rail != null
                    ? rail.name
                    : "None";

            Debug.LogWarning(
                $"[CollisionDiagnostic][{eventName}]\n" +
                $"Frame={Time.frameCount}, FixedTime={Time.fixedTime:F3}\n" +
                $"THIS  = {GetColliderDescription(playerCollider)}\n" +
                $"OTHER = {GetColliderDescription(otherCollider)}\n" +
                $"IgnoreCollision = {isIgnored}\n" +
                $"RailInfo = {railName}\n" +
                $"ConfiguredRailCollider = {isConfiguredRailCollider}\n" +
                $"Point = {contact.point:F3}\n" +
                $"Normal = {contact.normal:F3}\n" +
                $"Separation = {contact.separation:F4}\n" +
                $"RelativeVelocity = {collision.relativeVelocity:F3}\n" +
                $"Impulse = {collision.impulse:F3}",
                otherCollider);
        }
    }

    /// <summary>
    /// Colliderがレールの明示指定Colliderに含まれているか確認します。
    /// </summary>
    /// <param name="rail">対象レール。</param>
    /// <param name="collider">確認するCollider。</param>
    /// <returns>
    /// true：明示指定Colliderです。
    /// false：指定されていません。
    /// </returns>
    private static bool IsConfiguredRailCollider(
        SplineRailInfo rail,
        Collider collider)
    {
        if (rail == null ||
            collider == null)
        {
            return false;
        }

        SplineRailCollisionSettings settings =
            rail.GetComponent<SplineRailCollisionSettings>();

        if (settings == null ||
            settings.CollisionColliders == null)
        {
            return false;
        }

        foreach (Collider configuredCollider in
                 settings.CollisionColliders)
        {
            if (configuredCollider ==
                collider)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// ColliderのHierarchy、型、Layerなどを文字列化します。
    /// </summary>
    /// <param name="collider">対象Collider。</param>
    /// <returns>診断用文字列。</returns>
    private static string GetColliderDescription(
        Collider collider)
    {
        if (collider == null)
        {
            return "NULL";
        }

        string layerName =
            LayerMask.LayerToName(
                collider.gameObject.layer);

        string rigidbodyName =
            collider.attachedRigidbody != null
                ? collider.attachedRigidbody.name
                : "None";

        return
            $"{GetHierarchyPath(collider.transform)} " +
            $"[{collider.GetType().Name}] " +
            $"Layer={layerName}({collider.gameObject.layer}), " +
            $"Tag={collider.tag}, " +
            $"Trigger={collider.isTrigger}, " +
            $"Enabled={collider.enabled}, " +
            $"AttachedRigidbody={rigidbodyName}";
    }

    /// <summary>
    /// TransformのHierarchyパスを取得します。
    /// </summary>
    /// <param name="target">対象Transform。</param>
    /// <returns>Hierarchyパス。</returns>
    private static string GetHierarchyPath(
        Transform target)
    {
        if (target == null)
        {
            return "NULL";
        }

        string path =
            target.name;

        Transform parent =
            target.parent;

        while (parent != null)
        {
            path =
                parent.name +
                "/" +
                path;

            parent =
                parent.parent;
        }

        return path;
    }
}
