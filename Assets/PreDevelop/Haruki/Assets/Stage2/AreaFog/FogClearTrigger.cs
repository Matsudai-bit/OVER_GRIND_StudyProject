using UnityEngine;

/// <summary>
/// トリガー作動時に実行する霧の制御アクションの種類。
/// </summary>
public enum FogTriggerAction
{
    FOG_ACTION_CLEAR,
    FOG_ACTION_FILL,
    FOG_ACTION_SET_CUSTOM
}

/// <summary>
/// コライダーへの侵入・退出や外部イベントを検知して霧を晴らすトリガークラス。
/// </summary>
[RequireComponent(typeof(Collider))]
public class FogClearTrigger : MonoBehaviour
{
    // デフォルトの対象タグ名
    private const string DEFAULT_TARGET_TAG = "Player";

    // 霧の量の最小値・最大値
    private const float MIN_FOG_AMOUNT = 0.0f;
    private const float MAX_FOG_AMOUNT = 1.0f;

    /// <summary>
    /// 制御対象のローカルハイトフォグコントローラー。
    /// </summary>
    [Header("制御対象の参照")]
    [SerializeField]
    private LocalHeightFogController m_fogController;

    /// <summary>
    /// トリガーに反応させるオブジェクトのタグ名。
    /// </summary>
    [Header("トリガー条件設定")]
    [SerializeField]
    private string m_targetTag = DEFAULT_TARGET_TAG;

    /// <summary>
    /// 一度だけ発動させるかどうか。
    /// </summary>
    [SerializeField]
    private bool m_triggerOnce = false;

    /// <summary>
    /// トリガー侵入時に実行するアクション。
    /// </summary>
    [Header("霧の制御アクション設定")]
    [SerializeField]
    private FogTriggerAction m_enterAction = FogTriggerAction.FOG_ACTION_CLEAR;

    /// <summary>
    /// カスタム指定時の目標の霧の量（0.0〜1.0）。
    /// </summary>
    [SerializeField, Range(MIN_FOG_AMOUNT, MAX_FOG_AMOUNT)]
    private float m_customFogAmount = 0.2f;

    /// <summary>
    /// 霧が変化するのにかかる時間（秒）。
    /// </summary>
    [SerializeField, Min(0.0f)]
    private float m_transitionDuration = 3.0f;

    /// <summary>
    /// トリガーから出た際に霧を元の状態へ戻すかどうか。
    /// </summary>
    [Header("トリガー退出時の設定")]
    [SerializeField]
    private bool m_revertOnExit = false;

    /// <summary>
    /// トリガーから出た際に戻す霧の量（0.0〜1.0）。
    /// </summary>
    [SerializeField, Range(MIN_FOG_AMOUNT, MAX_FOG_AMOUNT)]
    private float m_exitFogAmount = 1.0f;

    // すでにトリガーが発動済みかどうかを記録するフラグ
    private bool m_hasTriggered;

    /// <summary>
    /// トリガーに他のコライダーが侵入した際の処理です。
    /// </summary>
    /// <param name="other">侵入したコライダー。</param>
    private void OnTriggerEnter(Collider other)
    {
        if (!CanActivateTrigger(other))
        {
            return;
        }

        ExecuteTriggerAction();
    }

    /// <summary>
    /// トリガーから他のコライダーが退出した際の処理です。
    /// </summary>
    /// <param name="other">退出したコライダー。</param>
    private void OnTriggerExit(Collider other)
    {
        if (!m_revertOnExit)
        {
            return;
        }

        if (other == null || !other.CompareTag(m_targetTag))
        {
            return;
        }

        ExecuteExitAction();
    }

    /// <summary>
    /// 接触したコライダーがトリガーの発動条件を満たしているか判定します。
    /// </summary>
    /// <param name="targetCollider">判定対象のコライダー。</param>
    /// <returns>
    /// true：発動条件を満たしています。
    /// false：発動条件を満たしていません。
    /// </returns>
    public bool CanActivateTrigger(Collider targetCollider)
    {
        // nullチェック
        if (targetCollider == null || m_fogController == null)
        {
            return false;
        }

        // 一度きりの設定ですでに発動済みの場合は不可
        if (m_triggerOnce && m_hasTriggered)
        {
            return false;
        }

        // タグの一致確認
        return targetCollider.CompareTag(m_targetTag);
    }

    /// <summary>
    /// 設定された侵入時アクション（霧を晴らす等）を実行します。外部スクリプトやUIからも呼び出し可能です。
    /// </summary>
    public void ExecuteTriggerAction()
    {
        if (m_fogController == null)
        {
            Debug.LogWarning("FogClearTrigger: 制御対象の LocalHeightFogController が設定されていません。", this);
            return;
        }

        m_hasTriggered = true;
        ApplyFogAction(m_enterAction, m_customFogAmount, m_transitionDuration);
    }

    /// <summary>
    /// トリガー退出時の霧を戻すアクションを実行します。
    /// </summary>
    public void ExecuteExitAction()
    {
        if (m_fogController == null)
        {
            return;
        }

        m_fogController.TransitionFogAmount(m_exitFogAmount, m_transitionDuration);
    }

    /// <summary>
    /// アクション種別に応じてコントローラーへ霧の変化を指示します。
    /// </summary>
    /// <param name="actionType">実行するアクションの種類。</param>
    /// <param name="customAmount">カスタム設定時の霧の量。</param>
    /// <param name="duration">変化にかける秒数。</param>
    private void ApplyFogAction(FogTriggerAction actionType, float customAmount, float duration)
    {
        switch (actionType)
        {
            case FogTriggerAction.FOG_ACTION_CLEAR:
                m_fogController.ClearFog(duration);
                break;

            case FogTriggerAction.FOG_ACTION_FILL:
                m_fogController.FillFog(duration);
                break;

            case FogTriggerAction.FOG_ACTION_SET_CUSTOM:
                m_fogController.TransitionFogAmount(customAmount, duration);
                break;
        }
    }
}