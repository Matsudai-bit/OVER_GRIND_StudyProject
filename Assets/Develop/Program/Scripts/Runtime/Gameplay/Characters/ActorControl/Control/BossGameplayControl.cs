using Unity.Behavior;
using UnityEngine;

public sealed class BossGameplayControl
    : MonoBehaviour, IActorControllable
{
    [SerializeField]
    private BossController m_bossController;
 
    public bool IsControlEnabled { get; private set; }

    public void SetControlEnabled(bool isEnabled)
    {
        IsControlEnabled = isEnabled;

        if (isEnabled)
        {
            EnableGameplayControl();
        }
        else
        {
            DisableGameplayControl();
        }
    }

    private void EnableGameplayControl()
    {
        if (m_bossController)
        {
            if (m_bossController.PhaseController.TryGetCurrentPhaseComponent(out BehaviorGraphAgent agent))
            {
                agent.enabled = true;   

            }

        }
    }

    private void DisableGameplayControl()
    {
        if (m_bossController)
        {
            if (m_bossController.PhaseController.TryGetCurrentPhaseComponent(out BehaviorGraphAgent agent))
            {
                agent.enabled = false;

            }

        }
    }

}