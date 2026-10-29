using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gameplay Cueの状態変化に応じて、対応するチュートリアルUIを表示します。
/// </summary>
[DisallowMultipleComponent]
public sealed class TutorialController : MonoBehaviour
{
    [Serializable]
    private sealed class CueUiBinding
    {
        [SerializeField, Header("対象Cue")]
        private GameplayCueAsset m_cue;

        [SerializeField, Header("表示するUI")]
        private GameObject m_uiObject;

        public GameplayCueAsset Cue => m_cue;

        public GameObject UiObject => m_uiObject;
    }

    [SerializeField, Header("Cue変更イベント")]
    private GameplayCueChangedEvent m_cueChangedEvent;

    [SerializeField, Header("CueとUIの対応")]
    private List<CueUiBinding> m_cueUiBindings = new();

    private GameplayCueAsset m_currentCue;
    private GameObject m_currentUiObject;
    private readonly List<GameplayCueAsset> m_activeCues = new();

    /// <summary>
    /// Cue変更イベントを購読し、UIを初期化します。
    /// </summary>
    private void OnEnable()
    {
        HideAllUi(false);
        m_currentCue = null;
        m_currentUiObject = null;
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

        HideAllUi(false);
        m_currentCue = null;
        m_currentUiObject = null;
        m_activeCues.Clear();
    }

    /// <summary>
    /// Cueの開始・終了に応じて表示UIを切り替えます。
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
                ShowCueUi(eventData.Cue);
                break;

            case GameplayCueEventType.FINISHED:
                HideFinishedCueUi(eventData.Cue);
                break;
        }
    }

    /// <summary>
    /// 指定Cueに対応するUIへ表示を切り替えます。
    /// </summary>
    /// <param name="cue">開始されたCue。</param>
    private void ShowCueUi(GameplayCueAsset cue)
    {
        m_activeCues.Remove(cue);
        m_activeCues.Add(cue);

        RefreshUi();
    }

    /// <summary>
    /// 現在表示中のCueが終了した場合だけUIを非表示にします。
    /// </summary>
    /// <param name="cue">終了されたCue。</param>
    private void HideFinishedCueUi(GameplayCueAsset cue)
    {
        m_activeCues.Remove(cue);

        RefreshUi();
    }

    /// <summary>
    /// 有効なCueのうち、最後に開始されたCueのUIを表示します。
    /// </summary>
    private void RefreshUi()
    {
        HideAllUi(true);
        m_currentCue = null;
        m_currentUiObject = null;

        if (m_activeCues.Count == 0)
        {
            return;
        }

        GameplayCueAsset cue =
            m_activeCues[m_activeCues.Count - 1];
        GameObject uiObject = FindUiObject(cue);

        if (uiObject == null)
        {
            Debug.LogWarning(
                $"Cueに対応するUIが設定されていません。 Cue: {cue.name}",
                this);
            return;
        }

        m_currentCue = cue;
        m_currentUiObject = uiObject;
        GetUiAnimator(m_currentUiObject).PlayShow();
    }

    /// <summary>
    /// 指定Cueに対応するUIを検索します。
    /// </summary>
    /// <param name="cue">検索対象のCue。</param>
    /// <returns>対応するUI。未設定の場合はnull。</returns>
    private GameObject FindUiObject(GameplayCueAsset cue)
    {
        foreach (CueUiBinding binding in m_cueUiBindings)
        {
            if (binding == null || binding.Cue != cue)
            {
                continue;
            }

            return binding.UiObject;
        }

        return null;
    }

    /// <summary>
    /// 対応表に登録されたUIをすべて非表示にします。
    /// </summary>
    private void HideAllUi(bool animate)
    {
        foreach (CueUiBinding binding in m_cueUiBindings)
        {
            if (binding?.UiObject == null)
            {
                continue;
            }

            UiTweenAnimator animator = GetUiAnimator(binding.UiObject);
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
            animator.SetAnimationType(UiTweenAnimator.AnimationType.HORIZONTAL_EXPAND);
        }
        return animator;
    }
}
