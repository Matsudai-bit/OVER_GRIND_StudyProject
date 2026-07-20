using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ChangeP1BossStateAction", story: "Change [ControllerObject] state to [NextStateID]", category: "Action", id: "566694c6b6e9c52b25098cd21eb23379")]
public partial class ChangeP1BossStateAction : Action
{
    [SerializeReference] public BlackboardVariable<P1BossController> ControllerObject;
    [SerializeReference] public BlackboardVariable<P1BossStateID> NextStateID;
 
    /// <summary>
    /// 状態変更を実行します。
    /// </summary>
    /// <returns>Actionノードの実行結果。</returns>
    protected override Status OnStart()
    {
        // Blackboard変数を確認します。
        if (ControllerObject == null || ControllerObject.Value == null)
        {
            LogFailure("Controller Objectが設定されていません。");
            return Status.Failure;
        }

        if (NextStateID == null)
        {
            LogFailure("Next Stateが設定されていません。");
            return Status.Failure;
        }

        // EnemyControllerを取得します。
        if (!ControllerObject.Value.TryGetComponent(out P1BossController enemyController))
        {
            LogFailure(
                $"{ControllerObject.Value.name}にEnemyControllerがありません。");

            return Status.Failure;
        }

        // コントローラーへ状態変更を依頼します。
        switch(NextStateID.Value)
        {
            case P1BossStateID.IDLE:
                enemyController.StateMachine.ChangeState<P1BossIdleState>();
                break;
            case P1BossStateID.WALK:
                enemyController.StateMachine.ChangeState<P1BossWalkState>();
                break;
            case P1BossStateID.ATTACK_LEFT:
                enemyController.StateMachine.ChangeState<P1BossAttackState>("Attack_Left", P1AttackType.LEFT_LEG);
                break;
            case P1BossStateID.ATTACK_RIGHT:
                enemyController.StateMachine.ChangeState<P1BossAttackState>("Attack_Right", P1AttackType.RIGHT_LEG);
                break;
            case P1BossStateID.TURN:
                enemyController.StateMachine.ChangeState<P1BossTurnState>();
                break;
        }

        return Status.Success;
    }
}

