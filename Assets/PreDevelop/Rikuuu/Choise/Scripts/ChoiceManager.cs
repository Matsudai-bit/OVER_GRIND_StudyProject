using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class ChoiceManager : MonoBehaviour
{
    [Header("コンポーネント類")]
    // タイトル（英語）
    [SerializeField]
    private TextMeshProUGUI m_titleEnglish;
    // タイトル（日本語）
    [SerializeField]
    private TextMeshProUGUI m_titleJapanese;
    // 詳細説明テキスト
    [SerializeField]
    private TextMeshProUGUI m_detail;
    [Space]

    // ボタンセレクター
    [SerializeField]
    private ButtonSelector m_buttonSelector;
    // YESボタン
    [SerializeField]
    private SelectButton m_buttonY;
    [Space]

    [Header("アニメーション")]
    // フェードインアニメーション
    [SerializeField]
    private PlayableDirector m_fadeInDirector;
    // フェードアウトアニメーション
    [SerializeField]
    private PlayableDirector m_fadeOutDirector;

    [Header("関数系統")]
    // 展開時に呼ぶ関数
    [SerializeField]
    private UnityEngine.Events.UnityEvent m_OpenEvent;
    // 収縮時に呼ぶ関数
    [SerializeField]
    private UnityEngine.Events.UnityEvent m_CloseEvent;

    [Header("入力判定関連")]
    // キャンセルキーが押される判定
    [SerializeField]
    private InputActionReference m_cancelActionRef;

    // 使用しているデータ
    ChoiceData m_choiceData;
    // アニメーション再生中か
    private bool m_isAnimating;
    // 開いているか
    private bool m_isOpen;

    private void Start()
    {
        m_cancelActionRef?.action.Enable();
    }

    private void Update()
    {
        // キャンセルボタンが押されたら
        if(m_cancelActionRef != null && m_cancelActionRef.action.WasPressedThisFrame())
        {
            // ウィンドウを閉じる
            CloseChoiceMenu();
        }
    }

    public void OpenChoiceMenu(ChoiceData data)
    {
        // アニメーション再生中、または既に開いているなら何もしない
        if (m_isAnimating || m_isOpen)
        {
            return;
        }
        m_isOpen = true;

        // 展開時に呼ぶ関数を実行する
        m_OpenEvent?.Invoke();
        // データの上書き
        m_choiceData = data;

        // タイトル（英語）
        m_titleEnglish.text = m_choiceData.TitleEnglish;
        // タイトル（日本語）
        m_titleJapanese.text = m_choiceData.TitleJapanese;
        // 詳細設定
        m_detail.text = m_choiceData.Detail;

        // フェードインアニメーションを再生
        // 再生が終わってからボタンセレクターのロックを解除する
        PlayDirector(m_fadeInDirector, () =>
        {
            // ボタンセレクターのロックを解除する
            m_buttonSelector.UnLockCursor();
        });
    }

    public void CloseChoiceMenu()
    {
        // アニメーション再生中、または開いていないなら何もしない
        if (m_isAnimating || !m_isOpen)
        {
            return;
        }

        // フェードアウト中に操作されないよう、先にボタンセレクターをロックする
        m_buttonSelector.LockCursor();

        // フェードアウトアニメーションを再生
        // 再生が終わってから後片付けを行う
        PlayDirector(m_fadeOutDirector, () =>
        {
            // データのリセット
            m_choiceData = null;
            // 縮小時に呼び出す処理を実行
            m_CloseEvent?.Invoke();
            // ボタンセレクターのリセット
            m_buttonSelector.ResetCursor();
            m_isOpen = false;
        });
    }

    // アニメーションを再生し、終了したらonFinishedを実行する
    private void PlayDirector(PlayableDirector director, Action onFinished)
    {
        // アニメーションが設定されていなければ、すぐに終了扱いにする
        if (director == null)
        {
            onFinished?.Invoke();
            return;
        }

        m_isAnimating = true;

        // 終了時に自分自身を解除するため、先に変数を宣言しておく
        Action<PlayableDirector> handler = null;
        handler = (PlayableDirector finished) =>
        {
            finished.stopped -= handler;
            m_isAnimating = false;
            onFinished?.Invoke();
        };

        director.stopped += handler;
        director.Play();
    }
}