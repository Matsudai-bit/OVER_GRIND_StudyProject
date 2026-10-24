using UnityEngine;

/// <summary>
/// S1P3ボスのスタン状態を実行します。
/// </summary>
public sealed class S1P3BossStunnedState :
    StateBase<BossController>
{
    // Animatorパラメータ名
    private const string STUN_PARAMETER_NAME = "Stun";

    // AnimatorパラメータID
    private static readonly int STUN_PARAMETER_ID =
        Animator.StringToHash(
            STUN_PARAMETER_NAME);

    // スタン状態パラメータ
    private S1P3BossStunnedStateParameters m_parameters;

    // 経過時間
    private float m_elapsedTime;

    private S1P3BossReferences m_references;

    /// <summary>
    /// スタンを開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_elapsedTime = 0.0f;

        if (!TryGetParameters())
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);

            return;
        }
        
        if (Owner.PhaseController.TryGetCurrentPhaseComponent(out S1P3BossStateTransitionRequest transitionRequest))
        {
            transitionRequest.ConsumeStunnedRequest();
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        Owner.AnimationController?.SetBool(
            STUN_PARAMETER_ID,
            true);
    }

    /// <summary>
    /// スタン時間を更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間。</param>
    protected override void OnUpdate(float deltaTime)
    {
        if (m_parameters == null)
        {
            return;
        }

        m_elapsedTime += deltaTime;

        if (m_elapsedTime <
            m_parameters.Duration)
        {
            return;
        }

        Owner.AnimationController?.SetBool(
            STUN_PARAMETER_ID,
            false);

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// スタンを終了します。
    /// </summary>
    protected override void OnExitState()
    {
        Owner.AnimationController?.SetBool(
            STUN_PARAMETER_ID,
            false);

        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_parameters = null;
    }

    /// <summary>
    /// 現在フェーズのスタン状態パラメータを取得します。
    /// </summary>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetParameters()
    {
        if (Owner.PhaseController == null)
        {
            return false;
        }

        if (!Owner.PhaseController.TryGetCurrentPhaseComponent(
                out m_references))
        {
            return false;
        }

        if (m_references.StateParameterAsset == null)
        {
            return false;
        }

        m_parameters =
            m_references.StateParameterAsset.Stunned;

        return m_parameters != null;
    }
}
