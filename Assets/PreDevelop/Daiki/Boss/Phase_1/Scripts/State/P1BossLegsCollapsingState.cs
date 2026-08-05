using UnityEngine;
using UnityEngine.SceneManagement;

public class P1BossLegsCollapsingState : StateBase<P1BossController>
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void OnStartState()
    {
        if (!Owner.P1_boss.gameObject.activeSelf) { return; }

        Owner.stateText.text = "LegsCollapsingState";
        Owner.SetStateExecutionStatus(StateExecutionStatus.RUNNING);
        Owner.LegController.DestroyLegParts();

        Owner.GroundCollider.transform.position = new Vector3(Owner.GroundCollider.transform.position.x, Owner.GroundCollider.transform.position.y + 3.4f, Owner.GroundCollider.transform.position.z) ;
        Owner.P1_boss.gameObject.SetActive(false);
        Owner.P2_boss.gameObject.SetActive(true);

        Owner.StartDelayCoroutine(5.0f, () =>
        {
            SceneManager.LoadScene("Prot_Stage1_Phase2");

        });
    }

    void ChangeScene()
    {
    }

    // Update is called once per frame
    protected override void OnUpdate(float deltaTime)
    {
        
        Owner.SetStateExecutionStatus(StateExecutionStatus.SUCCEEDED);
    }
}
