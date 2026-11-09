using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// プレイヤーカメラで使用するパラメータを保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "PlayerCameraParameter",
    menuName = "Game/Parameters/Player/Camera Parameter")]
public sealed class PlayerCameraParameterAsset : ScriptableObject
{
    // X軸カメラ最大感度
    [SerializeField, Header("X軸カメラ感度")]
    [Range(0.1f, 5.0f)]
    [Tooltip("X軸カメラ感度の最大値。")]
    private float m_cameraMaxSensitivityX = 5.0f;

    // X軸カメラ最小感度
    [SerializeField, Range(0.1f, 5.0f)]
    [Tooltip("X軸カメラ感度の最小値。")]
    private float m_cameraMinSensitivityX = 1.0f;

    // X軸カメラ感度のデフォルト比率
    [SerializeField, Range(0.0f, 1.0f)]
    [Tooltip("X軸カメラ感度のデフォルト比率。0で最小、1で最大です。")]
    private float m_defaultCameraSensitivityRatioX = 1.0f;

    // Y軸カメラ最大感度
    [SerializeField, Header("Y軸カメラ感度")]
    [Range(0.1f, 5.0f)]
    [Tooltip("Y軸カメラ感度の最大値。")]
    private float m_cameraMaxSensitivityY = 5.0f;

    // Y軸カメラ最小感度
    [SerializeField, Range(0.1f, 5.0f)]
    [Tooltip("Y軸カメラ感度の最小値。")]
    private float m_cameraMinSensitivityY = 1.0f;

    // Y軸カメラ感度のデフォルト比率
    [SerializeField, Range(0.0f, 1.0f)]
    [Tooltip("Y軸カメラ感度のデフォルト比率。0で最小、1で最大です。")]
    private float m_defaultCameraSensitivityRatioY = 1.0f;

    // Y軸カメラ反転のデフォルト設定
    [SerializeField, Header("カメラ反転")]
    [FormerlySerializedAs("m_isCameraReverseY")]
    [Tooltip("Y軸カメラ操作をデフォルトで反転させるか。")]
    private bool m_defaultCameraReverseY = false;

    // チャージ中に進行方向へ向く割合
    [SerializeField, Header("チャージカメラ")]
    [Range(0.0f, 1.0f)]
    [Tooltip(
        "チャージ中に進行方向へ向く割合。"
        + "0でチャージ開始時の向き、1で進行方向を向きます。")]
    private float m_chargeCameraDirectionInfluence = 1.0f;

    // チャージ中に維持するカメラ上下角度
    [SerializeField, Range(-10.0f, 45.0f)]
    [Tooltip(
        "チャージ中に維持するカメラの上下角度。"
        + "Playerを少し上から見る角度です。")]
    private float m_chargeCameraVerticalAngle = 8.0f;

    // チャージ中の上下回転速度
    [SerializeField, Min(0.0f)]
    [Tooltip(
        "チャージ中にカメラの高さを"
        + "目的角度へ戻す速度[度/秒]。")]
    private float m_chargeCameraVerticalTurnSpeed = 120.0f;

    // ランタイム中のX軸カメラ感度比率
    private float m_cameraSensitivityRatioX = 1.0f;

    // ランタイム中のY軸カメラ感度比率
    private float m_cameraSensitivityRatioY = 1.0f;

    // ランタイム中のY軸カメラ反転設定
    private bool m_isCameraReverseY = false;

    /// <summary>
    /// 現在のX軸カメラ感度を取得します。
    /// </summary>
    public float CameraSensitivityX =>
        Mathf.Lerp(
            m_cameraMinSensitivityX,
            m_cameraMaxSensitivityX,
            m_cameraSensitivityRatioX);

    /// <summary>
    /// 現在のY軸カメラ感度を取得します。
    /// </summary>
    public float CameraSensitivityY =>
        Mathf.Lerp(
            m_cameraMinSensitivityY,
            m_cameraMaxSensitivityY,
            m_cameraSensitivityRatioY);

    /// <summary>
    /// 現在のX軸カメラ感度比率を取得します。
    /// </summary>
    public float CameraSensitivityRatioX =>
        m_cameraSensitivityRatioX;

    /// <summary>
    /// 現在のY軸カメラ感度比率を取得します。
    /// </summary>
    public float CameraSensitivityRatioY =>
        m_cameraSensitivityRatioY;

    /// <summary>
    /// 現在Y軸カメラ操作を反転しているか取得します。
    /// </summary>
    public bool IsCameraReverseY =>
        m_isCameraReverseY;

    private void OnEnable()
    {
        ResetRuntimeSettings();
    }

    /// <summary>
    /// X軸カメラ感度の比率を設定します。
    /// </summary>
    /// <param name="ratio">
    /// 最小感度から最大感度までの比率。0～1。
    /// </param>
    public void SetCameraSensitivityRatioX(float ratio)
    {
        m_cameraSensitivityRatioX =
            Mathf.Clamp01(ratio);
    }

    /// <summary>
    /// Y軸カメラ感度の比率を設定します。
    /// </summary>
    /// <param name="ratio">
    /// 最小感度から最大感度までの比率。0～1。
    /// </param>
    public void SetCameraSensitivityRatioY(float ratio)
    {
        m_cameraSensitivityRatioY =
            Mathf.Clamp01(ratio);
    }

    /// <summary>
    /// Y軸カメラ反転設定を変更します。
    /// </summary>
    /// <param name="isReverse">反転させるか。</param>
    public void SetCameraReverseY(bool isReverse)
    {
        m_isCameraReverseY = isReverse;
    }

    /// <summary>
    /// ランタイム設定をAssetのデフォルト値へ戻します。
    /// </summary>
    public void ResetRuntimeSettings()
    {
        m_cameraSensitivityRatioX =
            m_defaultCameraSensitivityRatioX;

        m_cameraSensitivityRatioY =
            m_defaultCameraSensitivityRatioY;

        m_isCameraReverseY =
            m_defaultCameraReverseY;
    }

    /// <summary>
    /// 現在の設定からカメラパラメータを生成します。
    /// </summary>
    /// <returns>
    /// 現在のユーザー設定を反映したカメラパラメータ。
    /// </returns>
    public PlayerCameraParameter CreateCameraParameter()
    {
        return new PlayerCameraParameter(
            CameraSensitivityX,
            CameraSensitivityY,
            m_isCameraReverseY,
            m_chargeCameraDirectionInfluence,
            m_chargeCameraVerticalAngle,
            m_chargeCameraVerticalTurnSpeed);
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        // 最大値が最小値を下回らないよう補正
        if (m_cameraMaxSensitivityX <
            m_cameraMinSensitivityX)
        {
            m_cameraMaxSensitivityX =
                m_cameraMinSensitivityX;
        }

        if (m_cameraMaxSensitivityY <
            m_cameraMinSensitivityY)
        {
            m_cameraMaxSensitivityY =
                m_cameraMinSensitivityY;
        }
    }

#endif
}