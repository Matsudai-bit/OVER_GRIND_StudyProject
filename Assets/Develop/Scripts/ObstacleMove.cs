using UnityEngine;

public class ObstMove : MonoBehaviour
{
    public float speed = 5f;
  

    void Update()
    {
        // 下方向に移動
        transform.Translate(Vector3.down * speed * Time.deltaTime);

        // 画面外（下）に出たら削除
        if (transform.position.y < -10f )
            Destroy(gameObject);
    }

    // 衝突した瞬間
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            Debug.Log("障害物に当たった！");
            Destroy(gameObject);
        }
    }
}
