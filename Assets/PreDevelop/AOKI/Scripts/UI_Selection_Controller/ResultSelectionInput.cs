using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

// クラス名をファイル名(ResultSelectionInput)に合わせて修正しました
public class ResultSelectionInput : MonoBehaviour
{
    [Header("Next Stage ")]
    [SerializeField] private RectTransform nextStageVisual;
    [Tooltip("非選択時にどれくらい横へずらしておくか")]
    [SerializeField] private float slideOffset = -50f;

    [Header("Stage Select ")]
    [SerializeField] private RectTransform stageSelectVisual;
    [Tooltip("非選択時の縮小サイズ割合")]
    [SerializeField] private float unselectedScale = 0.8f;

    [Header("アニメーション設定")]
    [SerializeField] private float duration = 0.25f;

    [Header("ループ設定")]
    [SerializeField] private float loopDuration = 0.8f;
    [Tooltip("オレンジが選択中に横にスライドし続ける幅")]
    [SerializeField] private float loopSlideAmount = -10f;
    [Tooltip("白枠が選択中に拡大し続ける割合")]
    [SerializeField] private float loopScaleAmount = 1.05f;

    [Header("Input System 設定")]
    [SerializeField] private InputAction upAction = new InputAction("Up", binding: "<Keyboard>/upArrow");
    [SerializeField] private InputAction downAction = new InputAction("Down", binding: "<Keyboard>/downArrow");
    [SerializeField] private InputAction submitAction = new InputAction("Submit", binding: "<Keyboard>/enter");

    private bool isNextStageSelected = true;

    // 元の座標とサイズを記憶する用
    private float nsOriginalPosX;
    private Vector3 ssOriginalScale;

    private void Awake()
    {
        // スクリプト起動時に、WASDキーやゲームパッドの入力を自動で追加バインドします
        upAction.AddBinding("<Keyboard>/w");
        upAction.AddBinding("<Gamepad>/dpad/up");
        upAction.AddBinding("<Gamepad>/leftStick/up");

        downAction.AddBinding("<Keyboard>/s");
        downAction.AddBinding("<Gamepad>/dpad/down");
        downAction.AddBinding("<Gamepad>/leftStick/down");

        submitAction.AddBinding("<Gamepad>/buttonSouth"); // 決定ボタン (XboxのA, PSの×など)
    }

    private void OnEnable()
    {
        upAction.Enable();
        downAction.Enable();
        submitAction.Enable();
    }

    private void OnDisable()
    {
        upAction.Disable();
        downAction.Disable();
        submitAction.Disable();
    }

    void Start()
    {
        // 最初の位置・サイズを記憶
        if (nextStageVisual != null) nsOriginalPosX = nextStageVisual.anchoredPosition.x;
        if (stageSelectVisual != null) ssOriginalScale = stageSelectVisual.localScale;

        // 起動時はアニメーションなしで即座に初期状態を反映
        UpdateVisuals(true);
    }

    void Update()
    {
        // InputActionを使った入力判定
        if (downAction.WasPressedThisFrame())
        {
            if (isNextStageSelected) SelectStageSelect();
        }
        else if (upAction.WasPressedThisFrame())
        {
            if (!isNextStageSelected) SelectNextStage();
        }

        // 決定キー
        if (submitAction.WasPressedThisFrame())
        {
            if (isNextStageSelected) Debug.Log("Next Stage 決定！");
            else Debug.Log("Stage Select 決定！");
        }
    }

    public void SelectNextStage()
    {
        if (isNextStageSelected) return;
        isNextStageSelected = true;
        UpdateVisuals();
    }

    public void SelectStageSelect()
    {
        if (!isNextStageSelected) return;
        isNextStageSelected = false;
        UpdateVisuals();
    }

    private void UpdateVisuals(bool isInstant = false)
    {
        float t = isInstant ? 0f : duration;


        // オレンジの奴
        if (nextStageVisual != null)
        {
            nextStageVisual.DOKill();

            if (isNextStageSelected)
            {
                nextStageVisual.DOAnchorPosX(nsOriginalPosX, t).SetEase(Ease.OutCubic)
                    .OnComplete(() =>
                    {
                        if (isInstant) return;
                        nextStageVisual.DOAnchorPosX(nsOriginalPosX + loopSlideAmount, loopDuration)
                            .SetEase(Ease.InOutSine)
                            .SetLoops(-1, LoopType.Yoyo);
                    });
            }
            else
            {
                nextStageVisual.DOAnchorPosX(nsOriginalPosX + slideOffset, t).SetEase(Ease.OutCubic);
            }
        }

        // 白の奴
        if (stageSelectVisual != null)
        {
            stageSelectVisual.DOKill();

            if (!isNextStageSelected)
            {
                stageSelectVisual.DOScale(ssOriginalScale, t).SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        if (isInstant) return;
                        stageSelectVisual.DOScale(ssOriginalScale * loopScaleAmount, loopDuration)
                            .SetEase(Ease.InOutSine)
                            .SetLoops(-1, LoopType.Yoyo);
                    });
            }
            else
            {
                stageSelectVisual.DOScale(ssOriginalScale * unselectedScale, t).SetEase(Ease.OutCubic);
            }
        }
    }
}