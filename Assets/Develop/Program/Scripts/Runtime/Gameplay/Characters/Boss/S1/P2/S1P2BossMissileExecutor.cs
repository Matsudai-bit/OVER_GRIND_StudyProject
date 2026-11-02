using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S1P2ボスのミサイル連続発射を実行します。
/// ミサイル本体の生成・返却は共通のMissilePoolへ委譲します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P2BossMissileExecutor : MonoBehaviour
{
    // 3引数PrepareAttackから呼び出す際の爆発攻撃ID
    [SerializeField, Header("爆発攻撃ID（旧API用）")]
    private AttackIdentifier m_defaultAttackIdentifier;

    // ミサイル攻撃参照
    private S1BossMissileReferences m_missileReferences;

    // ミサイル攻撃パラメータ
    private S1P2BossMissileStateParameters m_missileParameter;

    // ミサイルの追尾対象
    private Transform m_target;

    // 爆発攻撃ID
    private AttackIdentifier m_attackIdentifier;

    // ミサイルの生成と寿命を管理する共通プール
    private MissilePool m_missilePool;

    // 次に使用する発射地点Index
    private int m_nextLaunchSiteIndex;

    // 完了した一斉発射の回数
    private int m_currentVolleyIndex;

    // 前回発射からの経過時間
    private float m_launchElapsedTime;

    // 発射開始済みか
    private bool m_hasStartedLaunching;

    /// <summary>
    /// ミサイルの連続発射を実行中か取得します。
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// 指定された数のミサイルをすべて発射したか取得します。
    /// 飛行中のミサイルが残っていてもtrueになります。
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// ミサイルの連続発射に失敗したか取得します。
    /// </summary>
    public bool HasFailed { get; private set; }

    /// <summary>
    /// 既存の3引数APIでミサイル攻撃を準備します。
    /// Inspector設定の爆発攻撃IDを使用します。
    /// </summary>
    /// <param name="missileReferences">発射地点などの参照。</param>
    /// <param name="parameters">連続発射パラメータ。</param>
    /// <param name="target">追尾対象。</param>
    /// <returns>
    /// true：準備できました。
    /// false：準備できませんでした。
    /// </returns>
    public bool PrepareAttack(
        S1BossMissileReferences missileReferences,
        S1P2BossMissileStateParameters parameters,
        Transform target)
    {
        return PrepareAttack(
            missileReferences,
            parameters,
            target,
            m_defaultAttackIdentifier);
    }

    /// <summary>
    /// 爆発攻撃IDを指定してミサイル攻撃を準備します。
    /// </summary>
    /// <param name="missileReferences">発射地点などの参照。</param>
    /// <param name="parameters">連続発射パラメータ。</param>
    /// <param name="target">追尾対象。</param>
    /// <param name="attackIdentifier">爆発に適用する攻撃ID。</param>
    /// <returns>
    /// true：準備できました。
    /// false：準備できませんでした。
    /// </returns>
    public bool PrepareAttack(
        S1BossMissileReferences missileReferences,
        S1P2BossMissileStateParameters parameters,
        Transform target,
        AttackIdentifier attackIdentifier)
    {
        ResetRuntimeState();

        // 旧Prefab参照ではなく、発射地点とプールを検証する
        if (!TryResolveMissilePool(missileReferences, out MissilePool missilePool))
        {
            FailAttack();
            return false;
        }

        if (parameters == null || !parameters.HasRequiredParameters() ||
            parameters.MissileParameterAsset == null ||
            parameters.VolleyCount <= 0)
        {
            Debug.LogError("ミサイル攻撃パラメータが不足しています。", this);
            FailAttack();
            return false;
        }

        if (target == null || attackIdentifier == null)
        {
            Debug.LogError("追尾対象または爆発攻撃IDが未設定です。", this);
            FailAttack();
            return false;
        }

        // 開始時に参照を確定する
        m_missileReferences = missileReferences;
        m_missileParameter = parameters;
        m_target = target;
        m_attackIdentifier = attackIdentifier;
        m_missilePool = missilePool;

        IsRunning = true;
        return true;
    }

    /// <summary>
    /// ミサイルの連続発射を開始します。
    /// </summary>
    /// <returns>
    /// true：開始できました。
    /// false：開始できませんでした。
    /// </returns>
    public bool BeginLaunch()
    {
        if (!IsRunning || IsCompleted || HasFailed)
        {
            return false;
        }

        // AnimationEventの重複による二重開始を防ぐ
        if (m_hasStartedLaunching)
        {
            return true;
        }

        m_hasStartedLaunching = true;

        // 発射開始時に1発目を発射する
        if (!TryLaunchNextMissile())
        {
            FailAttack();
            return false;
        }

        UpdateVolleyState();
        return !HasFailed;
    }

    /// <summary>
    /// 発射間隔に合わせて次のミサイルを発射します。
    /// </summary>
    /// <param name="deltaTime">経過時間。</param>
    public void UpdateAttack(float deltaTime)
    {
        if (!IsRunning || !m_hasStartedLaunching || IsCompleted || HasFailed ||
            m_missileParameter == null)
        {
            return;
        }

        m_launchElapsedTime += Mathf.Max(0.0f, deltaTime);

        float launchInterval = Mathf.Max(0.0f, m_missileParameter.LaunchInterval);
        if (m_launchElapsedTime < launchInterval)
        {
            return;
        }

        // 元の仕様を維持し、1回の更新につき最大1発に制限する
        m_launchElapsedTime = launchInterval > 0.0f
            ? Mathf.Max(0.0f, m_launchElapsedTime - launchInterval)
            : 0.0f;

        if (!TryLaunchNextMissile())
        {
            FailAttack();
            return;
        }

        UpdateVolleyState();
    }

    /// <summary>
    /// 残りの発射を中断します。
    /// 発射済みミサイルの飛行は継続します。
    /// </summary>
    public void Cancel()
    {
        IsRunning = false;

        m_missileReferences = null;
        m_missileParameter = null;
        m_target = null;
        m_attackIdentifier = null;
        m_missilePool = null;
        m_hasStartedLaunching = false;
    }

    /// <summary>
    /// 指定された発射地点にミサイルを貸し出して発射します。
    /// </summary>
    /// <returns>
    /// true：発射できました。
    /// false：発射できませんでした。
    /// </returns>
    private bool TryLaunchNextMissile()
    {
        IReadOnlyList<Transform> launchSites = m_missileReferences?.LaunchSites;

        if (launchSites == null || m_missilePool == null ||
            m_missileParameter == null)
        {
            return false;
        }

        while (m_nextLaunchSiteIndex < launchSites.Count)
        {
            int launchSiteIndex = m_nextLaunchSiteIndex;
            Transform launchSite = launchSites[m_nextLaunchSiteIndex];
            m_nextLaunchSiteIndex++;

            if (launchSite == null)
            {
                Debug.LogWarning(
                    $"ミサイル発射地点 {launchSiteIndex} がnullです。", this);
                continue;
            }

            // 共通プールから貸し出し、Prefab側の汎用インターフェースで初期化する
            if (!m_missilePool.TrySpawn(
                    launchSite.position,
                    launchSite.rotation,
                    out PooledMissile pooledMissile))
            {
                Debug.LogWarning(
                    "ミサイルプールから取得できませんでした。", this);
                return false;
            }

            IPooledMissileLauncher launcher =
                pooledMissile.GetComponent<IPooledMissileLauncher>();

            if (launcher == null ||
                !launcher.Initialize(
                    m_missileParameter.MissileParameterAsset,
                    m_target,
                    m_missileParameter.MissileUpwardDuration,
                    m_attackIdentifier,
                    m_missilePool.SceneEffectEmitter,
                    m_missilePool.ExplosionHitboxPool) ||
                !launcher.Launch())
            {
                // 初期化失敗時は必ずプールへ返却する
                m_missilePool.Release(pooledMissile);
                Debug.LogError(
                    "ミサイルの初期化または発射に失敗しました。", this);
                return false;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// 現在の一斉発射を終了し、次の一斉発射へ進みます。
    /// </summary>
    private void UpdateVolleyState()
    {
        if (!HasFinishedCurrentVolley())
        {
            return;
        }

        m_currentVolleyIndex++;
        if (m_currentVolleyIndex >= m_missileParameter.VolleyCount)
        {
            CompleteAttack();
            return;
        }

        // 次の一斉発射では最初の発射地点から再開する
        m_nextLaunchSiteIndex = 0;
        m_launchElapsedTime = 0.0f;
    }

    /// <summary>
    /// 現在の一斉発射で未使用の発射地点があるかを確認します。
    /// </summary>
    /// <returns>
    /// true：今回の一斉発射が終了しました。
    /// false：未発射の地点があります。
    /// </returns>
    private bool HasFinishedCurrentVolley()
    {
        IReadOnlyList<Transform> launchSites = m_missileReferences?.LaunchSites;
        if (launchSites == null)
        {
            return true;
        }

        for (int i = m_nextLaunchSiteIndex; i < launchSites.Count; i++)
        {
            if (launchSites[i] != null)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// ミサイルを発射し終えたことを記録します。
    /// </summary>
    private void CompleteAttack()
    {
        IsRunning = false;
        IsCompleted = true;
    }

    /// <summary>
    /// ミサイルの発射に失敗したことを記録します。
    /// </summary>
    private void FailAttack()
    {
        IsRunning = false;
        HasFailed = true;
    }

    /// <summary>
    /// 旧Prefab参照に依存せず、共通プールと発射地点を検証します。
    /// </summary>
    /// <param name="missileReferences">発射地点と親Transform。</param>
    /// <param name="missilePool">取得したプール。</param>
    /// <returns>
    /// true：必要な参照を取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryResolveMissilePool(
        S1BossMissileReferences missileReferences,
        out MissilePool missilePool)
    {
        missilePool = null;

        if (missileReferences == null ||
            missileReferences.MissileParent == null ||
            missileReferences.LaunchSites == null ||
            missileReferences.LaunchSites.Count == 0)
        {
            Debug.LogError("ミサイル攻撃参照が不足しています。", this);
            return false;
        }

        // nullでない発射地点が少なくとも1つ必要
        bool hasValidLaunchSite = false;
        foreach (Transform launchSite in missileReferences.LaunchSites)
        {
            if (launchSite != null)
            {
                hasValidLaunchSite = true;
                break;
            }
        }

        if (!hasValidLaunchSite)
        {
            Debug.LogError("有効なミサイル発射地点がありません。", this);
            return false;
        }

        missilePool = missileReferences.MissileParent.GetComponent<MissilePool>();
        if (missilePool == null ||
            missilePool.SceneEffectEmitter == null ||
            missilePool.ExplosionHitboxPool == null)
        {
            Debug.LogError(
                "MissileParentのMissilePoolまたは共有管理者が未設定です。", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 実行状態を初期化します。
    /// </summary>
    private void ResetRuntimeState()
    {
        m_missileReferences = null;
        m_missileParameter = null;
        m_target = null;
        m_attackIdentifier = null;
        m_missilePool = null;

        m_nextLaunchSiteIndex = 0;
        m_currentVolleyIndex = 0;
        m_launchElapsedTime = 0.0f;
        m_hasStartedLaunching = false;

        IsRunning = false;
        IsCompleted = false;
        HasFailed = false;
    }
}
