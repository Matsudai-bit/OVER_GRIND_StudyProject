using UnityEngine;

/// <summary>
/// QTE中の継続接触に合わせてヒット音を再生します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1QTECollisionHitAudioPlayer :
    MonoBehaviour
{
    [SerializeField, Header("参照")]
    private S1QTEController m_qteController;

    [SerializeField]
    private CollisionSensor m_collisionSensor;

    // 再生するヒット音
    [SerializeField, Header("音声")]
    private AudioID m_hitAudioId;

    // ヒット音の再生間隔
    [SerializeField, Min(0.01f)]
    private float m_hitInterval = 0.3f;

    // 接触開始直後にもヒット音を鳴らすか
    [SerializeField]
    private bool m_playImmediatelyOnContact = true;

    // 長押し中のみ音を鳴らすか
    [SerializeField, Header("再生条件")]
    private bool m_onlyWhileHolding = true;

    // 前フレームまで再生条件を満たしていたか
    private bool m_wasPlaybackConditionMet;

    // 前回のヒット音からの経過時間
    private float m_elapsedHitTime;

    private void OnDisable()
    {
        ResetHitTimer();
    }

    private void Update()
    {
        bool canPlay =
            CanPlayHitSound();

        if (!canPlay)
        {
            ResetHitTimer();
            return;
        }

        /*
         * 接触開始直後の処理です。
         */
        if (!m_wasPlaybackConditionMet)
        {
            m_wasPlaybackConditionMet = true;
            m_elapsedHitTime = 0.0f;

            if (m_playImmediatelyOnContact)
            {
                PlayHitSound();
            }

            return;
        }

        m_elapsedHitTime +=
            Time.unscaledDeltaTime;

        if (m_elapsedHitTime <
            m_hitInterval)
        {
            return;
        }

        /*
         * フレーム誤差を蓄積しすぎないよう、
         * interval分だけ減算します。
         */
        m_elapsedHitTime -=
            m_hitInterval;

        PlayHitSound();
    }
    /// <summary>
    /// ヒット音を再生可能か確認します。
    /// </summary>
    private bool CanPlayHitSound()
    {
        if (m_qteController == null ||
            m_collisionSensor == null)
        {
            return false;
        }

        // QTE中でなければ再生しない
        if (!m_qteController.IsQteActive)
        {
            return false;
        }

        // 長押しループ開始地点より前では再生しない
        if (!m_qteController.HasReachedHoldLoopStart)
        {
            return false;
        }

        // 対象に接触していなければ再生しない
        if (!m_collisionSensor.IsDetectingTarget)
        {
            return false;
        }

        // 長押し中でなければ再生しない
        if (!m_qteController.IsHolding)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 現在の接触位置からヒット音を再生します。
    /// </summary>
    private void PlayHitSound()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.Play2D(
            m_hitAudioId);
    }

    /// <summary>
    /// ヒット音の時間計測をリセットします。
    /// </summary>
    private void ResetHitTimer()
    {
        m_wasPlaybackConditionMet = false;
        m_elapsedHitTime = 0.0f;
    }
}