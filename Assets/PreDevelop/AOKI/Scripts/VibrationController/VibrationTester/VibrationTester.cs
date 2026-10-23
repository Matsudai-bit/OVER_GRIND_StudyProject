using UnityEngine;


//　テスト実行用
public class VibrationTester : MonoBehaviour
{
    [System.Serializable]
    public struct VibrationTestEntry
    {
        [Tooltip("テスト用の説明（例: 弱攻撃、爆発など）")]
        public string label;

        [Tooltip("実行するキー")]
        public KeyCode triggerKey;

        [Tooltip("再生する振動データ")]
        public VibrationData vibrationData;
    }

    [Header("テスト用振動リスト")]
    [SerializeField] private VibrationTestEntry[] testEntries;

    [Header("テスト用停止キー")]
    [SerializeField] private KeyCode stopKey = KeyCode.Space;

    private void Update()
    {
        if (VibrationManager.Instance == null) return;

        // リストに登録されたキーの入力をチェックして再生
        foreach (var entry in testEntries)
        {
            if (Input.GetKeyDown(entry.triggerKey))
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

        // 停止キーが押されたら途中で振動を強制カット
        if (Input.GetKeyDown(stopKey))
        {
            Debug.Log("[VibrationTest] 振動を強制停止");
            VibrationManager.Instance.StopVibration();
        }
    }
}