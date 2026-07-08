using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutorialWindowController : MonoBehaviour
{
    [Header("データ")]
    [SerializeField] private TutorialSlideData slideData;

    [Header("スライド表示領域")]
    [Tooltip("RectMask2D等でマスクされたビューポート（説明ウィンドウの表示範囲）")]
    [SerializeField] private RectTransform viewport;
    [Tooltip("全スライドを横一列に並べて格納する親RectTransform")]
    [SerializeField] private RectTransform slideRoot;
    [Tooltip("Imageコンポーネントを持つスライド1枚分のプレハブ")]
    [SerializeField] private GameObject slideImagePrefab;

    [Header("矢印")]
    [SerializeField] private RectTransform leftArrow;
    [SerializeField] private RectTransform rightArrow;
    [SerializeField] private float arrowBounceAmplitude = 8f;
    [SerializeField] private float arrowBounceSpeed = 2f;

    [Header("ドットインジケーター")]
    [SerializeField] private Transform dotsContainer;
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private Sprite dotEmptySprite;
    [SerializeField] private Sprite dotFilledSprite;

    [Header("遷移設定")]
    [SerializeField] private float transitionDuration = 0.35f;
    [SerializeField] private AnimationCurve transitionEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("入力設定")]
    [Tooltip("スティック/軸がこの値を超えたら入力とみなす")]
    [SerializeField] private float stickThreshold = 0.5f;

    [Header("終了時に非表示にするルート")]
    [SerializeField] private GameObject windowRoot;

    public event Action OnWindowClosed;

    private readonly List<RectTransform> slideRects = new List<RectTransform>();
    private readonly List<Image> dotImages = new List<Image>();

    private InputAction navigateAction;
    private InputAction closeAction;

    private Vector2 leftArrowBasePos;
    private Vector2 rightArrowBasePos;

    private int currentIndex = 0;
    private bool isTransitioning = false;
    private bool waitingForNeutral = false;

    private void Awake()
    {
        // ナビゲーション（左右）: キーボードA/D・矢印キー、ゲームパッド左スティック・十字キー
        navigateAction = new InputAction("Navigate", InputActionType.Value, expectedControlType: "Axis");
        navigateAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Negative", "<Gamepad>/leftStick/left")
            .With("Negative", "<Gamepad>/dpad/left")
            .With("Positive", "<Keyboard>/d")
            .With("Positive", "<Keyboard>/rightArrow")
            .With("Positive", "<Gamepad>/leftStick/right")
            .With("Positive", "<Gamepad>/dpad/right");

        // 閉じる: PC C/ESC、Xbox Bボタン(buttonEast)
        closeAction = new InputAction("Close", InputActionType.Button);
        closeAction.AddBinding("<Keyboard>/c");
        closeAction.AddBinding("<Keyboard>/escape");
        closeAction.AddBinding("<Gamepad>/buttonEast");

        if (leftArrow != null) leftArrowBasePos = leftArrow.anchoredPosition;
        if (rightArrow != null) rightArrowBasePos = rightArrow.anchoredPosition;
    }

    private void OnEnable()
    {
        navigateAction.Enable();
        closeAction.Enable();
        closeAction.performed += OnClosePerformed;
    }

    private void OnDisable()
    {
        closeAction.performed -= OnClosePerformed;
        navigateAction.Disable();
        closeAction.Disable();
    }

    private void Start()
    {
        BuildSlides();
        BuildDots();
        UpdateDots();
        SnapToIndex(currentIndex);
    }

    private void Update()
    {
        AnimateArrows();
        HandleNavigationInput();
    }

    // ---------- 初期構築 ----------

    private void BuildSlides()
    {
        float width = viewport.rect.width;

        for (int i = 0; i < slideData.slideImages.Length; i++)
        {
            GameObject go = Instantiate(slideImagePrefab, slideRoot);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(width, 0f);
            rt.anchoredPosition = new Vector2(i * width, 0f);

            Image img = go.GetComponent<Image>();
            img.sprite = slideData.slideImages[i];

            slideRects.Add(rt);
        }

        slideRoot.sizeDelta = new Vector2(width * slideData.slideImages.Length, slideRoot.sizeDelta.y);
    }

    private void BuildDots()
    {
        for (int i = 0; i < slideData.slideImages.Length; i++)
        {
            GameObject go = Instantiate(dotPrefab, dotsContainer);
            dotImages.Add(go.GetComponent<Image>());
        }
    }

    private void UpdateDots()
    {
        for (int i = 0; i < dotImages.Count; i++)
        {
            dotImages[i].sprite = (i == currentIndex) ? dotFilledSprite : dotEmptySprite;
        }
    }

    // ---------- 入力処理 ----------

    private void HandleNavigationInput()
    {
        float navValue = navigateAction.ReadValue<float>();

        if (!waitingForNeutral)
        {
            if (navValue >= stickThreshold)
            {
                NextSlide();
                waitingForNeutral = true;
            }
            else if (navValue <= -stickThreshold)
            {
                PreviousSlide();
                waitingForNeutral = true;
            }
        }
        else if (Mathf.Abs(navValue) < stickThreshold * 0.5f)
        {
            // スティックが一度ニュートラルに戻るまで次の入力を受け付けない（連続入力防止）
            waitingForNeutral = false;
        }
    }

    private void OnClosePerformed(InputAction.CallbackContext ctx)
    {
        CloseWindow();
    }

    // ---------- スライド送り ----------

    private void NextSlide()
    {
        if (isTransitioning) return;

        if (currentIndex >= slideData.slideImages.Length - 1)
        {
            // 最後のスライドから次へ入力 → ウィンドウを閉じる
            CloseWindow();
            return;
        }

        StartCoroutine(TransitionTo(currentIndex + 1));
    }

    private void PreviousSlide()
    {
        if (isTransitioning) return;
        if (currentIndex <= 0) return;

        StartCoroutine(TransitionTo(currentIndex - 1));
    }

    private void SnapToIndex(int index)
    {
        float width = viewport.rect.width;
        slideRoot.anchoredPosition = new Vector2(-index * width, slideRoot.anchoredPosition.y);
        currentIndex = index;
    }

    private IEnumerator TransitionTo(int targetIndex)
    {
        isTransitioning = true;

        float width = viewport.rect.width;
        float startX = slideRoot.anchoredPosition.x;
        float endX = -targetIndex * width;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionEase.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));
            float x = Mathf.Lerp(startX, endX, t);
            slideRoot.anchoredPosition = new Vector2(x, slideRoot.anchoredPosition.y);
            yield return null;
        }

        slideRoot.anchoredPosition = new Vector2(endX, slideRoot.anchoredPosition.y);
        currentIndex = targetIndex;
        UpdateDots();
        isTransitioning = false;
    }

    // ---------- 矢印アニメーション ----------

    private void AnimateArrows()
    {
        float offset = Mathf.Sin(Time.time * arrowBounceSpeed) * arrowBounceAmplitude;

        if (leftArrow != null)
            leftArrow.anchoredPosition = leftArrowBasePos + new Vector2(-Mathf.Abs(offset), 0f);

        if (rightArrow != null)
            rightArrow.anchoredPosition = rightArrowBasePos + new Vector2(Mathf.Abs(offset), 0f);
    }

    // ---------- 終了処理 ----------

    private void CloseWindow()
    {
        if (windowRoot != null) windowRoot.SetActive(false);
        OnWindowClosed?.Invoke();
    }

    // ---------- UIボタン用（マウスクリック対応） ----------

    public void OnClickNext() => NextSlide();
    public void OnClickPrevious() => PreviousSlide();
    public void OnClickClose() => CloseWindow();
}