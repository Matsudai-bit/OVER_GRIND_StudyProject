using Unity.Behavior;
using UnityEngine;

public class P1BossWalkState : StateBase<P1BossController>
{
    protected override void OnStartState()
    {
        Owner.Animator.SetBool("Walk", true);
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

        // êÖïΩë¨ìxÇääÇÁÇ©Ç…ñ⁄ïWílÇ÷ãﬂÇ√ÇØÇÈ
        Vector3 currentHorizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 newHorizontal = Vector3.MoveTowards(currentHorizontal, targetVelocity, 100.0f * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(newHorizontal.x, rb.linearVelocity.y, newHorizontal.z);

     
    }
}
