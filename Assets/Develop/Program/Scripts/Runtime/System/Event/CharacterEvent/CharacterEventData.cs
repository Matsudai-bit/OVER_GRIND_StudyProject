using UnityEngine;

/// <summary>
/// 攻撃演出のタイミング種別です。
/// </summary>
public enum CharacterAttackCueType
{
    CHARGE,  // 溜め
    RELEASE, // 解放 
    IMPACT   // 衝撃
}

/// <summary>
/// 攻撃開始・終了イベントの情報です。
/// </summary>
public readonly struct CharacterAttackEventData
{
    /// <summary>
    /// イベントを発生させたキャラクターを取得します。
    /// </summary>
    public GameObject Source { get; }

    /// <summary>
    /// 攻撃IDを取得します。
    /// </summary>
    public AttackIdentifier AttackId { get; }

    public CharacterAttackEventData(
        GameObject source,
        AttackIdentifier attackId)
    {
        Source = source;
        AttackId = attackId;
    }
}

/// <summary>
/// 攻撃演出イベントの情報です。
/// </summary>
public readonly struct CharacterAttackCueEventData
{
    /// <summary>
    /// イベントを発生させたキャラクターを取得します。
    /// </summary>
    public GameObject Source { get; }

    /// <summary>
    /// 攻撃IDを取得します。
    /// </summary>
    public AttackIdentifier AttackId { get; }

    /// <summary>
    /// 演出タイミング種別を取得します。
    /// </summary>
    public CharacterAttackCueType CueType { get; }

    /// <summary>
    /// イベント発生位置を取得します。
    /// </summary>
    public Vector3 Position { get; }

    public CharacterAttackCueEventData(
        GameObject source,
        AttackIdentifier attackId,
        CharacterAttackCueType cueType,
        Vector3 position)
    {
        Source = source;
        AttackId = attackId;
        CueType = cueType;
        Position = position;
    }
}

/// <summary>
/// 攻撃ヒットイベントの情報です。
/// </summary>
public readonly struct CharacterAttackHitEventData
{
    /// <summary>
    /// 攻撃したキャラクターを取得します。
    /// </summary>
    public GameObject Source { get; }

    /// <summary>
    /// 攻撃IDを取得します。
    /// </summary>
    public AttackIdentifier AttackId { get; }

    /// <summary>
    /// 攻撃対象を取得します。
    /// </summary>
    public GameObject Target { get; }

    /// <summary>
    /// ヒット位置を取得します。
    /// </summary>
    public Vector3 HitPosition { get; }
    
    public CharacterAttackHitEventData(
        GameObject source,
        AttackIdentifier attackId,
        GameObject target,
        Vector3 hitPosition)
    {
        Source = source;
        AttackId = attackId;
        Target = target;
        HitPosition = hitPosition;
    }
}

/// <summary>
/// 移動イベントの情報です。
/// </summary>
public readonly struct CharacterMoveEventData
{
    /// <summary>
    /// イベントを発生させたキャラクターを取得します。
    /// </summary>
    public GameObject Source { get; }

    /// <summary>
    /// イベント発生位置を取得します。
    /// </summary>
    public Vector3 Position { get; }

    /// <summary>
    /// 移動方向を取得します。
    /// </summary>
    public Vector3 Direction { get; }

    /// <summary>
    /// 移動速度を取得します。
    /// </summary>
    public float Speed { get; }

    public CharacterMoveEventData(
        GameObject source,
        Vector3 position,
        Vector3 direction,
        float speed)
    {
        Source = source;
        Position = position;
        Direction = direction;
        Speed = speed;
    }
}

/// <summary>
/// ダメージイベントの情報です。
/// </summary>
public readonly struct CharacterDamageEventData
{
    /// <summary>
    /// ダメージを受けたキャラクターを取得します。
    /// </summary>
    public GameObject Source { get; }

    /// <summary>
    /// 攻撃元を取得します。
    /// </summary>
    public GameObject Attacker { get; }

    /// <summary>
    /// ダメージ量を取得します。
    /// </summary>
    public float Damage { get; }

    /// <summary>
    /// ヒット位置を取得します。
    /// </summary>
    public Vector3 HitPosition { get; }

    public CharacterDamageEventData(
        GameObject source,
        GameObject attacker,
        float damage,
        Vector3 hitPosition)
    {
        Source = source;
        Attacker = attacker;
        Damage = damage;
        HitPosition = hitPosition;
    }
}

/// <summary>
/// 死亡イベントの情報です。
/// </summary>
public readonly struct CharacterDeathEventData
{
    /// <summary>
    /// 死亡したキャラクターを取得します。
    /// </summary>
    public GameObject Source { get; }

    /// <summary>
    /// 死亡位置を取得します。
    /// </summary>
    public Vector3 Position { get; }

    public CharacterDeathEventData(
        GameObject source,
        Vector3 position)
    {
        Source = source;
        Position = position;
    }
}