using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gameplay Cueの状態変化に応じて、対応するヒントUIを表示します。
/// </summary>
[DisallowMultipleComponent]
public sealed class HintController : MonoBehaviour
{
    [Serializable]
    private sealed class CueHintBinding
    {
        [SerializeField, Header("対象Cue")]
        private GameplayCueAsset m_cue;

        [SerializeField, Header("表示するヒントUI")]
        private GameObject m_hintUiObject;

        /// <summary>
        /// 対応するCueを取得します。
        /// </summary>
        public GameplayCueAsset Cue => m_cue;

        /// <summary>
        /// 対応するヒントUIを取得します。
        /// </summary>
        public GameObject HintUiObject => m_hintUiObject;
    }

    [SerializeField, Header("Cue変更イベント")]
    private GameplayCueChangedEvent m_cueChangedEvent;

    [SerializeField, Header("CueとヒントUIの対応")]
    private List<CueHintBinding> m_cueHintBindings = new();

    private GameplayCueAsset m_currentCue;
    private GameObject m_currentHintUiObject;
    private readonly List<GameplayCueAsset> m_activeCues = new();

    /// <summary>
    /// Cue変更イベントを購読し、ヒントUIを初期化します。
    /// </summary>
    private void OnEnable()
    {
        HideAllHintUi(false);
        m_currentCue = null;
        m_currentHintUiObject = null;
        m_activeCues.Clear();

        if (m_cueChangedEvent == null)
        {
            Debug.LogWarning(
                $"{nameof(GameplayCueChangedEvent)}が設定されていません。",
                this);
            return;
        }

        m_cueChangedEvent.RegisterListener(OnCueChanged);
    }

    /// <summary>
    /// Cue変更イベントの購読を解除します。
    /// </summary>
    private void OnDisable()
    {
        if (m_cueChangedEvent != null)
        {
            m_cueChangedEvent.UnregisterListener(OnCueChanged);
        }

        HideAllHintUi(false);
        m_currentCue = null;
        m_currentHintUiObject = null;
        m_activeCues.Clear();
    }

    /// <summary>
    /// Cueの開始・終了に応じて表示ヒントUIを切り替えます。
    /// </summary>
    /// <param name="eventData">Cueの状態変更情報。</param>
    private void OnCueChanged(GameplayCueChangedEventData eventData)
    {
        if (eventData.Cue == null)
        {
            return;
        }

        switch (eventData.EventType)
        {
            case GameplayCueEventType.STARTED:
                ShowCueHintUi(eventData.Cue);
                break;

            case GameplayCueEventType.FINISHED:
                HideFinishedCueHintUi(eventData.Cue);
                break;
        }
    }

    /// <summary>
    /// 指定Cueに対応するヒントUIへ表示を切り替えます。
    /// </summary>
    /// <param name="cue">開始されたCue。</param>
    private void ShowCueHintUi(GameplayCueAsset cue)
    {
        m_activeCues.Remove(cue);
        m_activeCues.Add(cue);

        RefreshHintUi();
    }

    /// <summary>
    /// 現在表示中のCueが終了した場合だけヒントUIを非表示にします。
    /// </summary>
    /// <param name="cue">終了されたCue。</param>
    private void HideFinishedCueHintUi(GameplayCueAsset cue)
    {
        m_activeCues.Remove(cue);

        RefreshHintUi();
    }

    /// <summary>
    /// 有効なCueのうち、最後に開始されたCueのヒントUIを表示します。
    /// </summary>
    private void RefreshHintUi()
    {
        HideAllHintUi(true);
        m_currentCue = null;
        m_currentHintUiObject = null;

        if (m_activeCues.Count == 0)
        {
            return;
        }

        GameplayCueAsset cue =
            m_activeCues[m_activeCues.Count - 1];
        GameObject hintUiObject = FindHintUiObject(cue);

        if (hintUiObject == null)
        {
            Debug.LogWarning(
                $"Cueに対応するヒントUIが設定されていません。 Cue: {cue.name}",
                this);
            return;
        }

        m_currentCue = cue;
        m_currentHintUiObject = hintUiObject;
        GetUiAnimator(m_currentHintUiObject).PlayShow();
    }

    /// <summary>
    /// 指定Cueに対応するヒントUIを検索します。
    /// </summary>
    /// <param name="cue">検索対象のCue。</param>
    /// <returns>対応するヒントUI。未設定の場合はnull。</returns>
    private GameObject FindHintUiObject(GameplayCueAsset cue)
    {
        foreach (CueHintBinding binding in m_cueHintBindings)
        {
            if (binding == null || binding.Cue != cue)
            {
                continue;
            }

            return binding.HintUiObject;
        }

        return null;
    }

    /// <summary>
    /// 対応表に登録されたヒントUIをすべて非表示にします。
    /// </summary>
    private void HideAllHintUi(bool animate)
    {
        foreach (CueHintBinding binding in m_cueHintBindings)
        {
            if (binding?.HintUiObject == null)
            {
                continue;
            }

            UiTweenAnimator animator = GetUiAnimator(binding.HintUiObject);
            if (animate)
            {
                animator.PlayHide();
            }
            else
            {
                animator.HideImmediately();
            }
        }
    }

    /// <summary>
    /// 指定UIのアニメーションコンポーネントを取得します。
    /// </summary>
    /// <param name="uiObject">対象UI。</param>
    /// <returns>UIアニメーションコンポーネント。</returns>
    private UiTweenAnimator GetUiAnimator(GameObject uiObject)
    {
        UiTweenAnimator animator = uiObject.GetComponent<UiTweenAnimator>();
        if (animator == null)
        {
            animator = uiObject.AddComponent<UiTweenAnimator>();
            animator.SetAnimationType(UiTweenAnimator.AnimationType.SLIDE);
        }
        return animator;
    }
}
