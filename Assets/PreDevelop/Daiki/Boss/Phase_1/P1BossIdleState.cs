using Unity.Behavior;
using UnityEngine;

public class P1BossIdleState : StateBase<P1BossController>
{
    protected override void OnStartState()
    {
        Debug.Log("待機状態の開始");
        Owner.SetStateExecutionStatus(StateExecutionStatus.RUNNING);

        // コルーチンの起動

        Owner.StartDelayCoroutine(3, () =>
        {
            Debug.Log("待機状態の成功");
            // 3秒後にここの処理が実行される
            Owner.SetStateExecutionStatus(StateExecutionStatus.SUCCEEDED);

        });

    }
}
