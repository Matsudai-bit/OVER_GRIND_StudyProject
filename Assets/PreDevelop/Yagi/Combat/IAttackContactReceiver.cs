/// <summary>
/// 攻撃判定との接触開始・終了を受け取ります。
/// </summary>
public interface IAttackContactReceiver
{
    /// <summary>
    /// 攻撃判定との接触開始を受け取ります。
    /// </summary>
    /// <param name="attackSourceId">接触した攻撃判定のInstance ID。</param>
    void BeginAttackContact(int attackSourceId);

    /// <summary>
    /// 攻撃判定との接触終了を受け取ります。
    /// </summary>
    /// <param name="attackSourceId">離れた攻撃判定のInstance ID。</param>
    void EndAttackContact(int attackSourceId);
}
