using UnityEngine;

/// <summary>
/// 攻撃ダメージ管理の共通インターフェースを提供します。
/// </summary>
public abstract class AttackDamageControllerBase : MonoBehaviour
{
    /// <summary>
    /// 指定した攻撃のダメージパラメータを適用します。
    /// </summary>
    /// <param name="attackIdentifier">適用する攻撃ID。</param>
    /// <returns>
    /// true：適用しました。
    /// false：適用できませんでした。
    /// </returns>
    public abstract bool ApplyDamageParameters(
        AttackIdentifier attackIdentifier);
}