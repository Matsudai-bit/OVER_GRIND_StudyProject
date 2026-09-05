using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻撃IDに対応するダメージを各AttackHitboxへ設定します。
/// </summary>
/// <typeparam name="THitboxId">Hitboxを識別する列挙型。</typeparam>
[DisallowMultipleComponent]
public abstract class AttackDamageController<THitboxId> :
    AttackDamageControllerBase
    where THitboxId : struct, Enum
{
    // 現在フェーズで使用するダメージパラメータ
    private AttackDamageParameterAsset<THitboxId> m_parameterAsset;

    // Hitbox IDからAttackHitboxを取得する検索テーブル
    private readonly Dictionary<THitboxId, AttackHitbox>
        m_hitboxMap = new();

    /// <summary>
    /// 現在フェーズで使用する攻撃ダメージ設定を設定します。
    /// </summary>
    /// <param name="parameterAsset">使用するダメージパラメータ。</param>
    /// <param name="hitboxBindings">Hitbox IDとAttackHitboxの対応。</param>
    /// <returns>
    /// true：設定しました。
    /// false：設定内容に不備があります。
    /// </returns>
    public bool SetDamageSettings(
        AttackDamageParameterAsset<THitboxId> parameterAsset,
        IReadOnlyList<AttackHitboxBinding<THitboxId>> hitboxBindings)
    {
        if (parameterAsset == null)
        {
            Debug.LogError(
                "攻撃ダメージパラメータが設定されていません。",
                this);

            return false;
        }

        if (!ValidateHitboxBindings(
                hitboxBindings))
        {
            return false;
        }

        ClearDamageSettings();

        m_parameterAsset =
            parameterAsset;

        foreach (AttackHitboxBinding<THitboxId> binding
                 in hitboxBindings)
        {
            m_hitboxMap.Add(
                binding.HitboxId,
                binding.AttackHitbox);
        }

        ResetHitboxDamages();

        return true;
    }

    /// <summary>
    /// 現在フェーズの攻撃ダメージ設定を解除します。
    /// </summary>
    public void ClearDamageSettings()
    {
        ResetHitboxDamages();

        m_parameterAsset = null;
        m_hitboxMap.Clear();
    }

    /// <summary>
    /// 指定した攻撃のダメージを使用するHitboxへ設定します。
    /// </summary>
    /// <param name="attackIdentifier">開始する攻撃ID。</param>
    /// <returns>
    /// true：設定に成功しました。
    /// false：攻撃パラメータまたはHitbox情報を取得できませんでした。
    /// </returns>
    public override bool ApplyDamageParameters(
        AttackIdentifier attackIdentifier)
    {
        if (m_parameterAsset == null)
        {
            Debug.LogWarning(
                "攻撃ダメージパラメータが設定されていません。",
                this);

            return false;
        }

        if (attackIdentifier == null)
        {
            Debug.LogWarning(
                "Attack IDが設定されていません。",
                this);

            return false;
        }

        if (!m_parameterAsset.TryGetAttackParameter(
                attackIdentifier,
                out AttackDamageParameter<THitboxId> attackParameter))
        {
            Debug.LogWarning(
                $"{attackIdentifier.name} のダメージパラメータがありません。",
                this);

            return false;
        }

        ResetHitboxDamages();

        IReadOnlyList<HitboxDamageParameter<THitboxId>> hitboxParameters =
            attackParameter.HitboxDamageParameters;

        if (hitboxParameters == null)
        {
            return true;
        }

        bool isValid = true;

        foreach (HitboxDamageParameter<THitboxId> hitboxParameter
                 in hitboxParameters)
        {
            if (hitboxParameter == null)
            {
                continue;
            }

            if (!m_hitboxMap.TryGetValue(
                    hitboxParameter.HitboxId,
                    out AttackHitbox attackHitbox))
            {
                Debug.LogWarning(
                    $"{attackIdentifier.name} のHitbox ID " +
                    $"{hitboxParameter.HitboxId} に対応する" +
                    $"{nameof(AttackHitbox)}がありません。",
                    this);

                isValid = false;
                continue;
            }

            attackHitbox.SetDamage(
                hitboxParameter.Damage);
        }

        return isValid;
    }

    /// <summary>
    /// 登録されている全Hitboxのダメージを初期値へ戻します。
    /// </summary>
    public void ResetHitboxDamages()
    {
        foreach (AttackHitbox attackHitbox
                 in m_hitboxMap.Values)
        {
            if (attackHitbox == null)
            {
                continue;
            }

            attackHitbox.ResetDamage();
        }
    }

    /// <summary>
    /// Hitbox対応に不正な設定がないか確認します。
    /// </summary>
    /// <param name="hitboxBindings">確認するHitbox対応。</param>
    /// <returns>
    /// true：設定は有効です。
    /// false：設定に不備があります。
    /// </returns>
    private bool ValidateHitboxBindings(
        IReadOnlyList<AttackHitboxBinding<THitboxId>> hitboxBindings)
    {
        if (hitboxBindings == null)
        {
            Debug.LogError(
                "Hitbox対応が設定されていません。",
                this);

            return false;
        }

        bool isValid = true;

        HashSet<THitboxId> registeredIds = new();
        HashSet<AttackHitbox> registeredHitboxes = new();

        foreach (AttackHitboxBinding<THitboxId> binding
                 in hitboxBindings)
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

            if (!registeredIds.Add(
                    binding.HitboxId))
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

        return isValid;
    }
}
