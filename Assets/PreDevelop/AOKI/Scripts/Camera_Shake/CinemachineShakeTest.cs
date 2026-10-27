using UnityEngine;
using UnityEngine.InputSystem;

public class CinemachineShakeTest : MonoBehaviour
{
    [SerializeField] private CinemachineShake cinemachineShake;

    [Header("パターン1（加算テスト）")]
    [SerializeField] private Key key1 = Key.O;
    [SerializeField] private float duration1 = 0.3f;
    [SerializeField] private float magnitude1 = 1.0f;

    [Header("パターン2（上書きテスト）")]
    [SerializeField] private Key key2 = Key.P;
    [SerializeField] private float duration2 = 1.0f;
    [SerializeField] private float magnitude2 = 3.0f;

    [Header("停止キー")]
    [SerializeField] private Key stopKey = Key.M;

    private void Awake()
    {
        if (cinemachineShake == null)
        {
            cinemachineShake = FindObjectOfType<CinemachineShake>();
        }
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 加算（overwrite: false）
        if (keyboard[key1].wasPressedThisFrame)
        {
            cinemachineShake.Shake(duration1, magnitude1, overwrite: false);
        }

        // 上書き（overwrite: true）
        if (keyboard[key2].wasPressedThisFrame)
        {
            cinemachineShake.Shake(duration2, magnitude2, overwrite: true);
        }

        // 強制停止
        if (keyboard[stopKey].wasPressedThisFrame)
        {
            cinemachineShake.StopShake();
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20, 20, 320, 200), GUI.skin.box);
        GUILayout.Label("<b>Cinemachine Impulse Tester</b>");

        if (GUILayout.Button($"[{key1}] 軽い振動 (加算) - {duration1}s / 強度:{magnitude1}"))
        {
            cinemachineShake.Shake(duration1, magnitude1, overwrite: false);
        }

        if (GUILayout.Button($"[{key2}] 重い振動 (上書き) - {duration2}s / 強度:{magnitude2}"))
        {
            cinemachineShake.Shake(duration2, magnitude2, overwrite: true);
        }

        GUILayout.Space(10);
        if (GUILayout.Button($"停止 [{stopKey}]", GUILayout.Height(30)))
        {
            cinemachineShake.StopShake();
        }

        GUILayout.EndArea();
    }
}