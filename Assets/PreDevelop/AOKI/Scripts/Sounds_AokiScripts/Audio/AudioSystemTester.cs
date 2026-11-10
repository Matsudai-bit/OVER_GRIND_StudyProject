using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 音声管理システムの動作テストを行うクラスです。
/// </summary>
public class AudioSystemTester : MonoBehaviour
{
    private const float GUI_AREA_X = 20.0f;
    private const float GUI_AREA_Y = 20.0f;
    private const float GUI_AREA_WIDTH = 280.0f;
    private const float GUI_AREA_HEIGHT = 220.0f;

    /// <summary>
    /// 3D 追従再生のテスト用オブジェクトです。
    /// </summary>
    [Header("テスト対象のオブジェクト")]
    [Tooltip("3D 追従再生のテスト用に動かすオブジェクト（Cube など）")]
    [SerializeField] private Transform m_followTarget;

    /// <summary>
    /// 2D 再生テストに使用するサウンド ID です。
    /// </summary>
    [Header("テストに使用する SoundID")]
    [SerializeField] private AudioID m_se2dId;

    /// <summary>
    /// 3D 固定位置再生テストに使用するサウンド ID です。
    /// </summary>
    [SerializeField] private AudioID m_se3dPosId;

    /// <summary>
    /// 3D 追従再生テストに使用するサウンド ID です。
    /// </summary>
    [SerializeField] private AudioID m_se3dFollowId;

    /// <summary>
    /// 再生中のループ音声用ハンドルです。
    /// </summary>
    private AudioPlayHandle m_loopHandle;

    private void Update()
    {
        // キーボード入力をチェックしてテスト再生を実行します
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            Play2dSound();
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            Play3dPosSound();
        }

        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            Play3dFollowSound();
        }

        if (keyboard.digit4Key.wasPressedThisFrame)
        {
            StopFollowSound();
        }
    }

    private void OnGUI()
    {
        // 画面上に簡易テスト用 UI を表示します
        GUILayout.BeginArea(new Rect(GUI_AREA_X, GUI_AREA_Y, GUI_AREA_WIDTH, GUI_AREA_HEIGHT), GUI.skin.box);
        GUILayout.Label("<b>Audio System Tester</b>");

        if (GUILayout.Button("2D 音声を再生"))
        {
            Play2dSound();
        }

        if (GUILayout.Button("3D 固定位置音声を再生"))
        {
            Play3dPosSound();
        }

        if (GUILayout.Button("3D 追従音声を再生開始"))
        {
            Play3dFollowSound();
        }

        if (GUILayout.Button("追従音声を停止"))
        {
            StopFollowSound();
        }

        GUILayout.EndArea();
    }

    /// <summary>
    /// 2D サウンドを再生します。
    /// </summary>
    public void Play2dSound()
    {
        Debug.Log("[Test] 2D サウンド再生");
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Play2D(m_se2dId);
        }
    }

    /// <summary>
    /// 自身のある位置で 3D 固定位置サウンドを再生します。
    /// </summary>
    public void Play3dPosSound()
    {
        Debug.Log($"[Test] 3D 固定位置サウンド再生: {transform.position}");
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Play3DAtPosition(m_se3dPosId, transform.position);
        }
    }

    /// <summary>
    /// 指定されたターゲットを追従する 3D サウンドを再生します。
    /// </summary>
    public void Play3dFollowSound()
    {
        if (m_followTarget == null)
        {
            Debug.LogWarning("[Test] m_followTarget が設定されていません。インスペクターでアタッチしてください。");
            return;
        }

        Debug.Log($"[Test] 3D 追従サウンド再生開始 -> 追従対象: {m_followTarget.name}");
        if (AudioManager.Instance != null)
        {
            m_loopHandle = AudioManager.Instance.Play3DFollowing(m_se3dFollowId, m_followTarget);
        }
    }

    /// <summary>
    /// 再生中の追従サウンドを停止します。
    /// </summary>
    public void StopFollowSound()
    {
        if (m_loopHandle != null)
        {
            Debug.Log("[Test] 追従サウンド停止");
            m_loopHandle.Stop();
            m_loopHandle = null;
        }
    }
}