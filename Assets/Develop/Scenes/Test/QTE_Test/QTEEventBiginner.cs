using UnityEngine;

public class QTEEventBiginner : MonoBehaviour
{
    private void Start()
    {
        // デバッグ：イベントを発生させる
        QTEEventManager.Instance.BeginQTEEvent("EscapeAction");
    }
}
