using UnityEngine;

public class RotateImage : MonoBehaviour
{
    [Header("回転速度（度/秒）")]
    [SerializeField] private float rotateSpeed = 100f;

    [Header("時計回りにする場合はチェック")]
    [SerializeField] private bool clockwise = true;

    private void Update()
    {
        // 時計回りの場合はマイナス方向へ回転
        float direction = clockwise ? -1f : 1f;

        // フレームレートに依存しないよう Time.deltaTime を掛けて回転
        transform.Rotate(0f, 0f, direction * rotateSpeed * Time.deltaTime);
    }
}