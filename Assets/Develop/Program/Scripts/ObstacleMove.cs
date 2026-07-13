using UnityEngine;

public class ObstMove : MonoBehaviour
{
    public float speed = 5f;
    int hp = 10;
    public int hit = 1;


    void Update()
    {
        // 下方向に移動
        transform.Translate(Vector3.down * speed * Time.deltaTime);

        // 画面外（下）に出たら削除
        if (transform.position.y < -10f || hp <= 0)
            Destroy(gameObject);
    }

    // 衝突した瞬間
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            Debug.Log("障害物に当たった！");
            
        }
    }
    private void OnDrawGizmos()
    {
        // DebugManagerが存在し、かつGizmo表示が有効なときだけ描画する
        if (DebugManager.Instance == null || !DebugManager.Instance.GizmosActive)
        {
            return;
        }

        // 索敵範囲を半透明の赤い球体で描画
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawCube(transform.position,transform.localScale);

        // 輪郭線を不透明な赤で描画
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }

    public void TakeDamage(int Damage)
    {
        Debug.Log(Damage + "ダメージ受けた");
       hp -= Damage;
    }
}
