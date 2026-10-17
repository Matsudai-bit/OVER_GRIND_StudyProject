using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S1P2ボスのミサイル連続発射を実行します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P2BossMissileExecutor :
    MonoBehaviour
{
    // ミサイル攻撃参照
    private S1BossMissileReferences m_missileReferences;

    // ミサイル攻撃パラメータ
    private S1P2BossMissileStateParameters m_missileParameter;

    // 攻撃対象
    private Transform m_target;

    // 次に使用する発射地点Index
    private int m_nextLaunchSiteIndex;

    // 現在の一斉発射回数
    private int m_currentVolleyIndex;

    // 前回発射からの経過時間
    private float m_launchElapsedTime;

    // 発射処理を開始したか
    private bool m_hasStartedLaunching;

    /// <summary>
    /// ミサイル攻撃を実行中か取得します。
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// ミサイル攻撃が完了したか取得します。
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// ミサイル攻撃が失敗したか取得します。
    /// </summary>
    public bool HasFailed { get; private set; }

    /// <summary>
    /// ミサイル攻撃を準備します。
    /// </summary>
    /// <param name="missileReferences">
    /// ミサイル攻撃で使用する参照。
    /// </param>
    /// <param name="parameters">
    /// ミサイル攻撃パラメータ。
    /// </param>
    /// <param name="target">
    /// ミサイルの攻撃対象。
    /// </param>
    /// <returns>
    /// true：準備できました。
    /// false：準備に失敗しました。
    /// </returns>
    public bool PrepareAttack(
        S1BossMissileReferences missileReferences,
        S1P2BossMissileStateParameters parameters,
        Transform target)
    {
        ResetRuntimeState();

        if (missileReferences == null ||
            !missileReferences.HasRequiredReferences())
        {
            Debug.LogError(
                "ミサイル攻撃参照が不足しています。",
                this);

            HasFailed = true;

            return false;
        }

        if (parameters == null ||
            !parameters.HasRequiredParameters())
        {
            Debug.LogError(
                "ミサイル攻撃パラメータが不足しています。",
                this);

            HasFailed = true;

            return false;
        }

        if (target == null)
        {
            Debug.LogError(
                "ミサイルの攻撃対象が設定されていません。",
                this);

            HasFailed = true;

            return false;
        }

        m_missileReferences =
            missileReferences;

        m_missileParameter =
            parameters;

        m_target =
            target;

        IsRunning = true;

        return true;
    }

    /// <summary>
    /// ミサイルの発射を開始します。
    /// </summary>
    /// <returns>
    /// true：開始できました。
    /// false：開始できませんでした。
    /// </returns>
    public bool BeginLaunch()
    {
        if (!IsRunning ||
            IsCompleted ||
            HasFailed)
        {
            return false;
        }

        // AnimationEventの重複などによる二重開始を防ぐ
        if (m_hasStartedLaunching)
        {
            return true;
        }

        m_hasStartedLaunching = true;

        // 状態開始と同時に1発目を発射する
        if (!TryLaunchNextMissile())
        {
            FailAttack();

            return false;
        }

        UpdateVolleyState();

        return !HasFailed;
    }

    /// <summary>
    /// ミサイル連続発射を更新します。
    /// </summary>
    /// <param name="deltaTime">
    /// 前フレームからの経過時間。
    /// </param>
    public void UpdateAttack(
        float deltaTime)
    {
        if (!IsRunning ||
            !m_hasStartedLaunching ||
            IsCompleted ||
            HasFailed ||
            m_missileParameter == null)
        {
            return;
        }

        m_launchElapsedTime +=
            deltaTime;

        if (m_launchElapsedTime <
            m_missileParameter.LaunchInterval)
        {
            return;
        }

        m_launchElapsedTime =
            0.0f;

        if (!TryLaunchNextMissile())
        {
            FailAttack();

            return;
        }

        UpdateVolleyState();
    }

    /// <summary>
    /// ミサイル攻撃を中断します。
    /// </summary>
    public void Cancel()
    {
        IsRunning = false;

        m_missileReferences = null;
        m_missileParameter = null;
        m_target = null;

        m_hasStartedLaunching = false;
    }

    /// <summary>
    /// 次の発射地点からミサイルを発射します。
    /// </summary>
    /// <returns>
    /// true：発射しました。
    /// false：発射できませんでした。
    /// </returns>
    private bool TryLaunchNextMissile()
    {
        IReadOnlyList<Transform> launchSites =
            m_missileReferences?.LaunchSites;

        if (launchSites == null)
        {
            return false;
        }

        while (m_nextLaunchSiteIndex <
               launchSites.Count)
        {
            int launchSiteIndex =
                m_nextLaunchSiteIndex;

            Transform launchSite =
                launchSites[
                    m_nextLaunchSiteIndex];

            m_nextLaunchSiteIndex++;

            if (launchSite == null)
            {
                Debug.LogWarning(
                    $"ミサイル発射地点 " +
                    $"{launchSiteIndex} がnullです。",
                    this);

                continue;
            }

            S1MissileController missile =
                Instantiate(
                    m_missileReferences.MissilePrefab,
                    launchSite.position,
                    launchSite.rotation,
                    m_missileReferences.MissileParent);

            if (missile == null)
            {
                return false;
            }

            missile.Initialize(
                m_missileParameter.MissileParameterAsset,
                m_target,
                m_missileParameter.MissileUpwardDuration);

            missile.Launch();

            return true;
        }

        return false;
    }

    /// <summary>
    /// 現在の一斉発射が終了したか確認し、
    /// 次の一斉発射または攻撃終了へ進めます。
    /// </summary>
    private void UpdateVolleyState()
    {
        if (!HasFinishedCurrentVolley())
        {
            return;
        }

        m_currentVolleyIndex++;

        if (m_currentVolleyIndex >=
            m_missileParameter.VolleyCount)
        {
            CompleteAttack();

            return;
        }

        // 次の一斉発射を先頭から開始する
        m_nextLaunchSiteIndex =
            0;

        m_launchElapsedTime =
            0.0f;
    }

    /// <summary>
    /// 現在の一斉発射が完了したか確認します。
    /// </summary>
    private bool HasFinishedCurrentVolley()
    {
        IReadOnlyList<Transform> launchSites =
            m_missileReferences?.LaunchSites;

        if (launchSites == null)
        {
            return true;
        }

        /*
         * 現在Index以降に有効な発射地点が存在する場合、
         * まだ現在のVolleyは終了していません。
         */
        for (int i = m_nextLaunchSiteIndex;
             i < launchSites.Count;
             i++)
        {
            if (launchSites[i] != null)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// ミサイル攻撃を正常終了します。
    /// </summary>
    private void CompleteAttack()
    {
        IsRunning = false;
        IsCompleted = true;
    }

    /// <summary>
    /// ミサイル攻撃を失敗終了します。
    /// </summary>
    private void FailAttack()
    {
        IsRunning = false;
        HasFailed = true;
    }

    /// <summary>
    /// 実行状態を初期化します。
    /// </summary>
    private void ResetRuntimeState()
    {
        m_missileReferences = null;
        m_missileParameter = null;
        m_target = null;

        m_nextLaunchSiteIndex = 0;
        m_currentVolleyIndex = 0;

        m_launchElapsedTime = 0.0f;

        m_hasStartedLaunching = false;

        IsRunning = false;
        IsCompleted = false;
        HasFailed = false;
    }
}