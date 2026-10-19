using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class InputManager : MonoBehaviour
{
    private bool m_isPauseActive = true;
    private int m_isOnWindow = 0;
    private float m_timer = 0.0f;
    private float m_intavalTime = 1.0f;

    [SerializeField] private PlayableDirector m_directorFadeIn;
    [SerializeField] private PlayableDirector m_directorFadeOut;
    [SerializeField] private GameObject m_detailText;
    [SerializeField] private GameObject m_container;
    [SerializeField] private GameObject m_background;
    [SerializeField] private GameObject m_selectButton;
    [SerializeField] private GameObject m_mechPanel;

    [Header("ButtonSelector")]
    [SerializeField] private ButtonSelector m_buttonSelector;

    [Header("Cursor Control")]
    [SerializeField] private GameObject m_cursor;
    [SerializeField] private Vector2 m_cursorInitialPosition; // インスペクターで指定するカーソルの初期設定位置

    private bool m_isFadeInPlaying = false;
    private bool m_isFadeOutPlaying = false;

    void Start()
    {
        m_directorFadeIn.playOnAwake = false;
        m_directorFadeOut.playOnAwake = false;
        m_directorFadeIn.time = 0;
        m_directorFadeOut.time = 0;
        m_directorFadeIn.Stop();
        m_directorFadeOut.Stop();

        // 通常の自動時間更新に戻す（これで全体のアニメーションが飛ぶ現象が治ります）
        m_directorFadeIn.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
        m_directorFadeOut.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;

        m_directorFadeIn.extrapolationMode = DirectorWrapMode.Hold;
        m_directorFadeOut.extrapolationMode = DirectorWrapMode.Hold;

        SetUIActive(false);
        m_buttonSelector.LockCursor();
    }

    void Update()
    {
        m_timer += Time.unscaledDeltaTime;

        // FadeIn の完了監視
        if (m_isFadeInPlaying)
        {
            if (m_directorFadeIn.time >= m_directorFadeIn.duration)
            {
                m_isFadeInPlaying = false;
                OnFadeInFinished();
            }
        }
        // FadeOut の完了監視(追加)
        if (m_isFadeOutPlaying)
        {
            if (m_directorFadeOut.time >= m_directorFadeOut.duration)
            {
                m_isFadeOutPlaying = false;
                OnFadeOutFinished();
            }
        }


        // デバッグ用（スペースキーで表示・非表示）
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && m_isPauseActive)
        {
            if (m_isOnWindow == 0)
            {
                Play();
                m_isOnWindow = 1;
            }
            else if (m_isOnWindow == 1)
            {
                Close();
                m_isOnWindow = 0;
            }
        }

        if (!m_isPauseActive)
        {
            if (m_timer >= m_intavalTime)
            {
                m_isPauseActive = true;
            }
        }
    }

    public void Play()
    {
        m_timer = 0;

        m_directorFadeOut.Stop();
        m_directorFadeOut.time = 0;

        m_isFadeOutPlaying = false;

        SetUIActive(true);
        m_buttonSelector.UnLockCursor();

        m_buttonSelector.ResetCursor();

        if (m_cursor != null)
        {
            m_cursor.SetActive(true);

            // カーソルの初期位置（UI座標）を直接セット
            RectTransform rect = m_cursor.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = m_cursorInitialPosition;
            }
        }

        // 全体の FadeIn Timeline を再生
        m_directorFadeIn.time = 0;
        m_directorFadeIn.Evaluate();
        m_directorFadeIn.Play();

        m_isFadeInPlaying = true;
        m_isPauseActive = false;
    }

    public void Close()
    {
        m_timer = 0;

        m_isFadeInPlaying = false;

        m_directorFadeIn.Stop();

        m_directorFadeOut.time = 0;
        m_directorFadeOut.Evaluate();
        m_directorFadeOut.Play();

        m_isFadeOutPlaying = true;
        m_isPauseActive = false;
    }

    private void OnFadeInFinished()
    {
        if (m_cursor != null)
        {
            CanvasGroup group = m_cursor.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 1.0f;
                group.blocksRaycasts = true;
                group.interactable = true;
            }

            m_cursor.transform.SetAsLastSibling();
        }

        StartScriptCursorControl();
    }

    private void OnFadeOutFinished()
    {
        SetUIActive(false);
        m_buttonSelector.LockCursor();
    }

    private void StartScriptCursorControl()
    {
        Debug.Log("FadeIn完了: スクリプトによるカーソル制御を開始します");
    }

    private void SetUIActive(bool isActive)
    {
        if (m_container != null) m_container.SetActive(isActive);
        if (m_detailText != null) m_detailText.SetActive(isActive);
        if (m_background != null) m_background.SetActive(isActive);
        if (m_selectButton != null) m_selectButton.SetActive(isActive);
        if (m_mechPanel != null) m_mechPanel.SetActive(isActive);
    }
}