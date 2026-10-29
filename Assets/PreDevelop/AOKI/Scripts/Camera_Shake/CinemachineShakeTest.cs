using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// CinemachineShake クラスの動作テストおよびデバッグを行うクラス。
/// </summary>
public class CinemachineShakeTest : MonoBehaviour
{
    // ==========================================
    // 定数
    // ==========================================

    /// <summary>
    /// GUI 表示エリアの X 座標。
    /// </summary>
    private const float GUI_POS_X = 20f;

    /// <summary>
    /// GUI 表示エリアの Y 座標。
    /// </summary>
    private const float GUI_POS_Y = 20f;

    /// <summary>
    /// GUI 表示エリアの横幅。
    /// </summary>
    private const float GUI_WIDTH = 320f;

    /// <summary>
    /// GUI 表示エリアの縦幅。
    /// </summary>
    private const float GUI_HEIGHT = 200f;

    // ==========================================
    // メンバー変数
    // ==========================================

    /// <summary>
    /// 制御対象の CinemachineShake コンポーネント。
    /// </summary>
    [Header("制御対象")]
    [SerializeField]
    private CinemachineShake m_cinemachineShake;

    /// <summary>
    /// パターン1（加算テスト用）の発動キー。
    /// </summary>
    [Header("パターン1（加算テスト）")]
    [SerializeField]
    private Key m_key1 = Key.O;

    /// <summary>
    /// パターン1の振動継続時間（秒）。
    /// </summary>
    [SerializeField]
    private float m_duration1 = 0.3f;

    /// <summary>
    /// パターン1の振動強度（振れ幅）。
    /// </summary>
    [SerializeField]
    private float m_magnitude1 = 1.0f;

    /// <summary>
    /// パターン2（上書きテスト用）の発動キー。
    /// </summary>
    [Header("パターン2（上書きテスト）")]
    [SerializeField]
    private Key m_key2 = Key.P;

    /// <summary>
    /// パターン2の振動継続時間（秒）。
    /// </summary>
    [SerializeField]
    private float m_duration2 = 1.0f;

    /// <summary>
    /// パターン2の振動強度（振れ幅）。
    /// </summary>
    [SerializeField]
    private float m_magnitude2 = 3.0f;

    /// <summary>
    /// 振動を強制停止させるキー。
    /// </summary>
    [Header("停止キー")]
    [SerializeField]
    private Key m_stopKey = Key.M;

    // ==========================================
    // Unity イベント関数
    // ==========================================

    /// <summary>
    /// コンポーネントの初期化処理を行います。
    /// </summary>
    private void Start()
    {
        // 参照が未設定の場合は Unity 6 推奨APIで自動検出して設定
        if (m_cinemachineShake == null)
        {
            m_cinemachineShake = FindFirstObjectByType<CinemachineShake>();
        }
    }

    /// <summary>
    /// 毎フレームのキー入力を監視し、振動処理を呼び出します。
    /// </summary>
    private void Update()
    {
        // キーボード接続状態の安全確認
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // パターン1（加算）の入力判定
        if (m_key1 != Key.None && keyboard[m_key1].wasPressedThisFrame)
        {
            ExecuteShakePattern1();
        }

        // パターン2（上書き）の入力判定
        if (m_key2 != Key.None && keyboard[m_key2].wasPressedThisFrame)
        {
            ExecuteShakePattern2();
        }

        // 強制停止の入力判定
        if (m_stopKey != Key.None && keyboard[m_stopKey].wasPressedThisFrame)
        {
            ExecuteStopShake();
        }
    }

    /// <summary>
    /// テスト用のオンスクリーン GUI を描画します。
    /// </summary>
    private void OnGUI()
    {
        // 表示領域の確定
        GUILayout.BeginArea(new Rect(GUI_POS_X, GUI_POS_Y, GUI_WIDTH, GUI_HEIGHT), GUI.skin.box);
        GUILayout.Label("<b>Cinemachine Impulse Tester</b>");

        // パターン1 実行ボタン
        if (GUILayout.Button($"[{m_key1}] 軽い振動 (加算) - {m_duration1}s / 強度:{m_magnitude1}"))
        {
            ExecuteShakePattern1();
        }

        // パターン2 実行ボタン
        if (GUILayout.Button($"[{m_key2}] 重い振動 (上書き) - {m_duration2}s / 強度:{m_magnitude2}"))
        {
            ExecuteShakePattern2();
        }

        GUILayout.Space(10);

        // 強制停止ボタン
        if (GUILayout.Button($"停止 [{m_stopKey}]", GUILayout.Height(30)))
        {
            ExecuteStopShake();
        }

        GUILayout.EndArea();
    }

    // ==========================================
    // private メソッド
    // ==========================================

    /// <summary>
    /// パターン1（加算振動）を実行します。
    /// </summary>
    private void ExecuteShakePattern1()
    {
        if (m_cinemachineShake == null)
        {
            Debug.LogWarning("【CinemachineShakeTest】CinemachineShake が設定されていません。");
            return;
        }

        // 加算モード（isOverwrite: false）で発動
        m_cinemachineShake.Shake(m_duration1, m_magnitude1, false);
    }

    /// <summary>
    /// パターン2（上書き振動）を実行します。
    /// </summary>
    private void ExecuteShakePattern2()
    {
        if (m_cinemachineShake == null)
        {
            Debug.LogWarning("【CinemachineShakeTest】CinemachineShake が設定されていません。");
            return;
        }

        // 上書きモード（isOverwrite: true）で発動
        m_cinemachineShake.Shake(m_duration2, m_magnitude2, true);
    }

    /// <summary>
    /// 現在の振動を強制停止します。
    /// </summary>
    private void ExecuteStopShake()
    {
        if (m_cinemachineShake == null)
        {
            Debug.LogWarning("【CinemachineShakeTest】CinemachineShake が設定されていません。");
            return;
        }

        m_cinemachineShake.StopShake();
    }
}