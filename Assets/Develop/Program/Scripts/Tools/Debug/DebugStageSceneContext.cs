using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Text;
using UnityEngine.InputSystem;
#endif

/// <summary>
/// StageSceneContext のランタイム情報を表示します。
/// </summary>
[DisallowMultipleComponent]
public sealed class DebugStageSceneContext : MonoBehaviour
{
    [SerializeField, Header("参照")]
    private StageSceneContext m_stageSceneContext;

#if UNITY_EDITOR || DEVELOPMENT_BUILD

    private const float UPDATE_INTERVAL = 0.1f;

    [SerializeField, Header("初期表示状態")]
    private bool m_isVisible = true;

    [SerializeField, Header("表示位置・サイズ")]
    private Rect m_displayRect =
        new Rect(340.0f, 10.0f, 380.0f, 300.0f);

    // 表示文字列生成用
    private readonly StringBuilder m_builder =
        new StringBuilder(512);

    // 現在表示する文字列
    private string m_displayText =
        string.Empty;

    // 表示更新間隔計測
    private float m_elapsedTime;

    private void OnEnable()
    {
        UpdateDisplayText();
    }

    private void Update()
    {
        // F4で表示状態を切り替える
        if (Keyboard.current != null
            && Keyboard.current.f4Key.wasPressedThisFrame)
        {
            m_isVisible =
                !m_isVisible;
        }

        if (!m_isVisible)
        {
            return;
        }

        m_elapsedTime +=
            Time.unscaledDeltaTime;

        if (m_elapsedTime < UPDATE_INTERVAL)
        {
            return;
        }

        UpdateDisplayText();

        m_elapsedTime =
            0.0f;
    }

    private void OnGUI()
    {
        if (!m_isVisible)
        {
            return;
        }

        GUI.Box(
            m_displayRect,
            "Stage Scene Context");

        GUILayout.BeginArea(
            m_displayRect);

        GUILayout.Space(25.0f);

        GUILayout.Label(
            m_displayText);

        GUILayout.EndArea();
    }

    /// <summary>
    /// StageSceneContext の表示内容を更新します。
    /// </summary>
    private void UpdateDisplayText()
    {
        m_builder.Clear();

        if (m_stageSceneContext == null)
        {
            m_builder.AppendLine(
                "StageSceneContext : Not Assigned");

            m_displayText =
                m_builder.ToString();

            return;
        }

        AppendStageData();
        AppendEntryData();
        AppendCheckpointData();

        m_displayText =
            m_builder.ToString();
    }

    /// <summary>
    /// ステージ情報を追加します。
    /// </summary>
    private void AppendStageData()
    {
        m_builder.AppendLine(
            "[ Stage ]");

        m_builder.AppendLine(
            $"Stage Number : {m_stageSceneContext.StageNumber}");

        m_builder.AppendLine(
            $"Stage Name : {m_stageSceneContext.StageName}");

        m_builder.AppendLine(
            $"Retry Count : {m_stageSceneContext.RetryCount}");

        m_builder.AppendLine();
    }

    /// <summary>
    /// Scene開始要求情報を追加します。
    /// </summary>
    private void AppendEntryData()
    {
        m_builder.AppendLine(
            "[ Entry ]");

        m_builder.AppendLine(
            $"Entry Type : {m_stageSceneContext.EntryType}");

        m_builder.AppendLine(
            $"Carried Time : "
            + $"{m_stageSceneContext.CarriedElapsedSeconds:F2} sec");

        m_builder.AppendLine();
    }

    /// <summary>
    /// チェックポイント情報を追加します。
    /// </summary>
    private void AppendCheckpointData()
    {
        m_builder.AppendLine(
            "[ Checkpoint ]");

        StageCheckpointSnapshot snapshot =
            m_stageSceneContext.CheckpointSnapshot;

        if (snapshot == null)
        {
            m_builder.AppendLine(
                "Has Checkpoint : False");

            return;
        }

        m_builder.AppendLine(
            "Has Checkpoint : True");

        m_builder.AppendLine(
            $"Checkpoint ID : {snapshot.CheckpointId}");

        m_builder.AppendLine(
            $"Boss Phase : {snapshot.BossPhase}");

        int participantCount =
            snapshot.ParticipantSnapshots != null
                ? snapshot.ParticipantSnapshots.Count
                : 0;

        m_builder.AppendLine(
            $"Participant Count : {participantCount}");
    }

#endif
}