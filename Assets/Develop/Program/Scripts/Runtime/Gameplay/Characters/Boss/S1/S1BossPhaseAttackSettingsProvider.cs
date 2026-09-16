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

    // Hitbox IDとAttackHitboxの対応
    [SerializeField, Header("Hitbox対応")]
    private List<AttackHitboxBinding<S1BossHitboxId>>
        m_hitboxBindings = new();

    // Attack IDと使用するHitbox IDの対応
    [SerializeField, Header("攻撃Hitbox設定")]
    private List<AttackHitboxGroup<S1BossHitboxId>>
        m_hitboxGroups = new();

    // ルートに配置されている攻撃ダメージ管理
    [SerializeField, Header("参照")]
    private S1BossAttackDamageController m_attackDamageController;

    // ルートに配置されている攻撃Hitbox管理
    [SerializeField]
    private AttackHitboxRegistry m_attackHitboxRegistry;

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

        if (!TryCreateRuntimeHitboxGroups(
                out List<AttackHitboxRuntimeGroup> runtimeGroups))
        {
            return false;
        }

        if (!m_attackDamageController.SetDamageSettings(
                m_parameterAsset,
                m_hitboxBindings))
        {
            return false;
        }

        if (!m_attackHitboxRegistry.SetHitboxGroups(
                runtimeGroups))
        {
            m_attackDamageController.ClearDamageSettings();

            return false;
        }

        return true;
    }

    /// <summary>
    /// このフェーズで使用していた攻撃ダメージ設定を解除します。
    /// </summary>
    public override void ClearDamageSettings()
    {
        ResolveReferences();

        if (m_attackHitboxRegistry != null)
        {
            m_attackHitboxRegistry.ClearHitboxGroups();
        }

        if (m_attackDamageController != null)
        {
            m_attackDamageController.ClearDamageSettings();
        }
    }

    /// <summary>
    /// Hitbox ID設定から実行時のAttackHitbox群を生成します。
    /// </summary>
    /// <param name="runtimeGroups">生成した実行時Hitbox情報。</param>
    /// <returns>
    /// true：生成しました。
    /// false：設定に不備があります。
    /// </returns>
    private bool TryCreateRuntimeHitboxGroups(
        out List<AttackHitboxRuntimeGroup> runtimeGroups)
    {
        runtimeGroups = new List<AttackHitboxRuntimeGroup>();

        if (!TryCreateHitboxMap(
                out Dictionary<S1BossHitboxId, AttackHitbox> hitboxMap))
        {
            return false;
        }

        bool isValid = true;
        HashSet<AttackIdentifier> registeredAttackIds = new();

        foreach (AttackHitboxGroup<S1BossHitboxId> hitboxGroup
                 in m_hitboxGroups)
        {
            if (hitboxGroup == null)
            {
                Debug.LogError(
                    "攻撃Hitbox設定に未設定の要素があります。",
                    this);

                isValid = false;
                continue;
            }

            if (hitboxGroup.AttackIdentifier == null)
            {
                Debug.LogError(
                    "Attack IDが設定されていない攻撃Hitbox設定があります。",
                    this);

                isValid = false;
                continue;
            }

            if (!registeredAttackIds.Add(
                    hitboxGroup.AttackIdentifier))
            {
                Debug.LogError(
                    $"{hitboxGroup.AttackIdentifier.name} が重複しています。",
                    this);

                isValid = false;
                continue;
            }

            List<AttackHitbox> attackHitboxes = new();
            HashSet<S1BossHitboxId> registeredHitboxIds = new();

            foreach (S1BossHitboxId hitboxId
                     in hitboxGroup.HitboxIds)
            {
                if (!registeredHitboxIds.Add(
                        hitboxId))
                {
                    Debug.LogError(
                        $"{hitboxGroup.AttackIdentifier.name} のHitbox ID " +
                        $"{hitboxId} が重複しています。",
                        this);

                    isValid = false;
                    continue;
                }

                if (!hitboxMap.TryGetValue(
                        hitboxId,
                        out AttackHitbox attackHitbox))
                {
                    Debug.LogError(
                        $"{hitboxGroup.AttackIdentifier.name} で使用する" +
                        $"Hitbox ID {hitboxId} の対応先がありません。",
                        this);

                    isValid = false;
                    continue;
                }

                attackHitboxes.Add(
                    attackHitbox);
            }

            runtimeGroups.Add(
                new AttackHitboxRuntimeGroup(
                    hitboxGroup.AttackIdentifier,
                    attackHitboxes));
        }

        if (!isValid)
        {
            runtimeGroups.Clear();
        }

        return isValid;
    }

    /// <summary>
    /// Hitbox IDからAttackHitboxを取得する検索テーブルを生成します。
    /// </summary>
    /// <param name="hitboxMap">生成した検索テーブル。</param>
    /// <returns>
    /// true：生成しました。
    /// false：設定に不備があります。
    /// </returns>
    private bool TryCreateHitboxMap(
        out Dictionary<S1BossHitboxId, AttackHitbox> hitboxMap)
    {
        hitboxMap =
            new Dictionary<S1BossHitboxId, AttackHitbox>();

        bool isValid = true;
        HashSet<AttackHitbox> registeredHitboxes = new();

        foreach (AttackHitboxBinding<S1BossHitboxId> binding
                 in m_hitboxBindings)
        {
            if (binding == null)
            {
                Debug.LogError(
                    "Hitbox対応に未設定の要素があります。",
                    this);

                isValid = false;
                continue;
            }

            if (binding.AttackHitbox == null)
            {
                Debug.LogError(
                    $"Hitbox ID {binding.HitboxId} に" +
                    $"{nameof(AttackHitbox)}が設定されていません。",
                    this);

                isValid = false;
                continue;
            }

            if (!hitboxMap.TryAdd(
                    binding.HitboxId,
                    binding.AttackHitbox))
            {
                Debug.LogError(
                    $"Hitbox ID {binding.HitboxId} が重複しています。",
                    this);

                isValid = false;
            }

            if (!registeredHitboxes.Add(
                    binding.AttackHitbox))
            {
                Debug.LogError(
                    $"{binding.AttackHitbox.name} が複数のHitbox IDに" +
                    "登録されています。",
                    this);

                isValid = false;
            }
        }

        if (!isValid)
        {
            hitboxMap.Clear();
        }

        return isValid;
    }

    /// <summary>
    /// 必要な参照を取得します。
    /// </summary>
    private void ResolveReferences()
    {
        if (m_attackDamageController == null)
        {
            m_attackDamageController =
                GetComponentInParent<S1BossAttackDamageController>(
                    true);
        }

        if (m_attackHitboxRegistry == null)
        {
            m_attackHitboxRegistry =
                GetComponentInParent<AttackHitboxRegistry>(
                    true);
        }
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

        if (m_attackHitboxRegistry == null)
        {
            Debug.LogError(
                $"{nameof(AttackHitboxRegistry)}が見つかりません。",
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

        if (m_hitboxGroups == null)
        {
            Debug.LogError(
                "攻撃Hitbox設定がありません。",
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
