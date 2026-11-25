using UnityEngine;

public class S1P2BossTailSlamReference : MonoBehaviour
{
    [SerializeField, Header("尻尾のグラウンド判定センサー")]
    private CollisionSensor m_tailGroundCollisionSensor;

    [SerializeField, Header("衝撃破オブジェクト(設定しなくていい)")]
    private GameObject m_impactEffect;

    public CollisionSensor TailGroundCollisionSensor => m_tailGroundCollisionSensor;

    public GameObject ImpactEffect => m_impactEffect;
}
