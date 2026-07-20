using Unity.Behavior;
using UnityEngine;

public class P1BossTurnState : StateBase<P1BossController>
{
    /// <summary>
    /// 回転角度。
    /// </summary>
    private const float ROTATE_ANGLE = 90.0f;

    /// <summary>
    /// 回転時間。
    /// </summary>
    private const float ROTATE_DURATION = 2.0f;

    /// <summary>
    /// 開始時の回転。
    /// </summary>
    private Quaternion m_startRotation;

    /// <summary>
    /// 目標の回転。
    /// </summary>
    private Quaternion m_targetRotation;

    /// <summary>
    /// 経過時間。
    /// </summary>
    private float m_elapsedTime;

    /// <summary>
    /// 回転状態を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        if (Owner == null)
        {
            return;
        }

        Owner.Animator.SetBool("Turn", true);

        // 実行状態を開始状態へ設定します。
        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        // 回転に使用する値を初期化します。
        m_elapsedTime = 0.0f;
        m_startRotation = Owner.transform.rotation;

        m_targetRotation =
            m_startRotation *
            Quaternion.Euler(0.0f, ROTATE_ANGLE, 0.0f);
    }

    /// <summary>
    /// 回転状態を更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間。</param>
    protected override void OnUpdate(float deltaTime)
    {
        if (Owner == null)
        {
            return;
        }

        // 経過時間から回転割合を算出します。
        m_elapsedTime += deltaTime;

        float progress = Mathf.Clamp01(
            m_elapsedTime / ROTATE_DURATION);

        // 開始角度から目標角度まで補間します。
        Owner.transform.rotation = Quaternion.Slerp(
            m_startRotation,
            m_targetRotation,
            progress);

        if (progress < 1.0f)
        {
            return;
        }

        // 誤差を残さないよう目標角度へ固定します。
        Owner.transform.rotation = m_targetRotation;

        // 回転完了を通知します。
        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// 回転状態を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        if (Owner == null)
        {
            return;
        }

        Owner.Animator.SetBool("Turn", false);

        // 実行中に終了した場合はキャンセルとして扱います。
        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }
    }

}
