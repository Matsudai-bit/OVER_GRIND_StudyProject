using UnityEngine;

/// <summary>
/// Player StateからActionID単位で音声を再生・停止する共通処理です。
///
/// State名とActionIDを1対1に固定せず、
/// 同じActionIDを複数Stateから操作できるようにします。
/// </summary>
public static class PlayerAudioActionUtility
{
    // PlayerAudioActionIDをそのまま再生チャンネルとして使用します。
    // 同じAudioSequentialAssetを複数ActionIDへ登録した場合でも、
    // Action単位で停止対象を識別できます。
    private static int GetChannel(
        PlayerAudioActionID actionID)
    {
        return (int)actionID;
    }

    /// <summary>
    /// ActionIDに対応するシーケンス音声を再生します。
    /// すでに同じActionIDの音声が再生中なら再生成しません。
    /// </summary>
    /// <param name="owner">Player StateMachine。</param>
    /// <param name="actionID">再生する行動ID。</param>
    /// <returns>
    /// true：再生中、または再生を開始しました。
    /// false：必要なAssetまたはControllerがありません。
    /// </returns>
    public static bool PlaySequential(
        PlayerStateMachineComponent owner,
        PlayerAudioActionID actionID)
    {
        if (owner == null ||
            owner.AudioSequentialController == null ||
            !owner.TryGetAudioSequentialAsset(
                actionID,
                out AudioSequentialAsset audioAsset))
        {
            return false;
        }

        int channel =
            GetChannel(
                actionID);

        if (owner.AudioSequentialController.IsPlaying(
                audioAsset,
                channel))
        {
            return true;
        }

        return owner.AudioSequentialController.Play(
                   audioAsset,
                   channel) != null;
    }

    /// <summary>
    /// ActionIDに対応するシーケンス音声を停止します。
    /// </summary>
    /// <param name="owner">Player StateMachine。</param>
    /// <param name="actionID">停止する行動ID。</param>
    /// <param name="immediate">
    /// true：終了音を鳴らさず即時停止します。
    /// false：AudioSequentialAssetの終了音へ遷移します。
    /// </param>
    /// <returns>
    /// true：停止対象がありました。
    /// false：停止対象がありません。
    /// </returns>
    public static bool StopSequential(
        PlayerStateMachineComponent owner,
        PlayerAudioActionID actionID,
        bool immediate = false)
    {
        if (owner == null ||
            owner.AudioSequentialController == null ||
            !owner.TryGetAudioSequentialAsset(
                actionID,
                out AudioSequentialAsset audioAsset))
        {
            return false;
        }

        int channel =
            GetChannel(
                actionID);

        return immediate
            ? owner.AudioSequentialController.StopImmediate(
                audioAsset,
                channel)
            : owner.AudioSequentialController.Stop(
                audioAsset,
                channel);
    }

    /// <summary>
    /// ActionIDに対応するOneShot音声を1回再生します。
    /// </summary>
    /// <param name="owner">Player StateMachine。</param>
    /// <param name="actionID">再生する行動ID。</param>
    /// <returns>
    /// true：再生要求を行いました。
    /// false：必要なAssetまたはControllerがありません。
    /// </returns>
    public static bool PlayOneShot(
        PlayerStateMachineComponent owner,
        PlayerAudioActionID actionID)
    {
        if (owner == null ||
            owner.AudioSequentialController == null ||
            !owner.TryGetAudioOneShotAsset(
                actionID,
                out AudioOneShotAsset audioAsset))
        {
            return false;
        }

        return owner.AudioSequentialController.PlayOneShot(
                   audioAsset) != null;
    }

    /// <summary>
    /// 攻撃種別に対応するActionIDを取得します。
    /// </summary>
    /// <param name="isBoostAttack">
    /// Vブースト残量を使用する攻撃か。
    /// </param>
    /// <returns>攻撃用ActionID。</returns>
    public static PlayerAudioActionID GetAttackActionID(
        bool isBoostAttack)
    {
        return isBoostAttack
            ? PlayerAudioActionID.ATTACKING_BOOST
            : PlayerAudioActionID.ATTACKING_NORMAL;
    }

    /// <summary>
    /// 攻撃種別に対応するActionIDを取得します。
    /// </summary>
    /// <param name="isBoostAttack">
    /// Vブースト残量を使用する攻撃か。
    /// </param>
    /// <returns>攻撃用ActionID。</returns>
    public static PlayerAudioActionID GetChargeActionID(
        bool isBoostAttack)
    {
        return isBoostAttack
            ? PlayerAudioActionID.BOOST_CHARGING_STAY
            : PlayerAudioActionID.BOOST_CHARGING_MOVE;
    }

    /// <summary>
    /// 被弾などでPlayerの継続行動を強制中断する際に、
    /// 現在使用する可能性があるシーケンス音声を即時停止します。
    /// </summary>
    /// <param name="owner">Player StateMachine。</param>
    public static void StopAllGameplaySequentialImmediate(
        PlayerStateMachineComponent owner)
    {
        StopSequential(
            owner,
            PlayerAudioActionID.WALKING,
            true);

        StopSequential(
            owner,
            PlayerAudioActionID.ATTACKING_NORMAL,
            true);

        StopSequential(
            owner,
            PlayerAudioActionID.ATTACKING_BOOST,
            true);

        StopSequential(
            owner,
            PlayerAudioActionID.BOOST_CHARGING_STAY,
            true);
        StopSequential(
        owner,
        PlayerAudioActionID.BOOST_CHARGING_MOVE,
        true);

        StopSequential(
            owner,
            PlayerAudioActionID.V_RUNNING,
            true);

        StopSequential(
            owner,
            PlayerAudioActionID.RAIL_GRINDING,
            true);

        StopSequential(
            owner,
            PlayerAudioActionID.RAIL_JUMPING,
            true);
    }
}
