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
}