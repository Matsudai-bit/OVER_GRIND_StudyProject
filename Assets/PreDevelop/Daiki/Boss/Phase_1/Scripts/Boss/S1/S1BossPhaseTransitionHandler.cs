using UnityEngine;

/// <summary>
/// ステージ1ボス固有のフェーズ移行ステートを開始します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1BossPhaseTransitionHandler : MonoBehaviour
{
    // フェーズ1の脚管理
    [SerializeField, Header("ステージ1フェーズ移行")]
    private S1P1BossLegsController m_s1P1BossLegsController;

    // フェーズ1から2へ移行するまでの時間
    [SerializeField, Min(0.0f)]
    private float m_s1P1TransitionDuration = 5.0f;

    // ボス制御
    private BossController m_bossController;

    // フェーズ管理
    private BossPhaseController m_phaseController;

    /// <summary>
    /// 必要な参照を取得します。
    /// </summary>
    private void Awake()
    {
        m_bossController = GetComponent<BossController>();
        m_phaseController = GetComponent<BossPhaseController>();
    }

    /// <summary>
    /// フェーズ終了通知を購読します。
    /// </summary>
    private void OnEnable()
    {
        if (m_phaseController != null)
        {
            m_phaseController.PhaseCompletionRequested +=
                HandlePhaseCompletionRequested;
        }
    }

    /// <summary>
    /// フェーズ終了通知の購読を解除します。
    /// </summary>
    private void OnDisable()
    {
        if (m_phaseController != null)
        {
            m_phaseController.PhaseCompletionRequested -=
                HandlePhaseCompletionRequested;
        }
    }

    /// <summary>
    /// フェーズ終了要求に対応する遷移ステートを開始します。
    /// </summary>
    /// <param name="phaseID">終了したフェーズ。</param>
    private void HandlePhaseCompletionRequested(BossPhaseID phaseID)
    {
        if (m_bossController == null)
        {
            return;
        }

        switch (phaseID)
        {
            case BossPhaseID.PHASE_1:
                StartS1P1Transition();
                break;

            case BossPhaseID.PHASE_2:
                StartS1P2Transition();
                break;

            case BossPhaseID.PHASE_3:
                Debug.LogWarning(
                    "S1P3の撃破処理は未実装です。",
                    this);
                break;
        }
    }

    /// <summary>
    /// ステージ1フェーズ1の遷移を開始します。
    /// </summary>
    private void StartS1P1Transition()
    {
        if (m_s1P1BossLegsController == null)
        {
            Debug.LogError(
                $"{nameof(S1P1BossLegsController)}が設定されていません。",
                this);
            return;
        }

        m_bossController.StateMachine
            .ChangeState<S1P1BossLegsCollapsingState>(
                m_s1P1BossLegsController,
                m_s1P1TransitionDuration);
    }

    /// <summary>
    /// ステージ1フェーズ2の遷移を開始します。
    /// </summary>
    private void StartS1P2Transition()
    {
        //if (m_s1P1BossLegsController == null)
        //{
        //    Debug.LogError(
        //        $"{nameof(S1P1BossLegsController)}が設定されていません。",
        //        this);
        //    return;
        //}

        //m_bossController.StateMachine
        //    .ChangeState<S1P1BossLegsCollapsingState>(
        //        m_s1P1BossLegsController,
        //        m_s1P1TransitionDuration);

        m_phaseController.AdvancePhase();
    }
}
