using Unity.Behavior;
using UnityEngine;

/// <summary>
/// ボスの行動選択Conditionの基底クラスです。
/// </summary>
public abstract class BossActionDecisionCondition : Condition
{
    /// <summary>
    /// 指定した確率で行動を選択するか判定します。
    /// </summary>
    /// <param name="probability">選択確率。0.0～1.0。</param>
    /// <returns>
    /// true：抽選に成功しました。
    /// false：抽選に失敗しました。
    /// </returns>
    protected bool CheckProbability(float probability)
    {
        float clampedProbability =
            Mathf.Clamp01(probability);

        return Random.value <= clampedProbability;
    }

    /// <summary>
    /// 指定したStateがクールタイム終了済みか確認します。
    /// </summary>
    /// <typeparam name="TState">確認するState。</typeparam>
    /// <param name="bossController">対象ボス。</param>
    /// <returns>
    /// true：Stateを使用可能です。
    /// false：使用できません。
    /// </returns>
    protected bool IsStateReady<TState>(
        BossController bossController)
    {
        if (bossController == null)
        {
            return false;
        }

        BossStateCoolTimeManager coolTimeManager =
            bossController.GetComponent<BossStateCoolTimeManager>();

        if (coolTimeManager == null)
        {
            return false;
        }

        return coolTimeManager.IsReady<TState>();
    }

    /// <summary>
    /// XZ平面上の距離を取得します。
    /// </summary>
    /// <param name="from">開始位置。</param>
    /// <param name="to">終了位置。</param>
    /// <returns>水平距離。</returns>
    protected float GetHorizontalDistance(
        Vector3 from,
        Vector3 to)
    {
        Vector3 difference = to - from;
        difference.y = 0.0f;

        return difference.magnitude;
    }
}
