using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PauseEffectManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup detailText;
    [SerializeField] private RectTransform detailTextTransform;
    [SerializeField] private Image backgroundImage;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private　void StartEffect()
    {
        Sequence sequence = DOTween.Sequence();
        // 1. 背景の透明度を変更
        sequence.Append(backgroundImage.DOFade(0.85f, 0.5f));

        // 2. 次にパネルを元の大きさに戻す（0.2秒）
        sequence.Append(detailText.DOFade(1.0f, 0.5f));
        sequence.Join(detailTextTransform.DOAnchorPosX(0.0f, 0.5f));

        // 3. 最後にCanvasGroupをフェードアウトする（0.5秒）
       // sequence.Append(pauseCanvasGroup.DOFade(0f, 0.5f));

        // ※ポーズ中で Time.timeScale = 0 の場合はこれも追加する
        sequence.SetUpdate(true);
    }

 
}
