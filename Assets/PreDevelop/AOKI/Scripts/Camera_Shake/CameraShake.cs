using System.Collections;
using UnityEngine;

<<<<<<< HEAD
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
=======
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
>>>>>>> origin/InGameRoot
    }

    /// <summary>
    /// カメラ振動を実行します
    /// </summary>
<<<<<<< HEAD
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
=======
    public void Shake(float duration, float magnitude, bool overwrite = false)
    {
        Debug.Log($"Shake実行: 秒数={duration}s, 振れ幅={magnitude}, 上書き={overwrite}");

        if (overwrite || shakeCoroutine == null)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
>>>>>>> origin/InGameRoot
            shakeCoroutine = StartCoroutine(DoShake(duration, magnitude));
        }
        else
        {
<<<<<<< HEAD
            // 上書きしない場合：振れ幅を加算し、残り時間を延長
=======
>>>>>>> origin/InGameRoot
            currentMagnitude += magnitude;
            currentDuration = Mathf.Max(currentDuration, elapsedTime + duration);
        }
    }

<<<<<<< HEAD
=======
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

>>>>>>> origin/InGameRoot
    private IEnumerator DoShake(float duration, float magnitude)
    {
        currentDuration = duration;
        currentMagnitude = magnitude;
        elapsedTime = 0f;

        while (elapsedTime < currentDuration)
        {
<<<<<<< HEAD
            // 時間経過に伴う減衰率（1.0 -> 0.0）
            float damper = 1.0f - (elapsedTime / currentDuration);

            // 球体内のランダムな方向へ振れ幅を適用
            Vector3 offset = Random.insideUnitSphere * (currentMagnitude * damper);
            transform.localPosition = originalLocalPos + offset;
=======
            float damper = 1.0f - (elapsedTime / currentDuration);
            Vector2 randomCircle = Random.insideUnitCircle * (currentMagnitude * damper);

            // 揺れオフセット量を更新
            currentOffset = new Vector3(randomCircle.x, randomCircle.y, 0f);
>>>>>>> origin/InGameRoot

            elapsedTime += Time.deltaTime;
            yield return null;
        }

<<<<<<< HEAD
        // 振動終了後に元の位置へ復帰
        transform.localPosition = originalLocalPos;
        shakeCoroutine = null;
    }
=======
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
>>>>>>> origin/InGameRoot
}