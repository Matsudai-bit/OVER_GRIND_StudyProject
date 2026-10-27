using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// CinemachineShake クラスの動作テストおよびデバッグを行うクラス。
/// </summary>
public class CinemachineShakeTest : MonoBehaviour
{
    /// <summary>
    /// 振動入力テスト用のプリセット設定データ。
    /// </summary>
    [System.Serializable]
    public class ShakeInputPreset
    {
        /// <summary>
        /// Inspector 上で識別するためのプリセット名。
        /// </summary>
        public string m_presetName = "新規パターン";

        /// <summary>
        /// 発動トリガーとなるキー。
        /// </summary>
        public Key m_triggerKey = Key.O;

        /// <summary>
        /// 振動の継続時間（秒）。
        /// </summary>
        public float m_duration = 0.3f;

        /// <summary>
        /// 振動の強さ（振れ幅）。
        /// </summary>
        public float m_magnitude = 0.5f;

        /// <summary>
        /// 既存の振動を上書きするかどうか。
        /// </summary>
        public bool m_isOverwrite = false;
    }
    /// <summary>
    /// 制御対象の CinemachineShake コンポーネント。
    /// </summary>
    [Header("制御対象")]
    [SerializeField]
    private CinemachineShake m_cinemachineShake;

    /// <summary>
    /// 振動設定パターン1（加算テスト用）。
    /// </summary>
    [Header("振動設定1（加算）")]
    [SerializeField]
    private ShakeInputPreset m_preset1 = new ShakeInputPreset
    {
        m_presetName = "パターン1（加算）",
        m_triggerKey = Key.O,
        m_duration = 0.3f,
        m_magnitude = 0.5f,
        m_isOverwrite = false
    };

    /// <summary>
    /// 振動設定パターン2（上書きテスト用）。
    /// </summary>
    [Header("振動設定2（上書き）")]
    [SerializeField]
    private ShakeInputPreset m_preset2 = new ShakeInputPreset
    {
        m_presetName = "パターン2（上書き）",
        m_triggerKey = Key.P,
        m_duration = 1.0f,
        m_magnitude = 1.0f,
        m_isOverwrite = true
    };

    /// <summary>
    /// 振動を強制停止させるキー。
    /// </summary>
    [Header("強制停止設定")]
    [SerializeField]
    private Key m_stopKey = Key.M;

    /// <summary>
    /// コンポーネントの初期化処理を行います。
    /// </summary>
    private void Start()
    {
        // 参照未設定の場合はシーン内から自動取得
        if (m_cinemachineShake == null)
        {
            m_cinemachineShake = FindFirstObjectByType<CinemachineShake>();
        }
    }

    /// <summary>
    /// 毎フレームのキー入力を監視します。
    /// </summary>
    private void Update()
    {
        // キーボード接続状態の安全確認
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // パターン1 の入力判定
        if (m_preset1.m_triggerKey != Key.None && keyboard[m_preset1.m_triggerKey].wasPressedThisFrame)
        {
            TriggerShake(m_preset1);
        }

        // パターン2 の入力判定
        if (m_preset2.m_triggerKey != Key.None && keyboard[m_preset2.m_triggerKey].wasPressedThisFrame)
        {
            TriggerShake(m_preset2);
        }

        // 強制停止キーの入力判定
        if (m_stopKey != Key.None && keyboard[m_stopKey].wasPressedThisFrame)
        {
            StopShake();
        }
    }

    /// <summary>
    /// テスト用のオンスクリーン GUI を描画します。
    /// </summary>
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20, 20, 320, 220), GUI.skin.box);
        GUILayout.Label("<b>Cinemachine Shake Tester</b>");

        DrawPresetButton(m_preset1);
        DrawPresetButton(m_preset2);

        GUILayout.Space(10);
        if (GUILayout.Button($"<b>停止 [{m_stopKey}]</b>", GUILayout.Height(30)))
        {
            StopShake();
        }

        GUILayout.EndArea();
    }
    /// <summary>
    /// プリセット設定に基づいてカメラ振動を実行します。
    /// </summary>
    /// <param name="preset">実行対象の入力プリセットデータ。</param>
    private void TriggerShake(ShakeInputPreset preset)
    {
        if (m_cinemachineShake == null || preset == null)
        {
            return;
        }

        m_cinemachineShake.Shake(preset.m_duration, preset.m_magnitude, preset.m_isOverwrite);
    }

    /// <summary>
    /// カメラ振動を停止させます。
    /// </summary>
    private void StopShake()
    {
        if (m_cinemachineShake != null)
        {
            m_cinemachineShake.StopShake();
        }
    }

    /// <summary>
    /// オンスクリーン GUI にプリセット用のテストボタンを描画します。
    /// </summary>
    /// <param name="preset">描画対象の入力プリセットデータ。</param>
    private void DrawPresetButton(ShakeInputPreset preset)
    {
        if (preset == null)
        {
            return;
        }

        string modeText = preset.m_isOverwrite ? "上書き" : "加算";
        string buttonText = $"{preset.m_presetName} [{preset.m_triggerKey}]\n秒数: {preset.m_duration}s / 振れ幅: {preset.m_magnitude} ({modeText})";

        if (GUILayout.Button(buttonText))
        {
            TriggerShake(preset);
        }
    }
}