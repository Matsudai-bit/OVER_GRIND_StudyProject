using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// イベント駆動型 GamepadRumbleController の動作確認用テストスクリプト
/// </summary>
public class GamepadRumbleTester : MonoBehaviour
{
    [System.Serializable]
    public struct RumbleTestEntry
    {
        [Tooltip("テスト用の説明（例: 弱攻撃、爆発など）")]
        public string label;

        [Tooltip("実行するキー")]
        public Key triggerKey;

        [Tooltip("再生する振動アセット")]
        public GamepadRumbleAsset rumbleAsset;
    }

    [Header("発行するイベントアセット")]
    [SerializeField] private GamepadRumbleRequestEvent m_rumbleRequestEvent;

    [Header("テスト用振動リスト")]
    [SerializeField] private RumbleTestEntry[] m_testEntries;

    [Header("テスト用停止キー")]
    [SerializeField] private Key m_stopKey = Key.Enter;

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 強制停止の処理
        if (keyboard[m_stopKey].wasPressedThisFrame)
        {
            Debug.Log("[RumbleTest] 振動を強制停止しました");
            if (GamepadRumbleController.Instance != null)
            {
                GamepadRumbleController.Instance.StopAndResetMotors();
            }
            return;
        }

        if (m_rumbleRequestEvent == null) return;

        // 振動再生の処理
        foreach (var entry in m_testEntries)
        {
            if (keyboard[entry.triggerKey].wasPressedThisFrame)
            {
                if (entry.rumbleAsset != null)
                {
                    Debug.Log($"[RumbleTest] イベント発行: {entry.label} (Key: {entry.triggerKey})");
                    m_rumbleRequestEvent.Raise(entry.rumbleAsset);
                }
                else
                {
                    Debug.LogWarning($"[RumbleTest] {entry.label} に GamepadRumbleAsset が割り当てられていません。");
                }
            }
        }
    }
}