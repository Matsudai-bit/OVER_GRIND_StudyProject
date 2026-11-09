using System.Collections;
using UnityEngine;

// Cinemachine のカメラ位置計算が終わった「一番最後」に実行させるための属性
[DefaultExecutionOrder(9999)]
public class CameraShake : MonoBehaviour
{
    [Header("対象のカメラ")]
    [SerializeField] private Transform cameraTransform;

    private Coroutine shakeCoroutine;
    private float currentDuration;
    private float currentMagnitude;
    private float elapsedTime;
    private Vector3 currentOffset;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    /// <summary>
    /// カメラ振動を実行します
    /// </summary>
    public void Shake(float duration, float magnitude, bool overwrite = false)
    {
        Debug.Log($"Shake実行: 秒数={duration}s, 振れ幅={magnitude}, 上書き={overwrite}");

        if (overwrite || shakeCoroutine == null)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(DoShake(duration, magnitude));
        }
        else
        {
            currentMagnitude += magnitude;
            currentDuration = Mathf.Max(currentDuration, elapsedTime + duration);
        }
    }

    /// <summary>
    /// 振動を即座に強制停止します
    /// </summary>
    public void StopShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }
        currentOffset = Vector3.zero;
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        currentDuration = duration;
        currentMagnitude = magnitude;
        elapsedTime = 0f;

        while (elapsedTime < currentDuration)
        {
            float damper = 1.0f - (elapsedTime / currentDuration);
            Vector2 randomCircle = Random.insideUnitCircle * (currentMagnitude * damper);

            // 揺れオフセット量を更新
            currentOffset = new Vector3(randomCircle.x, randomCircle.y, 0f);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentOffset = Vector3.zero;
        shakeCoroutine = null;
    }

    // CinemachineBrain などの全カメラ追従処理が終わった「後」に呼ばれる
    private void LateUpdate()
    {
        if (currentOffset != Vector3.zero && cameraTransform != null)
        {
            // カメラの視線（向き）に対して上下左右にずらす
            cameraTransform.position += cameraTransform.right * currentOffset.x + cameraTransform.up * currentOffset.y;
        }
    }
}