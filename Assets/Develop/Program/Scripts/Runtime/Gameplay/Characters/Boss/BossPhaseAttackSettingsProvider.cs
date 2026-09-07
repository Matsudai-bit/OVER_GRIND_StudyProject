using UnityEngine;

/// <summary>
/// フェーズごとの攻撃ダメージ設定を適用します。
/// </summary>
public abstract class BossPhaseAttackSettingsProvider : MonoBehaviour
{
    /// <summary>
    /// このフェーズの攻撃ダメージ設定を適用します。
    /// </summary>
    /// <returns>
    /// true：設定に成功しました。
    /// false：設定に失敗しました。
    /// </returns>
    public abstract bool ApplyDamageSettings();

    /// <summary>
    /// このフェーズで使用していた攻撃ダメージ設定を解除します。
    /// </summary>
    public abstract void ClearDamageSettings();
}
