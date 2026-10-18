using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class ImageController : MonoBehaviour
{
    [SerializeField]
    UnityEngine.UI.Image image;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Sequenceのインスタンスを作成
        var sequence = DOTween.Sequence();
        
        this.image.DOFade(endValue: 1.0f,duration: 1.0f).SetDelay(3f);
        this.transform.DOScale(Vector2.one*1.2f, 0.5f).SetDelay(3f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
