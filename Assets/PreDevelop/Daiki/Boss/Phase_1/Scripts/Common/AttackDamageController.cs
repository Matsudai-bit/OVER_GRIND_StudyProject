using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻撃IDに対応するダメージを各AttackHitboxへ設定します。
/// </summary>
/// <typeparam name="THitboxId">Hitboxを識別する列挙型。</typeparam>
[DisallowMultipleComponent]
public abstract class AttackDamageController<THitboxId> : MonoBehaviour
    where THitboxId : struct, Enum
{
    // 現在使用するダメージパラメータ
    [SerializeField, Header("ダメージパラメータ")]
    private AttackDamageParameterAsset<THitboxId> m_parameterAsset;

    // Hitbox IDとAttackHitboxの対応
    [SerializeField, Header("Hitbox対応")]
    private List<AttackHitboxBinding<THitboxId>> m_hitboxBindings = new();

    /// <summary>
    /// 現在使用しているダメージパラメータを設定します。
    /// </summary>
    /// <param name="parameterAsset">使用するダメージパラメータ。</param>
    public void SetParameterAsset(
        AttackDamageParameterAsset<THitboxId> parameterAsset)
    {
        m_parameterAsset = parameterAsset;
        ResetHitboxDamages();
    }

    /// <summary>
    /// 指定した攻撃のダメージを使用するHitboxへ設定します。
    /// </summary>
    /// <param name="attackIdentifier">開始する攻撃ID。</param>
    /// <returns>
    /// true：設定に成功しました。
    /// false：攻撃パラメータを取得できませんでした。
    /// </returns>
    public bool ApplyDamageParameters(
        AttackIdentifier attackIdentifier)
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
                    $"Hitbox ID {hitboxParameter.HitboxId} に対応する{nameof(AttackHitbox)}がありません。",
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
        if (m_hitboxBindings == null)
        {
            return;
        }

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

        if (m_hitboxBindings == null)
        {
            return false;
        }

        EqualityComparer<THitboxId> comparer = EqualityComparer<THitboxId>.Default;

        foreach (AttackHitboxBinding<THitboxId> binding in m_hitboxBindings)
        {
            if (binding == null ||
                binding.AttackHitbox == null ||
                !comparer.Equals(binding.HitboxId, hitboxId))
            {
                continue;
            }

            attackHitbox = binding.AttackHitbox;
            return true;
        }

        return false;
    }
}
