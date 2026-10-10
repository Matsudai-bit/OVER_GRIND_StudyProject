using System;
using System.Collections.Generic;

/// <summary>
/// ゲームプレイモードの状態遷移を管理します。
/// </summary>
public sealed class GameplayModeStateMachine
{
    // 復帰先のモード
    private readonly Stack<GameplayModeType> m_modeStack = new();

    // 現在のモード
    private GameplayModeType m_currentMode = GameplayModeType.NONE;

    // 初期化済みか
    private bool m_isInitialized = false;

    /// <summary>
    /// モード変更時に通知されます。
    /// </summary>
    public event Action<GameplayModeChangedEventData> ModeChanged;

    /// <summary>
    /// 現在のモードを取得します。
    /// </summary>
    public GameplayModeType CurrentMode => m_currentMode;

    /// <summary>
    /// 初期化済みか取得します。
    /// </summary>
    public bool IsInitialized => m_isInitialized;

    /// <summary>
    /// 前のモードへ復帰可能か取得します。
    /// </summary>
    public bool CanPopMode => m_modeStack.Count > 0;

    /// <summary>
    /// モード管理を初期化します。
    /// </summary>
    /// <param name="initialMode">初期モード。</param>
    /// <returns>
    /// true：初期化しました。
    /// false：初期化できませんでした。
    /// </returns>
    public bool Initialize(GameplayModeType initialMode)
    {
        if (m_isInitialized)
        {
            return false;
        }

        if (initialMode == GameplayModeType.NONE)
        {
            return false;
        }

        GameplayModeType previousMode = m_currentMode;

        m_modeStack.Clear();

        m_currentMode = initialMode;
        m_isInitialized = true;

        NotifyModeChanged(previousMode, m_currentMode);

        return true;
    }

    /// <summary>
    /// 現在のモードを保持して次のモードへ移行します。
    /// </summary>
    /// <param name="nextMode">移行先のモード。</param>
    /// <returns>
    /// true：モードを変更しました。
    /// false：モードを変更できませんでした。
    /// </returns>
    public bool TryPushMode(GameplayModeType nextMode)
    {
        if (!m_isInitialized)
        {
            return false;
        }

        if (nextMode == GameplayModeType.NONE)
        {
            return false;
        }

        if (m_currentMode == nextMode)
        {
            return false;
        }

        GameplayModeType previousMode = m_currentMode;

        // 現在のモードを復帰先として保持
        m_modeStack.Push(m_currentMode);

        m_currentMode = nextMode;

        NotifyModeChanged(previousMode, m_currentMode);

        return true;
    }

    /// <summary>
    /// 直前のモードへ復帰します。
    /// </summary>
    /// <returns>
    /// true：前のモードへ復帰しました。
    /// false：復帰できませんでした。
    /// </returns>
    public bool TryPopMode()
    {
        if (!m_isInitialized)
        {
            return false;
        }

        if (m_modeStack.Count <= 0)
        {
            return false;
        }

        GameplayModeType previousMode = m_currentMode;

        m_currentMode = m_modeStack.Pop();

        NotifyModeChanged(previousMode, m_currentMode);

        return true;
    }

    /// <summary>
    /// モード管理を終了します。
    /// </summary>
    /// <returns>
    /// true：終了しました。
    /// false：終了できませんでした。
    /// </returns>
    public bool Reset()
    {
        if (!m_isInitialized)
        {
            return false;
        }

        GameplayModeType previousMode = m_currentMode;

        m_modeStack.Clear();

        m_currentMode = GameplayModeType.NONE;
        m_isInitialized = false;

        NotifyModeChanged(previousMode, m_currentMode);

        return true;
    }

    /// <summary>
    /// モード変更を通知します。
    /// </summary>
    /// <param name="previousMode">変更前のモード。</param>
    /// <param name="currentMode">変更後のモード。</param>
    private void NotifyModeChanged(
        GameplayModeType previousMode,
        GameplayModeType currentMode)
    {
        GameplayModeChangedEventData eventData =
            new GameplayModeChangedEventData(
                previousMode,
                currentMode);

        ModeChanged?.Invoke(eventData);
    }
}