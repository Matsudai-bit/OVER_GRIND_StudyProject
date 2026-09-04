using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻撃IDに対応するダメージを各AttackHitboxへ設定します。
/// </summary>
/// <typeparam name="THitboxId">Hitboxを識別する列挙型。</typeparam>
[DisallowMultipleComponent]
public abstract class AttackDamageController<THitboxId>
    : AttackDamageControllerBase
    where THitboxId : struct, Enum
{
    // 現在使用するダメージパラメータ
    private AttackDamageParameterAsset<THitboxId> m_parameterAsset;

    // 現在使用するHitbox IDとAttackHitboxの対応
    private readonly List<AttackHitboxBinding<THitboxId>> m_hitboxBindings = new();

    /// <summary>
    /// ダメージパラメータとHitbox対応をまとめて設定します。
    /// </summary>
    /// <param name="parameterAsset">使用するダメージパラメータ。</param>
    /// <param name="hitboxBindings">使用するHitbox対応。</param>
    /// <returns>
    /// true：設定に成功しました。
    /// false：設定内容が不正です。
    /// </returns>
    public bool SetDamageSettings(
        AttackDamageParameterAsset<THitboxId> parameterAsset,
        IReadOnlyList<AttackHitboxBinding<THitboxId>> hitboxBindings)
    {
        if (parameterAsset == null)
        {
            Debug.LogError(
                $"{nameof(AttackDamageParameterAsset<THitboxId>)}が設定されていません。",
                this);
            return false;
        }

        if (!ValidateHitboxBindings(hitboxBindings))
        {
            return false;
        }

        // 前の設定で使用していたHitboxを停止してから差し替えます。
        DisableHitboxes();
        ResetHitboxDamages();

        m_parameterAsset = parameterAsset;

        m_hitboxBindings.Clear();

        foreach (AttackHitboxBinding<THitboxId> binding in hitboxBindings)
        {
            m_hitboxBindings.Add(binding);
        }

        // 新しいHitbox側に以前のダメージ設定が残らないようにします。
        ResetHitboxDamages();

        return true;
    }

    /// <summary>
    /// 現在のダメージ設定を解除します。
    /// </summary>
    public void ClearDamageSettings()
    {
        DisableHitboxes();
        ResetHitboxDamages();

        m_parameterAsset = null;
        m_hitboxBindings.Clear();
    }

    /// <summary>
    /// 指定した攻撃のダメージを使用するHitboxへ設定します。
    /// </summary>
    /// <param name="attackIdentifier">開始する攻撃ID。</param>
    /// <returns>
    /// true：設定に成功しました。
    /// false：攻撃パラメータを取得できませんでした。
    /// </returns>
    public override bool ApplyDamageParameters(AttackIdentifier attackIdentifier)
    {
        if (m_parameterAsset == null)
        {
            Debug.LogWarning(
                $"{nameof(AttackDamageParameterAsset<THitboxId>)}が設定されていません。",
                this);
            return false;
        }

        if (attackIdentifier == null)
        {
            Debug.LogWarning(
                $"{nameof(AttackIdentifier)}が設定されていません。",
                this);
            return false;
        }

        if (!m_parameterAsset.TryGetAttackParameter(
                attackIdentifier,
                out AttackDamageParameter<THitboxId> attackParameter))
        {
            Debug.LogWarning(
                "指定した攻撃のダメージパラメータがありません。",
                this);
            return false;
        }

        // 前の攻撃で設定されたダメージを残さないようにします。
        ResetHitboxDamages();

        IReadOnlyList<HitboxDamageParameter<THitboxId>> hitboxParameters =
            attackParameter.HitboxDamageParameters;

        if (hitboxParameters == null)
        {
            return true;
        }

        foreach (HitboxDamageParameter<THitboxId> hitboxParameter in hitboxParameters)
        {
            if (hitboxParameter == null)
            {
                continue;
            }

            if (!TryGetHitbox(
                    hitboxParameter.HitboxId,
                    out AttackHitbox attackHitbox))
            {
                Debug.LogWarning(
                    $"Hitbox ID {hitboxParameter.HitboxId} に対応する" +
                    $"{nameof(AttackHitbox)}がありません。",
                    this);
                continue;
            }

            attackHitbox.SetDamage(
                hitboxParameter.Damage);
        }

        return true;
    }

    /// <summary>
    /// 全Hitboxのダメージを初期値へ戻します。
    /// </summary>
    public void ResetHitboxDamages()
    {
        foreach (AttackHitboxBinding<THitboxId> binding in m_hitboxBindings)
        {
            if (binding?.AttackHitbox == null)
            {
                continue;
            }

            binding.AttackHitbox.ResetDamage();
        }
    }

    /// <summary>
    /// Hitbox IDに対応するAttackHitboxを取得します。
    /// </summary>
    /// <param name="hitboxId">取得するHitbox ID。</param>
    /// <param name="attackHitbox">取得したAttackHitbox。</param>
    /// <returns>
    /// true：AttackHitboxを取得しました。
    /// false：対応するAttackHitboxがありません。
    /// </returns>
    public bool TryGetHitbox(
        THitboxId hitboxId,
        out AttackHitbox attackHitbox)
    {
        attackHitbox = null;

        EqualityComparer<THitboxId> comparer =
            EqualityComparer<THitboxId>.Default;

        foreach (AttackHitboxBinding<THitboxId> binding in m_hitboxBindings)
        {
            if (binding == null ||
                binding.AttackHitbox == null ||
                !comparer.Equals(
                    binding.HitboxId,
                    hitboxId))
            {
                continue;
            }

            attackHitbox = binding.AttackHitbox;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 現在登録されているHitboxをすべて無効化します。
    /// </summary>
    private void DisableHitboxes()
    {
        foreach (AttackHitboxBinding<THitboxId> binding in m_hitboxBindings)
        {
            if (binding?.AttackHitbox == null)
            {
                continue;
            }

            binding.AttackHitbox.DisableHitbox();
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

        HashSet<THitboxId> registeredIds = new();
        HashSet<AttackHitbox> registeredHitboxes = new();

        foreach (AttackHitboxBinding<THitboxId> binding in hitboxBindings)
        {
            if (binding == null)
            {
                Debug.LogError(
                    "Hitbox対応に未設定の要素があります。",
                    this);
                return false;
            }

            if (binding.AttackHitbox == null)
            {
                Debug.LogError(
                    $"Hitbox ID {binding.HitboxId} に" +
                    $"{nameof(AttackHitbox)}が設定されていません。",
                    this);
                return false;
            }

            if (!registeredIds.Add(binding.HitboxId))
            {
                Debug.LogError(
                    $"Hitbox ID {binding.HitboxId} が重複しています。",
                    this);
                return false;
            }

            if (!registeredHitboxes.Add(binding.AttackHitbox))
            {
                Debug.LogError(
                    $"{binding.AttackHitbox.name} が複数のHitbox IDに登録されています。",
                    binding.AttackHitbox);
                return false;
            }
        }

        return true;
    }
}
