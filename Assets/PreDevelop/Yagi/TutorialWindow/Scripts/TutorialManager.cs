using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;

public class TutorialManager : MonoBehaviour
{
    [Header("チュートリアルデータ")]
    [SerializeField] private TutorialData tutorialData;

    [Header("画像")]
    [SerializeField] private Image currentSlideImage;
    [SerializeField] private Image nextSlideImage;

    [Header("RectTransform")]
    [SerializeField] private RectTransform currentRect;
    [SerializeField] private RectTransform nextRect;

    [Header("設定")]
    [SerializeField] private float slideTime = 0.4f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    [Header("ページインジケーター")]
    [SerializeField]
    private Transform dotRoot;

    [SerializeField]
    private GameObject dotPrefab;

    [SerializeField]
    private Sprite currentDotSprite;

    [SerializeField]
    private Sprite normalDotSprite;

    /// <summary>
    /// 生成したDot一覧
    /// </summary>
    private Image[] dots;

    /// <summary>
    /// 現在のページ
    /// </summary>
    private int currentIndex = 0;

    /// <summary>
    /// アニメーション中か
    /// </summary>
    private bool isAnimating = false;

    /// <summary>
    /// スライド幅
    /// </summary>
    private float slideWidth;

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (isAnimating)
            return;

        InputUpdate();
    }

    private void Initialize()
    {
        if (tutorialData == null)
        {
            Debug.LogError("TutorialDataが設定されていません");
            return;
        }

        if (tutorialData.slideImages.Length == 0)
        {
            Debug.LogError("画像がありません");
            return;
        }

        currentIndex = 0;

        currentSlideImage.sprite = tutorialData.slideImages[currentIndex];

        slideWidth = currentRect.rect.width;

        nextSlideImage.gameObject.SetActive(false);

        currentRect.anchoredPosition = Vector2.zero;
        nextRect.anchoredPosition = new Vector2(slideWidth, 0);

        CreateDots();
    }

    /// <summary>
    /// ページインジケーター生成
    /// </summary>
    private void CreateDots()
    {
        // 古いDotを削除
        foreach (Transform child in dotRoot)
        {
            Destroy(child.gameObject);
        }

        dots = new Image[tutorialData.slideImages.Length];

        for (int i = 0; i < tutorialData.slideImages.Length; i++)
        {
            GameObject dot = Instantiate(dotPrefab, dotRoot);

            dots[i] = dot.GetComponent<Image>();
        }

        UpdateDots();
    }

    private void InputUpdate()
    {
        // 次へ
        if (Keyboard.current.dKey.wasPressedThisFrame ||
            Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            NextPage();
        }

        // 前へ
        if (Keyboard.current.aKey.wasPressedThisFrame ||
            Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            PreviousPage();
        }

        // 閉じる
        if (Keyboard.current.escapeKey.wasPressedThisFrame ||
            Keyboard.current.cKey.wasPressedThisFrame)
        {
            CloseTutorial();
        }

        if (Gamepad.current == null)
            return;

        // Xbox 次へ
        if (Gamepad.current.dpad.right.wasPressedThisFrame ||
            Gamepad.current.leftStick.right.wasPressedThisFrame)
        {
            NextPage();
        }

        // Xbox 前へ
        if (Gamepad.current.dpad.left.wasPressedThisFrame ||
            Gamepad.current.leftStick.left.wasPressedThisFrame)
        {
            PreviousPage();
        }

        // Xbox B
        if (Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            CloseTutorial();
        }
    }

    private void NextPage()
    {
        if (currentIndex >= tutorialData.slideImages.Length - 1)
        {
            CloseTutorial();
            return;
        }

        isAnimating = true;

        nextSlideImage.gameObject.SetActive(true);

        nextSlideImage.sprite =
            tutorialData.slideImages[currentIndex + 1];

        currentRect.anchoredPosition = Vector2.zero;
        nextRect.anchoredPosition = new Vector2(slideWidth, 0);

        Sequence sequence = DOTween.Sequence();

        sequence.Join(
            currentRect.DOAnchorPosX(-slideWidth, slideTime)
        );

        sequence.Join(
            nextRect.DOAnchorPosX(0, slideTime)
        );

        sequence.SetEase(ease);

        sequence.OnComplete(() =>
        {
            currentIndex++;

            // CurrentとNextを入れ替える
            Image tempImage = currentSlideImage;
            currentSlideImage = nextSlideImage;
            nextSlideImage = tempImage;

            RectTransform tempRect = currentRect;
            currentRect = nextRect;
            nextRect = tempRect;

            // 次のスライドを右側へ戻して待機
            nextRect.anchoredPosition = new Vector2(slideWidth, 0);
            nextSlideImage.gameObject.SetActive(false);

            // ページインジケーター更新
            UpdateDots();

            isAnimating = false;

            Debug.Log($"現在ページ : {currentIndex + 1}");
        });
    }

    private void PreviousPage()
    {
        if (currentIndex <= 0)
            return;

        isAnimating = true;

        nextSlideImage.gameObject.SetActive(true);

        nextSlideImage.sprite =
            tutorialData.slideImages[currentIndex - 1];

        currentRect.anchoredPosition = Vector2.zero;
        nextRect.anchoredPosition = new Vector2(-slideWidth, 0);

        Sequence sequence = DOTween.Sequence();

        sequence.Join(
            currentRect.DOAnchorPosX(slideWidth, slideTime)
        );

        sequence.Join(
            nextRect.DOAnchorPosX(0, slideTime)
        );

        sequence.SetEase(ease);

        sequence.OnComplete(() =>
        {
            // ★ここは--が正しい
            currentIndex--;

            // CurrentとNextを入れ替える
            Image tempImage = currentSlideImage;
            currentSlideImage = nextSlideImage;
            nextSlideImage = tempImage;

            RectTransform tempRect = currentRect;
            currentRect = nextRect;
            nextRect = tempRect;

            // 左側へ戻して待機
            nextRect.anchoredPosition = new Vector2(-slideWidth, 0);
            nextSlideImage.gameObject.SetActive(false);

            // ページインジケーター更新
            UpdateDots();

            isAnimating = false;

            Debug.Log($"現在ページ : {currentIndex + 1}");
        });
    }

    private void CloseTutorial()
    {
        DOTween.Kill(currentRect);
        DOTween.Kill(nextRect);

        gameObject.SetActive(false);

        Debug.Log("Tutorial Close");
    
    }
    /// <summary>
    /// ページインジケーター更新
    /// </summary>
    private void UpdateDots()
    {
        if (dots == null || dots.Length == 0)
            return;

        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] == null)
                continue;

            if (i == currentIndex)
            {
                dots[i].sprite = currentDotSprite;
            }
            else
            {
                dots[i].sprite = normalDotSprite;
            }
        }
    }
}