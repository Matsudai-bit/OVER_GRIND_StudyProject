using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ボスStateごとのクールタイムを管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class BossStateCoolTimeManager : MonoBehaviour
{
    // Stateごとの再使用可能時刻
    private readonly Dictionary<Type, float> m_readyTimeMap = new();

#if UNITY_EDITOR
    /// <summary>
    /// Inspector表示用のクールタイム情報です。
    /// </summary>
    public readonly struct CoolTimeDebugInfo
    {
        /// <summary>
        /// State名を取得します。
        /// </summary>
        public string StateName { get; }

        /// <summary>
        /// 残りクールタイムを取得します。
        /// </summary>
        public float RemainingTime { get; }

        public CoolTimeDebugInfo(
            string stateName,
            float remainingTime)
        {
            StateName = stateName;
            RemainingTime = remainingTime;
        }
    }
#endif

    /// <summary>
    /// 指定したStateのクールタイムを開始します。
    /// </summary>
    /// <typeparam name="TState">クールタイムを開始するState。</typeparam>
    /// <param name="coolTime">クールタイム秒数。</param>
    public void StartCoolTime<TState>(float coolTime)
    {
        Type stateType = typeof(TState);

        if (coolTime <= 0.0f)
        {
            m_readyTimeMap.Remove(stateType);
            return;
        }

        m_readyTimeMap[stateType] =
            Time.time + coolTime;
    }

    /// <summary>
    /// 指定したStateが使用可能か確認します。
    /// </summary>
    /// <typeparam name="TState">確認するState。</typeparam>
    /// <returns>
    /// true：使用可能です。
    /// false：クールタイム中です。
    /// </returns>
    public bool IsReady<TState>()
    {
        Type stateType = typeof(TState);

        if (!m_readyTimeMap.TryGetValue(
                stateType,
                out float readyTime))
        {
            return true;
        }

        if (Time.time < readyTime)
        {
            return false;
        }

        // 終了済みの情報を削除する
        m_readyTimeMap.Remove(stateType);

        return true;
    }

    /// <summary>
    /// 指定したStateの残りクールタイムを取得します。
    /// </summary>
    /// <typeparam name="TState">確認するState。</typeparam>
    /// <returns>残りクールタイム秒数。</returns>
    public float GetRemainingCoolTime<TState>()
    {
        Type stateType = typeof(TState);

        if (!m_readyTimeMap.TryGetValue(
                stateType,
                out float readyTime))
        {
            return 0.0f;
        }

        float remainingTime =
            readyTime - Time.time;

        if (remainingTime <= 0.0f)
        {
            m_readyTimeMap.Remove(stateType);
            return 0.0f;
        }

        return remainingTime;
    }

    /// <summary>
    /// 指定したStateのクールタイムを解除します。
    /// </summary>
    /// <typeparam name="TState">解除するState。</typeparam>
    public void ResetCoolTime<TState>()
    {
        m_readyTimeMap.Remove(
            typeof(TState));
    }

    /// <summary>
    /// すべてのクールタイムを解除します。
    /// </summary>
    public void ResetAllCoolTimes()
    {
        m_readyTimeMap.Clear();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Inspector表示用のクールタイム情報を取得します。
    /// </summary>
    /// <param name="debugInfos">取得結果。</param>
    public void GetCoolTimeDebugInfos(
        List<CoolTimeDebugInfo> debugInfos)
    {
        if (debugInfos == null)
        {
            return;
        }

        debugInfos.Clear();

        foreach (KeyValuePair<Type, float> pair in m_readyTimeMap)
        {
            float remainingTime =
                Mathf.Max(
                    0.0f,
                    pair.Value - Time.time);

            if (remainingTime <= 0.0f)
            {
                continue;
            }

            debugInfos.Add(
                new CoolTimeDebugInfo(
                    pair.Key.Name,
                    remainingTime));
        }
    }
#endif
}