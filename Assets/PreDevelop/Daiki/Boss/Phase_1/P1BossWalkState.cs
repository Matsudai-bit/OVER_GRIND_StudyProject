using System;
using System.Collections;
using UnityEngine;

public class P1BossWalkState : StateBase<P1BossController>
{
    protected override void OnStartState()
    {
        Owner.stateText.text = "Walk";
        Debug.Log("歩行状態の開始");

        Owner.Animator.SetBool("Walk", true);
        Owner.SetStateExecutionStatus(StateExecutionStatus.RUNNING);

        // コルーチンの起動
       
        Owner.StartDelayCoroutine(3, () =>
        {
            Debug.Log("歩行状態の成功");

            // 3秒後にここの処理が実行される
            Owner.SetStateExecutionStatus(StateExecutionStatus.SUCCEEDED);

        });

    }



    protected override void OnFixedUpdate()
    {
        HandleMovement();
    }


    protected override void OnExitState()
    {
        
        Owner.Animator.SetBool("Walk", false);
    }

    private void HandleMovement()
    {
        var rb = Owner.Rigidbody;
        
        Vector3 targetVelocity = Owner.transform.forward * 100.0f;

        // 水平速度を滑らかに目標値へ近づける
        Vector3 currentHorizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 newHorizontal = Vector3.MoveTowards(currentHorizontal, targetVelocity, 100.0f * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(newHorizontal.x, rb.linearVelocity.y, newHorizontal.z);

     
    }
 
}
