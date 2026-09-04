using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ1ボスのフェーズごとの攻撃ダメージ設定を保持します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1BossPhaseAttackSettingsProvider :
    BossPhaseAttackSettingsProvider
{
    // フェーズで使用する攻撃ダメージパラメータ
    [SerializeField, Header("攻撃ダメージ設定")]
    private S1BossAttackDamageParameterAsset m_parameterAsset;

    // フェーズで使用するHitbox対応
    [SerializeField, Header("Hitbox対応")]
    private List<AttackHitboxBinding<S1BossHitboxId>> m_hitboxBindings = new();

    // ルートに配置されている攻撃ダメージ管理
    [SerializeField, Header("参照")]
    private S1BossAttackDamageController m_attackDamageController;

    /// <summary>
    /// このフェーズの攻撃ダメージ設定を適用します。
    /// </summary>
    /// <returns>
    /// true：設定に成功しました。
    /// false：設定に失敗しました。
    /// </returns>
    public override bool ApplyDamageSettings()
    {
        ResolveReferences();

        if (!ValidateReferences())
        {
            return false;
        }

        return m_attackDamageController.SetDamageSettings(
            m_parameterAsset,
            m_hitboxBindings);
    }

    /// <summary>
    /// このフェーズで使用していた攻撃ダメージ設定を解除します。
    /// </summary>
    public override void ClearDamageSettings()
    {
        ResolveReferences();

        if (m_attackDamageController == null)
        {
            return;
        }

        m_attackDamageController.ClearDamageSettings();
    }

    /// <summary>
    /// 必要な参照を取得します。
    /// </summary>
    private void ResolveReferences()
    {
        if (m_attackDamageController != null)
        {
            return;
        }

        m_attackDamageController =
            GetComponentInParent<S1BossAttackDamageController>(
                true);
    }

    /// <summary>
    /// 必要な設定が揃っているか確認します。
    /// </summary>
    /// <returns>
    /// true：設定されています。
    /// false：設定が不足しています。
    /// </returns>
    private bool ValidateReferences()
    {
        bool isValid = true;

        if (m_attackDamageController == null)
        {
            Debug.LogError(
                $"{nameof(S1BossAttackDamageController)}が見つかりません。",
                this);

            isValid = false;
        }

        if (m_parameterAsset == null)
        {
            Debug.LogError(
                $"{nameof(S1BossAttackDamageParameterAsset)}が設定されていません。",
                this);

            isValid = false;
        }

        if (m_hitboxBindings == null)
        {
            Debug.LogError(
                "Hitbox対応が設定されていません。",
                this);

            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// Inspector設定時に参照を自動取得します。
    /// </summary>
    private void Reset()
    {
        ResolveReferences();
    }
}
