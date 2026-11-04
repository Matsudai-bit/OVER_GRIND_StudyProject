using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalLocalPos;
    private Coroutine shakeCoroutine;

    private float currentDuration;
    private float currentMagnitude;
    private float elapsedTime;

    private void Awake()
    {
        // 初期ローカル位置を記録
        originalLocalPos = transform.localPosition;
    }

    /// <summary>
    /// カメラ振動を実行します
    /// </summary>
    /// <param name="duration">振動の秒数</param>
    /// <param name="magnitude">振動の振れ幅（強度）</param>
    /// <param name="overwrite">true: 上書きしてリセット / false: 加算して時間延長</param>
    public void Shake(float duration, float magnitude, bool overwrite = false)
    {
        if (overwrite || shakeCoroutine == null)
        {
            // 既存のコルーチンを停止して上書き開始
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }
            shakeCoroutine = StartCoroutine(DoShake(duration, magnitude));
        }
        else
        {
            // 上書きしない場合：振れ幅を加算し、残り時間を延長
            currentMagnitude += magnitude;
            currentDuration = Mathf.Max(currentDuration, elapsedTime + duration);
        }
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        currentDuration = duration;
        currentMagnitude = magnitude;
        elapsedTime = 0f;

        while (elapsedTime < currentDuration)
        {
            // 時間経過に伴う減衰率（1.0 -> 0.0）
            float damper = 1.0f - (elapsedTime / currentDuration);

            // 球体内のランダムな方向へ振れ幅を適用
            Vector3 offset = Random.insideUnitSphere * (currentMagnitude * damper);
            transform.localPosition = originalLocalPos + offset;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 振動終了後に元の位置へ復帰
        transform.localPosition = originalLocalPos;
        shakeCoroutine = null;
    }
}