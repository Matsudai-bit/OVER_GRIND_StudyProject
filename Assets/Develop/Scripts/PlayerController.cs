using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject bulletPrefab;
    public Transform muzzle;
    public float bulletSpeed = 20f;
    public int maxAmmo = 10;
    public int currentAmmo = 0;


    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Shoot();
        }
    }

    void Shoot()
    {

        if (currentAmmo == maxAmmo)
        {
            return;
        }
        else
        {
            // マウス位置からRayを飛ばす
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            Vector3 targetPoint;

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                // マウスが指している場所
                targetPoint = hit.point;
            }
            else
            {
                // 何もない場合は遠くを狙う
                targetPoint = ray.GetPoint(1000f);
            }

            // 銃口からターゲットへの方向
            Vector3 direction = (targetPoint - muzzle.position).normalized;

            // 弾生成
            GameObject bullet = Instantiate(
                bulletPrefab,
                muzzle.position,
                Quaternion.LookRotation(direction)
            );

            // 発射
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            rb.linearVelocity = direction * bulletSpeed;
            currentAmmo++;
        }
    }
}
