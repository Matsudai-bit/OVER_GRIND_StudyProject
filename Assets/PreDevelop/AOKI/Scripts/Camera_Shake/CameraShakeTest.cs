using UnityEngine;

public class CameraShakeTest : MonoBehaviour
{
    [Header("対象のカメラシェイク")]
    [SerializeField] private CameraShake cameraShake;

    [Header("テスト設定1: 連続攻撃（加算テスト）")]
    [SerializeField] private float lightDuration = 0.2f;
    [SerializeField] private float lightMagnitude = 0.1f;

    [Header("テスト設定2: 一撃大ダメージ（上書きテスト）")]
    [SerializeField] private float heavyDuration = 1.2f;
    [SerializeField] private float heavyMagnitude = 0.6f;

    private void Start()
    {
        // アタッチを忘れている場合に自動取得
        if (cameraShake == null)
        {
            cameraShake = FindObjectOfType<CameraShake>();
        }
    }

    private void Update()
    {
        // [1]キー または [Space]キー: 加算テスト（連射すると揺れと時間が積み重なる）
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Space))
        {
            TriggerLightShake();
        }

        // [2]キー または [Enter]キー: 上書きテスト（実行中の揺れを中断してタイマーリセット）
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Return))
        {
            TriggerHeavyShake();
        }
    }

    // 画面上にテスト用ボタンを表示するGUI（Inspector操作不要でテスト可能）
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20, 20, 280, 160), GUI.skin.box);
        GUILayout.Label("<b>Camera Shake Test Panel</b>");

        if (GUILayout.Button($"[1 / Space] 軽い振動（加算）\n({lightDuration}s / 強度:{lightMagnitude})"))
        {
            TriggerLightShake();
        }

        if (GUILayout.Button($"[2 / Enter] 重い振動（上書き）\n({heavyDuration}s / 強度:{heavyMagnitude})"))
        {
            TriggerHeavyShake();
        }

        GUILayout.EndArea();
    }

    private void TriggerLightShake()
    {
        if (cameraShake == null) return;
        // overwrite: false で加算処理
        cameraShake.Shake(lightDuration, lightMagnitude, overwrite: false);
    }

    private void TriggerHeavyShake()
    {
        if (cameraShake == null) return;
        // overwrite: true で上書き処理
        cameraShake.Shake(heavyDuration, heavyMagnitude, overwrite: true);
    }
}