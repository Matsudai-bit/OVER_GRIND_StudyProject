using UnityEngine;

public class P1BossLegsCollapsingState : StateBase<P1BossController>
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void OnStartState()
    {
        Owner.stateText.text = "LegsCollapsingState";
        Owner.SetStateExecutionStatus(StateExecutionStatus.RUNNING);
        Owner.LegController.DestroyLegParts();

    }

    // Update is called once per frame
    protected override void OnUpdate(float deltaTime)
    {
        
        Owner.SetStateExecutionStatus(StateExecutionStatus.SUCCEEDED);
    }
}
