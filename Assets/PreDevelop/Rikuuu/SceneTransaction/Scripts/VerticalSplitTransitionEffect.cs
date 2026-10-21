using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

public enum FadeMode
{ 
    NONE = 0,

    FADE_IN, 
    FADE_OUT
}

public class VerticalSplitTransitionEffect : SceneTransitionEffect
{
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
    [HideInInspector]
    public float m_closeRate = 0.0f;

    // パネルオブジェクト
    private GameObject m_canvasObject;

    // 上パネル
    private RectTransform m_topPanel;
    // 下パネル
    private RectTransform m_bottomPanel;

    // 生成直後の閉じ具合
    private float m_initialRate = 0.0f;
    // 再生完了後の閉じ具合
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

        // アニメーターの生成
        m_animator = GetComponent<Animator>();
        if(m_animator == null)
        {
            m_animator = gameObject.AddComponent<Animator>();
        }
        // ポーズ中でも動かす
        m_animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        // 生成直後の状態設定
        switch(m_fadeMode)
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
        SetCloseRate(m_initialRate);
    }

    private void OnDestroy()
    {
        if(m_graph.IsValid())
        {
            m_graph.Destroy();
        }

        if(m_canvasObject != null)
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

        // クリップを再生
        AnimationPlayableUtilities.PlayClip(
            m_animator, 
            m_animationClip, 
            out m_graph
        );
        // グラフの時間もUnscaledにする(ポーズ中でも進めるため)
        m_graph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);

        yield return new WaitForSecondsRealtime(m_animationClip.length);

        SetCloseRate(m_finalRate);
        m_graph.Destroy();

        m_isTransitioning = false;
        onComplete?.Invoke();
    }

    // ----------------------------------------------------------

    private void CreateCover()
    {
        m_canvasObject = new GameObject("VerticalSplitOutCanvas");
        m_canvasObject.transform.SetParent(transform, false);

        Canvas canvas = m_canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = m_sortingOrder;

        CanvasScaler scaler = m_canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        m_canvasObject.AddComponent<GraphicRaycaster>();

        m_topPanel = CreatePanel("TopPanel");
        m_bottomPanel = CreatePanel("BottomPanel");
    }

    private RectTransform CreatePanel(string panelName)
    {
        GameObject panelObject = new GameObject(panelName);
        panelObject.transform.SetParent(m_canvasObject.transform, false);

        UnityEngine.UI.Image image = panelObject.AddComponent<UnityEngine.UI.Image>();
        image.color = m_color;
        image.raycastTarget = true;

        return image.rectTransform;
    }

    private void SetCloseRate(float rate)
    {
        // 上パネル:上端から中央へ伸びる
        m_topPanel.anchorMin = new Vector2(0f, Mathf.Lerp(1f, 0.5f, rate));
        m_topPanel.anchorMax = new Vector2(1f, 1f);

        // 下パネル:下端から中央へ伸びる
        m_bottomPanel.anchorMin = new Vector2(0f, 0f);
        m_bottomPanel.anchorMax = new Vector2(1f, Mathf.Lerp(0f, 0.5f, rate));

        // アンカーに合わせて余白をゼロにする
        m_topPanel.offsetMin = m_topPanel.offsetMax = Vector2.zero;
        m_bottomPanel.offsetMin = m_bottomPanel.offsetMax = Vector2.zero;
    }
}