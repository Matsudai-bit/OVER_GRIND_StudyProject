using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// イベント駆動型 GamepadRumbleController の動作確認用テストスクリプト。
/// シングルトン非依存の構成およびアセット単位の制御切り替えに対応しています。
/// </summary>
public class GamepadRumbleTester : MonoBehaviour
{
    /// <summary>
    /// テスト用の振動エントリー構造体。
    /// </summary>
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

    [Header("参照設定")]
    [SerializeField, Header("対象の振動コントローラー")]
    private GamepadRumbleController m_rumbleController;

    [SerializeField, Header("発行するイベントアセット")]
    private GamepadRumbleRequestEvent m_rumbleRequestEvent;

    [Header("テスト用振動リスト")]
    [SerializeField] private RumbleTestEntry[] m_testEntries;

    [Header("個別制御テスト用設定")]
    [SerializeField, Header("ループ切替 / 個別停止 / 復帰設定のテスト対象アセット")]
    private GamepadRumbleAsset m_targetControlAsset;

    [Header("テスト用操作キー設定")]
    [SerializeField, Header("全体強制停止キー")]
    private Key m_stopKey = Key.Enter;

    [SerializeField, Header("対象アセットのループON/OFF切り替えキー")]
    private Key m_toggleLoopKey = Key.L;

    [SerializeField, Header("対象アセットの個別停止キー")]
    private Key m_stopTargetKey = Key.K;

    [SerializeField, Header("対象アセットの割り込み復帰許可ON/OFF切り替えキー")]
    private Key m_toggleResumeKey = Key.S;

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // コントローラー参照が未設定の場合はシーン上から自動検索
        if (m_rumbleController == null)
        {
            m_rumbleController = FindFirstObjectByType<GamepadRumbleController>();
            if (m_rumbleController == null)
            {
                return;
            }
        }

        // 全体強制停止の処理
        if (keyboard[m_stopKey].wasPressedThisFrame)
        {
            Debug.Log("[RumbleTest] 振動を全体強制停止しました");
            m_rumbleController.StopAndResetMotors();
            return;
        }

        // 対象アセットの個別停止処理
        if (keyboard[m_stopTargetKey].wasPressedThisFrame)
        {
            if (m_targetControlAsset != null)
            {
                Debug.Log($"[RumbleTest] アセット '{m_targetControlAsset.name}' を個別停止しました");
                m_rumbleController.StopRumble(m_targetControlAsset);
            }
            else
            {
                Debug.LogWarning("[RumbleTest] 個別停止用の対象アセットが割り当てられていません。");
            }
        }

        // 対象アセットのループ ON/OFF 切り替え処理
        if (keyboard[m_toggleLoopKey].wasPressedThisFrame)
        {
            if (m_targetControlAsset != null)
            {
                bool nextLoopState = !m_targetControlAsset.IsLoop;
                m_rumbleController.SetLoop(m_targetControlAsset, nextLoopState);
                Debug.Log($"[RumbleTest] アセット '{m_targetControlAsset.name}' のループ状態を {nextLoopState} に変更しました");
            }
            else
            {
                Debug.LogWarning("[RumbleTest] ループ切り替え用の対象アセットが割り当てられていません。");
            }
        }

        // 対象アセットの割り込み復帰許可 (CanResumeAfterInterrupt) ON/OFF 切り替え処理
        if (keyboard[m_toggleResumeKey].wasPressedThisFrame)
        {
            if (m_targetControlAsset != null)
            {
                bool nextResumeState = !m_targetControlAsset.CanResumeAfterInterrupt;
                m_targetControlAsset.CanResumeAfterInterrupt = nextResumeState;
                Debug.Log($"[RumbleTest] アセット '{m_targetControlAsset.name}' の割り込み復帰許可を {nextResumeState} に切り替えました");
            }
            else
            {
                Debug.LogWarning("[RumbleTest] 復帰設定切り替え用の対象アセットが割り当てられていません。");
            }
        }

        if (m_rumbleRequestEvent == null)
        {
            return;
        }

        // 登録されている振動再生キーの判定処理
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