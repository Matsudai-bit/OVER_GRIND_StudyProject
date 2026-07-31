using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    [Header("チュートリアルデータ")]
    [SerializeField] private TutorialData tutorialData;

    [Header("GIF再生スロット")]
    [SerializeField] private GifPlayer currentGifPlayer;
    [SerializeField] private GifPlayer nextGifPlayer;

    [Header("挿絵(左上に斜めに配置するもの)")]
    [SerializeField] private Image currentOverlayImage;
    [SerializeField] private Image nextOverlayImage;

    [Header("フェード用CanvasGroup")]
    [SerializeField] private CanvasGroup currentCanvasGroup;
    [SerializeField] private CanvasGroup nextCanvasGroup;

    [Header("RectTransform")]
    [SerializeField] private RectTransform currentRect;
    [SerializeField] private RectTransform nextRect;
    [SerializeField] private RectTransform slideRoot;

    [Header("スライド設定")]
    [SerializeField] private float slideTime = 0.4f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    [Header("ページインジケーター")]
    [SerializeField] private Transform dotRoot;
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private Sprite currentDotSprite;
    [SerializeField] private Sprite normalDotSprite;

    public Action OnTutorialClosed;

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

    /// <summary>
    /// スライドごとにデコード済みのGIFフレームをキャッシュしておく
    /// </summary>
    private List<UniGif.GifTexture>[] slideFramesCache;

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

        if (tutorialData.Slides == null || tutorialData.Slides.Length == 0)
        {
            Debug.LogError("スライドがありません");
            return;
        }

        currentIndex = 0;

        slideWidth = slideRoot.rect.width + currentRect.rect.width;

        currentRect.gameObject.SetActive(true);
        currentRect.anchoredPosition = Vector2.zero;
        currentRect.localScale = Vector3.one;
        currentCanvasGroup.alpha = 1f;

        nextRect.anchoredPosition = new Vector2(slideWidth, 0);
        nextRect.localScale = Vector3.one;
        nextCanvasGroup.alpha = 1f;
        nextRect.gameObject.SetActive(false);

        // すでにデコード済みならそのまま再生、未デコードなら全スライドを順にデコードする
        if (slideFramesCache == null || slideFramesCache.Length != tutorialData.Slides.Length)
        {
            slideFramesCache = new List<UniGif.GifTexture>[tutorialData.Slides.Length];
            StartCoroutine(DecodeAllSlidesRoutine());
        }
        else
        {
            SetSlide(currentGifPlayer, currentOverlayImage, currentIndex);
        }

        CreateDots();
    }

    /// <summary>
    /// 全スライドのGIFを順番にデコードしてキャッシュする
    /// </summary>
    private IEnumerator DecodeAllSlidesRoutine()
    {
        for (int i = 0; i < tutorialData.Slides.Length; i++)
        {
            byte[] gifBytes = tutorialData.Slides[i].GifBytes;

            if (gifBytes == null)
            {
                Debug.LogError($"{i}番目のスライドにGIFファイルが設定されていません");
                continue;
            }

            List<UniGif.GifTexture> frames = null;

            yield return StartCoroutine(
                UniGif.GetTextureListCoroutine(gifBytes, (gifTexList, loopCount, width, height) =>
                {
                    frames = gifTexList;
                })
            );

            slideFramesCache[i] = frames;

            // 表示中のスライドのデコードが終わったタイミングで再生を始める
            if (i == currentIndex)
            {
                SetSlide(currentGifPlayer, currentOverlayImage, currentIndex);
            }
        }
    }

    /// <summary>
    /// 指定したスロット(GifPlayerと挿絵)に、指定インデックスのスライドを反映する。
    /// GIFはデコードが終わっていなければ完了を待つ。
    /// </summary>
    private void SetSlide(GifPlayer player, Image overlayImage, int index)
    {
        PlaySlide(player, index);

        Sprite sprite = tutorialData.Slides[index].OverlayImage;
        overlayImage.sprite = sprite;
        overlayImage.gameObject.SetActive(sprite != null);
    }

    /// <summary>
    /// 指定したGifPlayerに、指定インデックスのGIFを再生させる。
    /// デコードがまだ終わっていない場合は完了を待つ。
    /// </summary>
    private void PlaySlide(GifPlayer player, int index)
    {
        if (slideFramesCache[index] != null)
        {
            player.Play(slideFramesCache[index]);
        }
        else
        {
            StartCoroutine(WaitAndPlaySlide(player, index));
        }
    }

    private IEnumerator WaitAndPlaySlide(GifPlayer player, int index)
    {
        while (slideFramesCache[index] == null)
        {
            yield return null;
        }

        player.Play(slideFramesCache[index]);
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

        dots = new Image[tutorialData.Slides.Length];

        for (int i = 0; i < tutorialData.Slides.Length; i++)
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
        if (currentIndex >= tutorialData.Slides.Length - 1)
        {
            CloseTutorial();
            return;
        }

        PlayTransition(currentIndex + 1, slideWidth);
    }

    private void PreviousPage()
    {
        if (currentIndex <= 0)
            return;

        PlayTransition(currentIndex - 1, -slideWidth);
    }

    /// <summary>
    /// 横スライドによるページ送りアニメーション。
    /// enterFromXが正なら次へ(右から登場)、負なら前へ(左から登場)。
    /// </summary>
    private void PlayTransition(int targetIndex, float enterFromX)
    {
        isAnimating = true;

        nextRect.gameObject.SetActive(true);
        nextRect.anchoredPosition = new Vector2(enterFromX, 0);
        nextRect.localScale = Vector3.one;
        nextCanvasGroup.alpha = 1f;

        SetSlide(nextGifPlayer, nextOverlayImage, targetIndex);

        Sequence sequence = DOTween.Sequence();

        sequence.Join(currentRect.DOAnchorPosX(-enterFromX, slideTime));
        sequence.Join(nextRect.DOAnchorPosX(0, slideTime));

        sequence.SetEase(ease);

        sequence.OnComplete(() =>
        {
            currentIndex = targetIndex;

            // Current と Next のスロット一式を入れ替える
            (currentGifPlayer, nextGifPlayer) = (nextGifPlayer, currentGifPlayer);
            (currentOverlayImage, nextOverlayImage) = (nextOverlayImage, currentOverlayImage);
            (currentCanvasGroup, nextCanvasGroup) = (nextCanvasGroup, currentCanvasGroup);
            (currentRect, nextRect) = (nextRect, currentRect);

            // 新しいNext(=元Current)を画面外へ戻して待機
            nextGifPlayer.Stop();
            nextRect.anchoredPosition = new Vector2(enterFromX, 0);
            nextRect.gameObject.SetActive(false);

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

        currentGifPlayer.Stop();
        nextGifPlayer.Stop();

        gameObject.SetActive(false);

        OnTutorialClosed?.Invoke();

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

    public void ResetTutorial()
    {
        DOTween.Kill(currentRect);
        DOTween.Kill(nextRect);

        currentGifPlayer.Stop();
        nextGifPlayer.Stop();

        Initialize();
    }
}