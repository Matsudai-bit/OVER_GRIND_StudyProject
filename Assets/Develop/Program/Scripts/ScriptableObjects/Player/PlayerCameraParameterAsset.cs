using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// PlayerCameraで使用する全カメラパラメータを保持します。
/// Inspectorのデフォルト値と、セーブデータから反映される
/// ランタイム設定を一元管理します。
/// </summary>
[CreateAssetMenu(
    fileName = "PlayerCameraParameter",
    menuName = "Game/Parameters/Player/Camera Parameter")]
public sealed class PlayerCameraParameterAsset
    : ScriptableObject
{
    // --------------------------------------------------------
    // X Axis Sensitivity
    // --------------------------------------------------------

    // X軸カメラ感度の最大値
    [SerializeField, Header("X軸カメラ感度")]
    [Range(0.001f, 3.0f)]
    [Tooltip("X軸カメラ感度の最大値。")]
    private float m_cameraMaxSensitivityX = 5.0f;

    // X軸カメラ感度の最小値
    [SerializeField, Range(0.001f, 3.0f)]
    [Tooltip("X軸カメラ感度の最小値。")]
    private float m_cameraMinSensitivityX = 1.0f;

    // X軸感度のデフォルト比率
    [SerializeField, Range(0.0f, 1.0f)]
    [Tooltip(
        "X軸カメラ感度のデフォルト比率。"
        + "0で最小、1で最大です。")]
    private float m_defaultCameraSensitivityRatioX = 1.0f;

    // --------------------------------------------------------
    // Y Axis Sensitivity
    // --------------------------------------------------------

    // Y軸カメラ感度の最大値
    [SerializeField, Header("Y軸カメラ感度")]
    [Range(0.001f, 3.0f)]
    [Tooltip("Y軸カメラ感度の最大値。")]
    private float m_cameraMaxSensitivityY = 5.0f;

    // Y軸カメラ感度の最小値
    [SerializeField, Range(0.001f, 3.0f)]
    [Tooltip("Y軸カメラ感度の最小値。")]
    private float m_cameraMinSensitivityY = 1.0f;

    // Y軸感度のデフォルト比率
    [SerializeField, Range(0.0f, 1.0f)]
    [Tooltip(
        "Y軸カメラ感度のデフォルト比率。"
        + "0で最小、1で最大です。")]
    private float m_defaultCameraSensitivityRatioY = 1.0f;

    // --------------------------------------------------------
    // Reverse
    // --------------------------------------------------------

    // Y軸操作をデフォルトで反転するか
    [SerializeField, Header("カメラ反転")]
    [FormerlySerializedAs("m_isCameraReverseY")]
    [Tooltip(
        "Y軸カメラ操作をデフォルトで反転させるか。")]
    private bool m_defaultCameraReverseY = false;

    // --------------------------------------------------------
    // Charge Camera
    // --------------------------------------------------------

    // チャージ中に進行方向へ向く割合
    [SerializeField, Header("チャージカメラ")]
    [Range(0.0f, 1.0f)]
    [Tooltip(
        "チャージ中に進行方向へ向く割合。"
        + "0でチャージ開始時の向き、"
        + "1で進行方向を向きます。")]
    private float m_chargeCameraDirectionInfluence = 1.0f;

    // チャージ中に維持する上下角度
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

    // --------------------------------------------------------
    // Runtime User Settings
    // --------------------------------------------------------

    // セーブ設定から反映されたX軸感度比率
    private float m_cameraSensitivityRatioX = 1.0f;

    // セーブ設定から反映されたY軸感度比率
    private float m_cameraSensitivityRatioY = 1.0f;

    // セーブ設定から反映されたY軸反転設定
    private bool m_isCameraReverseY;

    /// <summary>
    /// ランタイムパラメータが変更された時に通知します。
    /// PlayerCameraはこの通知を受けて実際のCinemachine設定を再適用します。
    /// </summary>
    public event Action ParameterChanged;

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
    /// 現在のX軸感度比率を取得します。
    /// </summary>
    public float CameraSensitivityRatioX =>
        m_cameraSensitivityRatioX;

    /// <summary>
    /// 現在のY軸感度比率を取得します。
    /// </summary>
    public float CameraSensitivityRatioY =>
        m_cameraSensitivityRatioY;

    /// <summary>
    /// 現在Y軸操作を反転しているか取得します。
    /// </summary>
    public bool IsCameraReverseY =>
        m_isCameraReverseY;

    /// <summary>
    /// チャージ中の進行方向への影響率を取得します。
    /// </summary>
    public float ChargeCameraDirectionInfluence =>
        Mathf.Clamp01(
            m_chargeCameraDirectionInfluence);

    /// <summary>
    /// チャージ中に維持する上下角度を取得します。
    /// </summary>
    public float ChargeCameraVerticalAngle =>
        Mathf.Clamp(
            m_chargeCameraVerticalAngle,
            -10.0f,
            45.0f);

    /// <summary>
    /// チャージ中の上下回転速度を取得します。
    /// </summary>
    public float ChargeCameraVerticalTurnSpeed =>
        Mathf.Max(
            0.0f,
            m_chargeCameraVerticalTurnSpeed);

    /// <summary>
    /// Asset有効化時にランタイム設定をデフォルト値へ戻します。
    /// </summary>
    private void OnEnable()
    {
        ResetRuntimeSettings(
            notifyChanged: false);
    }

    /// <summary>
    /// X・Y両方のカメラ感度比率を同時に設定します。
    /// ゲーム設定の感度スライダーからの反映に使用します。
    /// </summary>
    /// <param name="ratio">
    /// 最小感度から最大感度までの比率。0～1。
    /// </param>
    public void SetCameraSensitivityRatio(
        float ratio)
    {
        float clampedRatio =
            Mathf.Clamp01(
                ratio);

        bool hasChanged =
            !Mathf.Approximately(
                m_cameraSensitivityRatioX,
                clampedRatio) ||
            !Mathf.Approximately(
                m_cameraSensitivityRatioY,
                clampedRatio);

        if (!hasChanged)
        {
            return;
        }

        m_cameraSensitivityRatioX =
            clampedRatio;

        m_cameraSensitivityRatioY =
            clampedRatio;

        NotifyParameterChanged();
    }

    /// <summary>
    /// X軸カメラ感度の比率を設定します。
    /// 個別調整が必要な場合の互換APIです。
    /// </summary>
    public void SetCameraSensitivityRatioX(
        float ratio)
    {
        float clampedRatio =
            Mathf.Clamp01(
                ratio);

        if (Mathf.Approximately(
                m_cameraSensitivityRatioX,
                clampedRatio))
        {
            return;
        }

        m_cameraSensitivityRatioX =
            clampedRatio;

        NotifyParameterChanged();
    }

    /// <summary>
    /// Y軸カメラ感度の比率を設定します。
    /// 個別調整が必要な場合の互換APIです。
    /// </summary>
    public void SetCameraSensitivityRatioY(
        float ratio)
    {
        float clampedRatio =
            Mathf.Clamp01(
                ratio);

        if (Mathf.Approximately(
                m_cameraSensitivityRatioY,
                clampedRatio))
        {
            return;
        }

        m_cameraSensitivityRatioY =
            clampedRatio;

        NotifyParameterChanged();
    }

    /// <summary>
    /// Y軸カメラ反転設定を変更します。
    /// </summary>
    /// <param name="isReverse">反転させるか。</param>
    public void SetCameraReverseY(
        bool isReverse)
    {
        if (m_isCameraReverseY ==
            isReverse)
        {
            return;
        }

        m_isCameraReverseY =
            isReverse;

        NotifyParameterChanged();
    }

    /// <summary>
    /// セーブデータから感度と反転設定をまとめて反映します。
    /// ParameterChangedは最後に1回だけ通知します。
    /// </summary>
    /// <param name="sensitivityRatio">
    /// カメラ感度比率。0～1。
    /// </param>
    /// <param name="isReverse">
    /// Y軸操作を反転するか。
    /// </param>
    public void ApplyRuntimeSettings(
        float sensitivityRatio,
        bool isReverse)
    {
        float clampedRatio =
            Mathf.Clamp01(
                sensitivityRatio);

        bool hasChanged =
            !Mathf.Approximately(
                m_cameraSensitivityRatioX,
                clampedRatio) ||
            !Mathf.Approximately(
                m_cameraSensitivityRatioY,
                clampedRatio) ||
            m_isCameraReverseY !=
                isReverse;

        if (!hasChanged)
        {
            return;
        }

        m_cameraSensitivityRatioX =
            clampedRatio;

        m_cameraSensitivityRatioY =
            clampedRatio;

        m_isCameraReverseY =
            isReverse;

        NotifyParameterChanged();
    }

    /// <summary>
    /// ランタイム設定をAssetのデフォルト値へ戻します。
    /// </summary>
    public void ResetRuntimeSettings()
    {
        ResetRuntimeSettings(
            notifyChanged: true);
    }

    /// <summary>
    /// ランタイム設定をAssetのデフォルト値へ戻します。
    /// </summary>
    /// <param name="notifyChanged">
    /// 変更通知を発行するか。
    /// </param>
    private void ResetRuntimeSettings(
        bool notifyChanged)
    {
        bool hasChanged =
            !Mathf.Approximately(
                m_cameraSensitivityRatioX,
                m_defaultCameraSensitivityRatioX) ||
            !Mathf.Approximately(
                m_cameraSensitivityRatioY,
                m_defaultCameraSensitivityRatioY) ||
            m_isCameraReverseY !=
                m_defaultCameraReverseY;

        m_cameraSensitivityRatioX =
            m_defaultCameraSensitivityRatioX;

        m_cameraSensitivityRatioY =
            m_defaultCameraSensitivityRatioY;

        m_isCameraReverseY =
            m_defaultCameraReverseY;

        if (notifyChanged &&
            hasChanged)
        {
            NotifyParameterChanged();
        }
    }

    /// <summary>
    /// 現在の設定からPlayerCameraParameterを生成します。
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
            ChargeCameraDirectionInfluence,
            ChargeCameraVerticalAngle,
            ChargeCameraVerticalTurnSpeed);
    }

    /// <summary>
    /// Parameterの変更を購読者へ通知します。
    /// </summary>
    private void NotifyParameterChanged()
    {
        ParameterChanged?.Invoke();
    }

#if UNITY_EDITOR

    /// <summary>
    /// Inspector編集値を補正し、
    /// Play中であれば現在のPlayerCameraへ再反映します。
    /// </summary>
    private void OnValidate()
    {
        // 最大感度が最小感度を下回らないよう補正する
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

        if (Application.isPlaying)
        {
            NotifyParameterChanged();
        }
    }

#endif
}
