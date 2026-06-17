using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [ SerializeField ]
    GameObject bulletPrefab;
    [ SerializeField ]
    float bulletSpeed = 20f;

    [ Header("Ammo Settings")]
    [SerializeField]
    int maxAmmo = 10;
    [SerializeField]
    int currentAmmo = 0;


    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // スペースキーで発射 (キーボード)
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Shoot();
        }

        // Aボタンで発射 (ゲームパッド)
        // buttonSouth がXboxコントローラーのAボタン（PlayStationなら×ボタン）に該当します
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        // 最大弾数打っている場合は発射しない
        if (currentAmmo == maxAmmo)
        {
            return;
        }

        // マウスの位置を取得
        Vector2 mousePos = Mouse.current.position.ReadValue();

        // カメラからマウス位置へのRayを作成
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        // 発射目標位置
        Vector3 targetPoint;

        // 衝突予測点を取得
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(1000f);
        }

        // プレイヤーから目標位置への方向を計算
        Vector3 direction = (targetPoint - transform.position).normalized;

        // 弾を生成して発射
        GameObject bullet = Instantiate(
            bulletPrefab,
            transform.position,
            Quaternion.LookRotation(direction)
        );

        // 弾に速度を与える
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.linearVelocity = direction * bulletSpeed;

        currentAmmo++;
    
}

    public int GetAmmo()
    {
        return maxAmmo - currentAmmo;
    }
}
