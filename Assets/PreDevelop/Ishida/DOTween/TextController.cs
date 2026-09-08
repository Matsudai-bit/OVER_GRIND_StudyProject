using DG.Tweening;
using UnityEngine;

public class TextController : MonoBehaviour
{
    [SerializeField]
    UnityEngine.UI.Text text;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.text.DOFade(endValue: 1.0f, duration: 1.0f).SetDelay(3f);
        this.transform.DOScale(Vector2.one * 1.2f, 0.5f).SetDelay(3f);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
