using Unity.Behavior;
using UnityEngine;

/// <summary>
/// ボスのゲームプレイ中の操作可否を制御します。
/// </summary>
[DisallowMultipleComponent]
public sealed class BossGameplayControl
    : MonoBehaviour, IActorControllable
{
    // ボス全体制御
    [SerializeField, Header("参照")]
    private BossController m_bossController;

    /// <summary>
    /// 現在ゲームプレイ操作が有効か取得します。
    /// </summary>
    public bool IsControlEnabled { get; private set; }

    private void Awake()
    {
        ResolveReferences();
    }

    /// <summary>
    /// ゲームプレイ操作の有効状態を設定します。
    /// </summary>
    /// <param name="isEnabled">
    /// true：操作を有効化します。
    /// false：操作を無効化します。
    /// </param>
    public void SetControlEnabled(
        bool isEnabled)
    {
        if (!TryGetPhaseController(
            out BossPhaseController phaseController))
        {
            Debug.LogError(
                $"{nameof(BossPhaseController)} を取得できないため、"
                + "ボスの操作状態を変更できません。",
                this);

            return;
        }

        IsControlEnabled =
            isEnabled;

        if (isEnabled)
        {
            EnableGameplayControl(
                phaseController);

            return;
        }

        DisableGameplayControl(
            phaseController);
    }

    /// <summary>
    /// ボスのゲームプレイ操作を有効化します。
    /// </summary>
    /// <param name="phaseController">
    /// 現在使用するフェーズ管理。
    /// </param>
    private void EnableGameplayControl(
        BossPhaseController phaseController)
    {
        if (!phaseController.TryGetCurrentPhaseComponent(
            out BehaviorGraphAgent agent))
        {
            return;
        }

        if (agent == null)
        {
            Debug.LogWarning(
                $"{nameof(BehaviorGraphAgent)} の取得結果がnullです。",
                this);

            return;
        }

        agent.enabled =
            true;
    }

    /// <summary>
    /// ボスのゲームプレイ操作を無効化します。
    /// </summary>
    /// <param name="phaseController">
    /// 現在使用するフェーズ管理。
    /// </param>
    private void DisableGameplayControl(
        BossPhaseController phaseController)
    {
        if (!phaseController.TryGetCurrentPhaseComponent(
            out BehaviorGraphAgent agent))
        {
            return;
        }

        if (agent == null)
        {
            Debug.LogWarning(
                $"{nameof(BehaviorGraphAgent)} の取得結果がnullです。",
                this);

            return;
        }

        agent.enabled =
            false;
    }

    /// <summary>
    /// BossController参照を取得します。
    /// </summary>
    private void ResolveReferences()
    {
        if (m_bossController != null)
        {
            return;
        }

        m_bossController =
            GetComponentInParent<BossController>(
                true);
    }

    /// <summary>
    /// 使用するBossPhaseControllerを取得します。
    /// </summary>
    /// <param name="phaseController">
    /// 取得したフェーズ管理。
    /// </param>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetPhaseController(
        out BossPhaseController phaseController)
    {
        phaseController =
            null;

        ResolveReferences();

        if (m_bossController == null)
        {
            Debug.LogError(
                $"{nameof(BossController)} が設定されていません。",
                this);

            return false;
        }

        phaseController =
            m_bossController.PhaseController;

        /*
         * BossControllerのAwakeがまだ実行されていないケースでも
         * RequireComponentで存在するBossPhaseControllerを直接取得します。
         */
        if (phaseController == null)
        {
            phaseController =
                m_bossController.GetComponent<BossPhaseController>();
        }

        return phaseController != null;
    }

    private void Reset()
    {
        ResolveReferences();
    }
}
