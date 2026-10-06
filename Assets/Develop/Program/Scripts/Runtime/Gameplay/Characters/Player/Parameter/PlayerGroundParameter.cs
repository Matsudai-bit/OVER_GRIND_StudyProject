using UnityEngine;

/// <summary>
/// プレイヤーの接地に関するパラメータを保持します。
/// </summary>
public readonly struct PlayerGroundParameter
{
    /// <summary>
    /// 接地時パラメータを生成します。
    /// </summary>
    /// <param name="groundCheckOrigin">      接地判定を行う位置                        </param>
    /// <param name="groundChackRadius">      接地判定の半径                            </param>
    /// <param name="groundLayerMask">        接地対象のレイヤー                        </param>
    /// <param name="railLayerMask">          接地対象のレイヤー（レール）              </param>
    /// <param name="railDetectionParameter"> 地上・空中の判定半径を設定するアセット    </param>
    public PlayerGroundParameter(
        Transform groundCheckOrigin,
        float groundChackRadius,
        LayerMask groundLayerMask,
        LayerMask railLayerMask,
        PlayerRailDetectionParameterAsset railDetectionParameter)
    {
        m_groundCheckOrigin = groundCheckOrigin;
        m_groundCheckRadius = groundChackRadius;
        m_groundLayerMask = groundLayerMask;
        m_railLayerMask = railLayerMask;
        m_railDetectionParameter = railDetectionParameter;
    }

    /// <summary>
    /// 接地判定を行う位置を取得します。
    /// </summary>
    public Transform m_groundCheckOrigin { get; }

    /// <summary>
    /// 接地判定の半径を取得します。
    /// </summary>
    public float m_groundCheckRadius { get; }

    /// <summary>
    /// 接地対象のレイヤーを取得します。
    /// </summary>
    public LayerMask m_groundLayerMask { get; }

    /// <summary>
    /// 接地対象のレイヤー（レール）を取得します。
    /// </summary>
    public LayerMask m_railLayerMask { get; }

    /// <summary>
    /// 地上・空中の判定半径を設定するアセットを取得します。
    /// </summary>
    public PlayerRailDetectionParameterAsset m_railDetectionParameter { get; }
}
