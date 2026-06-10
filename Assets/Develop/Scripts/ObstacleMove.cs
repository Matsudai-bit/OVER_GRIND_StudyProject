using UnityEngine;

public class ObstMove : MonoBehaviour
{
    public float speed = 5f;

    void Update()
    {
        // 下方向に移動
        transform.Translate(Vector3.down * speed * Time.deltaTime);

        // 画面外（下）に出たら削除
        if (transform.position.y < -10f)
            Destroy(gameObject);
    }
}
