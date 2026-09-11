using UnityEngine;

/// <summary>
/// ロックオン可能な敵の部位を表します。
/// </summary>
[DisallowMultipleComponent]
public sealed class TargetPoint : MonoBehaviour
{
    [Tooltip("ターゲット位置として使用するTransformです。未設定の場合は自身のTransformを使用します。")]
    [SerializeField]
    private Transform m_targetTransform;

    /// <summary>
    /// ターゲット位置として使用するTransformを取得します。
    /// </summary>
    public Transform TargetTransform
    {
        get
        {
            return m_targetTransform != null
                ? m_targetTransform
                : transform;
        }
    }

    /// <summary>
    /// ターゲットのワールド座標を取得します。
    /// </summary>
    public Vector3 Position
    {
        get
        {
            return TargetTransform.position;
        }
    }

    /// <summary>
    /// コンポーネント追加時に自身のTransformを初期値として設定します。
    /// </summary>
    private void Reset()
    {
        m_targetTransform = transform;
    }
}