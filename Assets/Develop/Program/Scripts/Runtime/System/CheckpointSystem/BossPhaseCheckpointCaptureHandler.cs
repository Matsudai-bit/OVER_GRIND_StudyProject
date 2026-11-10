using UnityEngine;

/// <summary>
/// ボスフェーズ変更時に新しいチェックポイントを記録します。
/// </summary>
[DisallowMultipleComponent]
public sealed class BossPhaseCheckpointCaptureHandler :
    MonoBehaviour
{
    // ボスフェーズ管理
    [SerializeField, Header("参照")]
    private BossPhaseController m_bossPhaseController;

    // チェックポイント管理
    [SerializeField]
    private StageCheckpointController m_checkpointController;

    private void OnEnable()
    {
        if (m_bossPhaseController != null)
        {
            m_bossPhaseController.PhaseChanged +=
                OnBossPhaseChanged;
        }
    }

    private void OnDisable()
    {
        if (m_bossPhaseController != null)
        {
            m_bossPhaseController.PhaseChanged -=
                OnBossPhaseChanged;
        }
    }

    /// <summary>
    /// ボスフェーズ変更時にチェックポイントを記録します。
    /// </summary>
    /// <param name="phaseID">変更後のフェーズ。</param>
    private void OnBossPhaseChanged(
        BossPhaseID phaseID)
    {
        if (m_checkpointController == null
            || m_checkpointController.IsRestoring)
        {
            return;
        }

        m_checkpointController
            .CaptureCurrentPhaseCheckpoint();
    }
}
