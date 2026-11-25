using TMPro;
using UnityEngine;

/// <summary>
/// ポーズ画面のステータス表示を担当します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PauseStatusView : MonoBehaviour
{
    [SerializeField, Header("参照")]
    private PauseStatusModel m_model;

    [SerializeField, Header("表示")]
    private TMP_Text m_remainingTimeText;

    [SerializeField]
    private TMP_Text m_retryCountText;

    private void OnEnable()
    {
        RegisterEvents();
        Refresh();
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    /// <summary>
    /// Modelのイベントを登録します。
    /// </summary>
    private void RegisterEvents()
    {
        if (m_model == null)
        {
            Debug.LogWarning(
                $"{nameof(PauseStatusModel)} が設定されていません。",
                this);

            return;
        }

        m_model.RemainingTimeChanged +=
            OnRemainingTimeChanged;

        m_model.RetryCountChanged +=
            OnRetryCountChanged;
    }

    /// <summary>
    /// Modelのイベントを解除します。
    /// </summary>
    private void UnregisterEvents()
    {
        if (m_model == null)
        {
            return;
        }

        m_model.RemainingTimeChanged -=
            OnRemainingTimeChanged;

        m_model.RetryCountChanged -=
            OnRetryCountChanged;
    }

    /// <summary>
    /// 現在のModel値を表示へ反映します。
    /// </summary>
    public void Refresh()
    {
        if (m_model == null)
        {
            return;
        }

        UpdateRemainingTimeText(
            m_model.RemainingSeconds);

        UpdateRetryCountText(
            m_model.RetryCount);
    }

    /// <summary>
    /// 残り時間変更を受け取ります。
    /// </summary>
    /// <param name="remainingSeconds">
    /// 現在の残り時間。
    /// </param>
    private void OnRemainingTimeChanged(
        double remainingSeconds)
    {
        UpdateRemainingTimeText(
            remainingSeconds);
    }

    /// <summary>
    /// リトライ回数変更を受け取ります。
    /// </summary>
    /// <param name="retryCount">
    /// 現在のリトライ回数。
    /// </param>
    private void OnRetryCountChanged(
        int retryCount)
    {
        UpdateRetryCountText(
            retryCount);
    }

    /// <summary>
    /// 残り時間表示を更新します。
    /// </summary>
    /// <param name="remainingSeconds">
    /// 表示する残り時間。
    /// </param>
    private void UpdateRemainingTimeText(
        double remainingSeconds)
    {
        if (m_remainingTimeText == null)
        {
            return;
        }

        int totalSeconds =
            Mathf.Max(
                0,
                Mathf.CeilToInt(
                    (float)remainingSeconds));

        int minutes =
            totalSeconds / 60;

        int seconds =
            totalSeconds % 60;

        m_remainingTimeText.text =
            $"{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// リトライ回数表示を更新します。
    /// </summary>
    /// <param name="retryCount">
    /// 表示するリトライ回数。
    /// </param>
    private void UpdateRetryCountText(
        int retryCount)
    {
        if (m_retryCountText == null)
        {
            return;
        }

        m_retryCountText.text =
            retryCount.ToString();
    }
}