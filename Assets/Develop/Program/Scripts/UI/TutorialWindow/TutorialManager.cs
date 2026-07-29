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

    [Header("RectTransform")]
    [SerializeField] private RectTransform currentRect;
    [SerializeField] private RectTransform nextRect;

    [Header("紙芝居アニメーション設定")]
    [SerializeField] private float slideTime = 0.4f;
    [SerializeField] private Ease ease = Ease.OutCubic;
    [Tooltip("後ろで待機しているカードの位置ズレ")]
    [SerializeField] private Vector2 backOffset = new Vector2(0f, -60f);
    [Tooltip("後ろで待機しているカードの縮小率")]
    [SerializeField] private float backScale = 0.85f;
    [Tooltip("後ろで待機しているカードの不透明度")]
    [SerializeField] private float backAlpha = 0.6f;

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

        currentRect.gameObject.SetActive(true);
        currentRect.anchoredPosition = Vector2.zero;
        currentRect.localScale = Vector3.one;
        SetAlpha(currentGifPlayer, 1f);

        nextRect.anchoredPosition = backOffset;
        nextRect.localScale = Vector3.one * backScale;
        SetAlpha(nextGifPlayer, backAlpha);
        nextRect.gameObject.SetActive(false);

        // すでにデコード済みならそのまま再生、未デコードなら全スライドを順にデコードする
        if (slideFramesCache == null || slideFramesCache.Length != tutorialData.Slides.Length)
        {
            slideFramesCache = new List<UniGif.GifTexture>[tutorialData.Slides.Length];
            StartCoroutine(DecodeAllSlidesRoutine());
        }
        else
        {
            PlaySlide(currentGifPlayer, currentIndex);
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
                PlaySlide(currentGifPlayer, currentIndex);
            }
        }
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

    private void SetAlpha(GifPlayer player, float alpha)
    {
        RawImage image = player.RawImage;
        Color color = image.color;
        color.a = alpha;
        image.color = color;
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

        PlayTransition(currentIndex + 1);
    }

    private void PreviousPage()
    {
        if (currentIndex <= 0)
            return;

        PlayTransition(currentIndex - 1);
    }

    /// <summary>
    /// 紙芝居風の前後移動アニメーション。
    /// 表のカードを奥へ、奥で待機していたカードを表へ動かす。
    /// 進む/戻るは同じロジックの向き違いなだけなので共通化している。
    /// </summary>
    private void PlayTransition(int targetIndex)
    {
        isAnimating = true;

        nextRect.gameObject.SetActive(true);
        nextRect.anchoredPosition = backOffset;
        nextRect.localScale = Vector3.one * backScale;
        SetAlpha(nextGifPlayer, backAlpha);

        PlaySlide(nextGifPlayer, targetIndex);

        Sequence sequence = DOTween.Sequence();

        // 表 → 奥(後ろへ下がる)
        sequence.Join(currentRect.DOAnchorPos(backOffset, slideTime));
        sequence.Join(currentRect.DOScale(backScale, slideTime));
        sequence.Join(currentGifPlayer.RawImage.DOFade(backAlpha, slideTime));

        // 奥 → 表(手前に迫り出す)
        sequence.Join(nextRect.DOAnchorPos(Vector2.zero, slideTime));
        sequence.Join(nextRect.DOScale(1f, slideTime));
        sequence.Join(nextGifPlayer.RawImage.DOFade(1f, slideTime));

        sequence.SetEase(ease);

        sequence.OnComplete(() =>
        {
            currentIndex = targetIndex;

            // Current と Next を入れ替える
            (currentGifPlayer, nextGifPlayer) = (nextGifPlayer, currentGifPlayer);
            (currentRect, nextRect) = (nextRect, currentRect);

            // 新しいNext(=元Current)を奥の待機状態に戻して隠す
            nextGifPlayer.Stop();
            nextRect.anchoredPosition = backOffset;
            nextRect.localScale = Vector3.one * backScale;
            SetAlpha(nextGifPlayer, backAlpha);
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