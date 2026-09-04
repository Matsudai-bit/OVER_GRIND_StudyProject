using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ボスが使用する攻撃Hitboxを一元管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class AttackHitboxRegistry : MonoBehaviour
{
    // Inspectorで設定する攻撃Hitbox情報
    [SerializeField, Header("攻撃Hitbox")]
    private List<AttackHitboxGroup> m_hitboxGroups = new();


    // 実行時に使用する検索テーブル
    private readonly Dictionary<AttackIdentifier, AttackHitboxGroup>
        m_hitboxGroupMap = new();

    /// <summary>
    /// Hitbox検索テーブルを構築します。
    /// </summary>
    private void Awake()
    {
        m_hitboxGroupMap.Clear();

        foreach (AttackHitboxGroup hitboxGroup in m_hitboxGroups)
        {
            if (hitboxGroup == null ||
                hitboxGroup.AttackIdentifier == null)
            {
                continue;
            }

            if (!m_hitboxGroupMap.TryAdd(
                    hitboxGroup.AttackIdentifier,
                    hitboxGroup))
            {
                Debug.LogWarning(
                    $"{hitboxGroup.AttackIdentifier.name}が重複しています。",
                    this);
            }
        }
    }

    /// <summary>
    /// 指定攻撃のHitboxをすべて有効にします。
    /// </summary>
    /// <param name="attackIdentifier">攻撃ID。</param>
    public void EnableHitbox(AttackIdentifier attackIdentifier, AttackDamageControllerBase attackDamage)

    {
        if (!TryGetHitboxGroup(
                attackIdentifier,
                out AttackHitboxGroup hitboxGroup))
        {
            return;
        }

       
        attackDamage.ApplyDamageParameters(attackIdentifier);

        hitbox.EnableHitbox();

    }

    /// <summary>
    /// 指定攻撃のHitboxをすべて無効にします。
    /// </summary>
    /// <param name="attackIdentifier">攻撃ID。</param>
    public void DisableHitboxes(AttackIdentifier attackIdentifier)
    {
        if (!TryGetHitboxGroup(
                attackIdentifier,
                out AttackHitboxGroup hitboxGroup))
        {
            return;
        }

        hitboxGroup.DisableHitboxes();
    }

    /// <summary>
    /// 指定攻撃のHitbox情報を取得します。
    /// </summary>
    /// <param name="attackIdentifier">攻撃ID。</param>
    /// <param name="hitboxGroup">取得したHitbox情報。</param>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    public bool TryGetHitboxGroup(
        AttackIdentifier attackIdentifier,
        out AttackHitboxGroup hitboxGroup)
    {
        hitboxGroup = null;

        if (attackIdentifier == null)
        {
            return false;
        }

        if (m_hitboxGroupMap.TryGetValue(
                attackIdentifier,
                out hitboxGroup))
        {
            return true;
        }

        Debug.LogWarning(
            $"{attackIdentifier.name}に対応するHitbox情報がありません。",
            this);

        return false;
    }
}