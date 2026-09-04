using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 現在フェーズで使用する攻撃Hitboxを管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class AttackHitboxRegistry : MonoBehaviour
{
    // 現在フェーズで使用する攻撃Hitbox情報
    private readonly List<AttackHitboxGroup> m_hitboxGroups = new();

    // 実行時に使用する検索テーブル
    private readonly Dictionary<AttackIdentifier, AttackHitboxGroup>
        m_hitboxGroupMap = new();

    /// <summary>
    /// 現在フェーズで使用するHitbox情報を設定します。
    /// </summary>
    /// <param name="hitboxGroups">設定するHitbox情報。</param>
    /// <returns>
    /// true：設定しました。
    /// false：設定内容に不備があります。
    /// </returns>
    public bool SetHitboxGroups(
        IReadOnlyList<AttackHitboxGroup> hitboxGroups)
    {
        if (hitboxGroups == null)
        {
            Debug.LogError(
                "攻撃Hitbox情報が設定されていません。",
                this);

            return false;
        }

        // 旧フェーズのHitboxを停止してから設定を破棄します。
        ClearHitboxGroups();

        bool isValid = true;

        foreach (AttackHitboxGroup hitboxGroup in hitboxGroups)
        {
            if (hitboxGroup == null)
            {
                Debug.LogWarning(
                    "攻撃Hitbox情報に未設定の要素があります。",
                    this);

                isValid = false;
                continue;
            }

            AttackIdentifier attackIdentifier =
                hitboxGroup.AttackIdentifier;

            if (attackIdentifier == null)
            {
                Debug.LogWarning(
                    "Attack IDが設定されていない攻撃Hitbox情報があります。",
                    this);

                isValid = false;
                continue;
            }

            if (!m_hitboxGroupMap.TryAdd(
                    attackIdentifier,
                    hitboxGroup))
            {
                Debug.LogWarning(
                    $"{attackIdentifier.name}が重複しています。",
                    this);

                isValid = false;
                continue;
            }

            m_hitboxGroups.Add(
                hitboxGroup);
        }

        return isValid;
    }

    /// <summary>
    /// 現在フェーズのHitbox情報を解除します。
    /// </summary>
    public void ClearHitboxGroups()
    {
        DisableAllHitboxes();

        m_hitboxGroups.Clear();
        m_hitboxGroupMap.Clear();
    }

    /// <summary>
    /// 指定攻撃のHitboxをすべて有効にします。
    /// </summary>
    /// <param name="attackIdentifier">攻撃ID。</param>
    /// <returns>
    /// true：Hitboxを有効にしました。
    /// false：対応するHitbox情報がありません。
    /// </returns>
    public bool EnableHitboxes(
        AttackIdentifier attackIdentifier)
    {
        if (!TryGetHitboxGroup(
                attackIdentifier,
                out AttackHitboxGroup hitboxGroup))
        {
            return false;
        }

        hitboxGroup.EnableHitboxes();

        return true;
    }

    /// <summary>
    /// 指定攻撃のHitboxをすべて無効にします。
    /// </summary>
    /// <param name="attackIdentifier">攻撃ID。</param>
    /// <returns>
    /// true：Hitboxを無効にしました。
    /// false：対応するHitbox情報がありません。
    /// </returns>
    public bool DisableHitboxes(
        AttackIdentifier attackIdentifier)
    {
        if (!TryGetHitboxGroup(
                attackIdentifier,
                out AttackHitboxGroup hitboxGroup))
        {
            return false;
        }

        hitboxGroup.DisableHitboxes();

        return true;
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

    /// <summary>
    /// 現在登録されているHitboxをすべて無効にします。
    /// </summary>
    private void DisableAllHitboxes()
    {
        foreach (AttackHitboxGroup hitboxGroup in m_hitboxGroups)
        {
            if (hitboxGroup == null)
            {
                continue;
            }

            hitboxGroup.DisableHitboxes();
        }
    }

    /// <summary>
    /// 破棄時にHitboxを無効化します。
    /// </summary>
    private void OnDestroy()
    {
        DisableAllHitboxes();
    }
}
