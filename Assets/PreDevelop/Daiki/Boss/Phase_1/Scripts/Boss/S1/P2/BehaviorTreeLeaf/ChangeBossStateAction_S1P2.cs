using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>
/// ステージ1ボスのステート変更を要求します。
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "ChangeBossStateAction_S1P2",
    story: "Change Stage1Phase2 [ControllerObject] state to [NextStateID]",
    category: "Action",
    id: "f8f0be769590a5c1ead3ce45dfd3bbea")]
public partial class ChangeBossStateAction_S1P2 : Action
{
    // 待機時間
    private const float IDLE_DURATION = 10.0f;

    // 操作対象のボス
    [SerializeReference]
    public BlackboardVariable<BossController> ControllerObject;

    // 次に実行する状態
    [SerializeReference]
    public BlackboardVariable<S1BossStateID> NextStateID;

    /// <summary>
    /// 状態変更を実行します。
    /// </summary>
    /// <returns>Actionノードの実行結果。</returns>
    protected override Status OnStart()
    {
        if (ControllerObject == null ||
            ControllerObject.Value == null)
        {
            LogFailure("Controller Objectが設定されていません。");
            return Status.Failure;
        }

        if (NextStateID == null)
        {
            LogFailure("Next State IDが設定されていません。");
            return Status.Failure;
        }

        BossController bossController = ControllerObject.Value;

        switch (NextStateID.Value)
        {
            case S1BossStateID.IDLE:
                bossController.StateMachine.ChangeState<BossIdleState>(
                    IDLE_DURATION);
                break;

            case S1BossStateID.P2_ATTACK_CHARGING:
                ChangeS1P2AttackState(bossController, S1P2BossAttackType.CHARGING);
                break;

            default:
                LogFailure("未対応のState IDです。");
                return Status.Failure;
        }

        return Status.Success;
    }

    /// <summary>
    /// ステージ1フェーズ1の攻撃ステートへ変更します。
    /// </summary>
    /// <param name="bossController">対象ボス。</param>
    /// <param name="attackType">攻撃種類。</param>
    /// <returns>Actionノードの実行結果。</returns>
    private Status ChangeS1P2AttackState(
        BossController bossController,
        S1P2BossAttackType attackType)
    {
        S1P2BossAttackSettings attackSettings =
            bossController.GetComponentInChildren<
                S1P2BossAttackSettings>(true);

        if (attackSettings == null)
        {
            LogFailure(
                $"{nameof(S1P2BossAttackSettings)}が見つかりません。");

            return Status.Failure;
        }

        if (!attackSettings.TryGetAttackSetting(
                attackType,
                out AttackIdentifier attackIdentifier,
                out string animationTriggerName))
        {
            LogFailure(
                $"{attackType}の攻撃設定がありません。");

            return Status.Failure;
        }

        bossController.StateMachine.ChangeState<S1P2BossChargingAttackState>(
            animationTriggerName,
            attackIdentifier,null);

        return Status.Success;
    }
}
