using UnityEngine;

/// <summary>
/// プールから貸し出されたミサイルの初期化・発射処理を定義します。
/// </summary>
public interface IPooledMissileLauncher
{
    /// <summary>
    /// 発射に必要な情報を設定します。
    /// </summary>
    /// <param name="parameterAsset">飛行パラメータ。</param>
    /// <param name="target">追尾対象。</param>
    /// <param name="upwardDuration">上昇時間。</param>
    /// <param name="attackIdentifier">爆発攻撃ID。</param>
    /// <param name="sceneEmitter">共有エフェクトの発生元。</param>
    /// <param name="hitboxPool">爆発攻撃判定プール。</param>
    /// <returns>
    /// true：初期化できました。
    /// false：初期化できませんでした。
    /// </returns>
    bool Initialize(
        MissileParameterAsset parameterAsset,
        Transform target,
        float upwardDuration,
        AttackIdentifier attackIdentifier,
        EffectEmitter sceneEmitter,
        ExplosionHitboxPool hitboxPool);

    /// <summary>
    /// ミサイルの発射を開始します。
    /// </summary>
    /// <returns>
    /// true：開始できました。
    /// false：開始できませんでした。
    /// </returns>
    bool Launch();
}
