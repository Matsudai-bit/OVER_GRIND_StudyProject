using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;


public class FadeTransitionEffect : SceneTransitionEffect
{
    // 1フレームで進める時間の上限(重いフレームでアニメが飛ばないようにする)
    private const float MAX_FRAME_DELTA = 1.0f / 30.0f;

    // アニメーションクリップ
    [SerializeField]
    private AnimationClip m_animationClip;
    // パネルの色
    [SerializeField]
    private Color m_color = Color.black;
    // Canvasの描画優先度
    [SerializeField]
    private int m_sortingOrder = 999;

    // フェードモード
    [SerializeField]
    private FadeMode m_fadeMode;

    // アニメーションが書き込む値
    //[HideInInspector]
    public float m_closeRate = 0.0f;

    // パネルオブジェクト
    private GameObject m_canvasObject;

    // パネル
    private Image m_panel;

    // 生成直後の表示具合
    private float m_initialRate = 0.0f;
    // 再生完了後の表示具合
    private float m_finalRate = 1.0f;

    // クリップ再生用のアニメーター
    private Animator m_animator;
    // クリップ再生用のグラフ
    private PlayableGraph m_graph;

    // トランジション中かどうか
    private bool m_isTransitioning = false;

    private void Awake()
    {
        // パネルを作成する
        CreateCover();

        // アニメーターの取得
        m_animator = GetComponent<Animator>();
        if (m_animator == null)
        {
            m_animator = gameObject.AddComponent<Animator>();
        }
        // ポーズ中でも動かす
        m_animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        // モードごとの初期状態・最終状態を設定する
        switch (m_fadeMode)
        {
            case FadeMode.FADE_IN:
                m_initialRate = 1.0f;
                m_finalRate = 0.0f;
                break;

            case FadeMode.FADE_OUT:
                m_initialRate = 0.0f;
                m_finalRate = 1.0f;
                break;

            default:
                break;
        }

        // 生成直後の状態にする
        SetCloseRate(m_initialRate);
        m_closeRate = m_initialRate;
    }

    private void OnDestroy()
    {
        if (m_graph.IsValid())
        {
            m_graph.Destroy();
        }

        if (m_canvasObject != null)
        {
            Destroy(m_canvasObject);
        }
    }

    private void LateUpdate()
    {
        if (m_isTransitioning)
        {
            SetCloseRate(m_closeRate);
        }
    }

    public override bool IsTransitioning()
    {
        return m_isTransitioning;
    }

    public override void StartTransition(Action onComplete)
    {
        StartCoroutine(Play(onComplete));
    }

    private IEnumerator Play(Action onComplete)
    {
        // クリップ未設定の場合
        if (m_animationClip == null)
        {
            Debug.LogWarning($"{gameObject.name}: m_animationClip が設定されていません。", this);

            SetCloseRate(m_finalRate);
            onComplete?.Invoke();

            yield break;
        }

        m_isTransitioning = true;

        // クリップを再生(時間は自前で進めるため、手動更新にする)
        AnimationClipPlayable playable = AnimationPlayableUtilities.PlayClip(
            m_animator,
            m_animationClip,
            out m_graph
        );
        m_graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

        float length = m_animationClip.length;
        float time = 0.0f;

        while (time < length)
        {
            // 現在時刻のポーズを評価して、パネルに反映する
            playable.SetTime(time);
            m_graph.Evaluate();
            SetCloseRate(m_closeRate);

            yield return null;

            // 重いフレームでも、1フレームで進める量に上限をかける
            time += Mathf.Min(Time.unscaledDeltaTime, MAX_FRAME_DELTA);
        }

        // 最終状態を確実に反映
        SetCloseRate(m_finalRate);
        m_graph.Destroy();

        m_isTransitioning = false;
        onComplete?.Invoke();
    }

    // ----------------------------------------------------------

    private void CreateCover()
    {
        // 自身の子として生成する
        m_canvasObject = new GameObject("FadeCanvas");
        m_canvasObject.transform.SetParent(transform, false);

        Canvas canvas = m_canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = m_sortingOrder;

        CanvasScaler scaler = m_canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        m_canvasObject.AddComponent<GraphicRaycaster>();

        // 全面を覆うパネル
        GameObject panelObject = new GameObject("Panel");
        panelObject.transform.SetParent(m_canvasObject.transform, false);

        m_panel = panelObject.AddComponent<Image>();
        m_panel.color = m_color;
        // 遷移中の入力を遮る
        m_panel.raycastTarget = true;

        RectTransform rect = m_panel.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // 表示具合をパネルのアルファに反映する
    private void SetCloseRate(float rate)
    {
        Color color = m_color;
        color.a = Mathf.Clamp01(rate);
        m_panel.color = color;
    }
}