using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// New Input System対応の振動テスト実行スクリプト
/// </summary>
public class VibrationTester : MonoBehaviour
{
    [System.Serializable]
    public struct VibrationTestEntry
    {
        [Tooltip("テスト用の説明（例: 弱攻撃、爆発など）")]
        public string label;

        [Tooltip("実行するキー（例: Digit1, Digit2, Rなど）")]
        public Key triggerKey;

        [Tooltip("再生する振動データ")]
        public VibrationData vibrationData;
    }

    [Header("テスト用振動リスト")]
    [SerializeField] private VibrationTestEntry[] testEntries;

    [Header("テスト用停止キー")]
    [SerializeField] private Key stopKey = Key.Space;

    private void Update()
    {
        if (VibrationManager.Instance == null) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 停止キーの判定（再生処理より先に判定し、同一キー指定時の即死競合をガード）
        if (keyboard[stopKey].wasPressedThisFrame)
        {
            Debug.Log("[VibrationTest] 振動を強制停止");
            VibrationManager.Instance.StopAllVibrations();
            return;
        }

        // リストに登録されたキーの判定と再生
        foreach (var entry in testEntries)
        {
            if (keyboard[entry.triggerKey].wasPressedThisFrame)
            {
                if (entry.vibrationData != null)
                {
                    Debug.Log($"[VibrationTest] 再生: {entry.label} (Key: {entry.triggerKey})");
                    VibrationManager.Instance.Play(entry.vibrationData);
                }
                else
                {
                    Debug.LogWarning($"[VibrationTest] {entry.label} に VibrationData が割り当てられていません。");
                }
            }
        }
    }
}