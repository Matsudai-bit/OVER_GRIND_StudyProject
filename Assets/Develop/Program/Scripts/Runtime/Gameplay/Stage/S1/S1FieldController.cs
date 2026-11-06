using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ1のフェーズごとのフィールド表示を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1FieldController : MonoBehaviour
{
    // ボスフェーズ変更イベント
    [SerializeField, Header("イベント")]
    private BossPhaseEventChannel m_eventChannel;

    // フェーズごとのフィールドオブジェクト
    [SerializeField, Header("フェーズ別フィールド")]
    private SerializeDictionary<BossPhaseID, List<GameObject>>
        m_phaseFieldGroup;

    /// <summary>
    /// イベントを購読します。
    /// </summary>
    private void OnEnable()
    {
        if (m_eventChannel == null)
        {
            Debug.LogError(
                $"[{nameof(S1FieldController)}] " +
                $"{nameof(BossPhaseEventChannel)}が設定されていません。",
                this);

            return;
        }

        m_eventChannel.PhaseChanged +=
            HandlePhaseChanged;

        // PhaseController側のAwakeが先に終わっていても初期状態を同期
        if (m_eventChannel.HasCurrentPhase)
        {
            ApplyPhase(
                m_eventChannel.CurrentPhase);
        }
    }

    /// <summary>
    /// イベント購読を解除します。
    /// </summary>
    private void OnDisable()
    {
        if (m_eventChannel == null)
        {
            return;
        }

        m_eventChannel.PhaseChanged -=
            HandlePhaseChanged;
    }


    /// <summary>
    /// フィールドグループの有効状態を変更します。
    /// </summary>
    /// <param name="fieldObjects">
    /// 対象のフィールドオブジェクト。
    /// </param>
    /// <param name="isActive">
    /// 有効にする場合はtrue。
    /// </param>
    private static void SetFieldGroupActive(
        List<GameObject> fieldObjects,
        bool isActive)
    {
        if (fieldObjects == null)
        {
            return;
        }

        foreach (GameObject fieldObject in fieldObjects)
        {
            if (fieldObject == null)
            {
                continue;
            }

            fieldObject.SetActive(
                isActive);
        }
    }
    private void HandlePhaseChanged(
    BossPhaseID previousID,
    BossPhaseID nextID)
    {
        ApplyPhase(nextID);
    }

    /// <summary>
    /// 指定フェーズのフィールドを有効化します。
    /// </summary>
    private void ApplyPhase(
        BossPhaseID phaseID)
    {
        foreach (
            KeyValuePair<BossPhaseID, List<GameObject>> pair
            in m_phaseFieldGroup)
        {
            bool isActive =
                pair.Key == phaseID;

            SetFieldGroupActive(
                pair.Value,
                isActive);
        }
    }
}