using System;
using Unity.Behavior;
using UnityEngine;

[Serializable]
[Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "IsBreakingLeg",
    story: "[TargetObject]の両足が破壊されたかどうかを確認する",
    category: "Conditions",
    id: "c75a0644179e64a14abedeef788e8737")]
public partial class IsBreakingLegCondition : Condition
{
    // 判定対象のゲームオブジェクト
    [SerializeReference]
    public BlackboardVariable<GameObject> TargetObject;

    // 対象のボス制御コンポーネント
    private P1BossController m_p1BossController;

    /// <summary>
    /// 両足が破壊されているかを判定します。
    /// </summary>
    /// <returns>
    /// true：両足が破壊されています。
    /// false：両足が破壊されていない、または判定できません。
    /// </returns>
    public override bool IsTrue()
    {
        if (m_p1BossController == null)
        {
            return false;
        }

        return m_p1BossController.AreBothLegsBroken;
    }

    /// <summary>
    /// 判定対象のコンポーネントを取得します。
    /// </summary>
    public override void OnStart()
    {
        m_p1BossController = null;

        if (TargetObject == null)
        {
            Debug.LogError(
                $"{nameof(IsBreakingLegCondition)}: " +
                $"{nameof(TargetObject)}が設定されていません。");

            return;
        }

        GameObject targetObject = TargetObject.Value;

        if (targetObject == null)
        {
            Debug.LogError(
                $"{nameof(IsBreakingLegCondition)}: " +
                $"{nameof(TargetObject)}の値が設定されていません。");

            return;
        }

        if (!targetObject.TryGetComponent(out m_p1BossController))
        {
            Debug.LogError(
                $"{nameof(IsBreakingLegCondition)}: " +
                $"{targetObject.name}に{nameof(P1BossController)}が設定されていません。");
        }
    }

    /// <summary>
    /// 保持している参照を解除します。
    /// </summary>
    public override void OnEnd()
    {
        m_p1BossController = null;
    }
}