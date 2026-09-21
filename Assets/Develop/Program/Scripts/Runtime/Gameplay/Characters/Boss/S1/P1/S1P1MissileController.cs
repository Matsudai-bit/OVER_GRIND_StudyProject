using System.Collections;
using UnityEngine;

public class S1P1MissileController : MonoBehaviour
{
    // 追尾ターゲット
    [SerializeField, Header("追尾対象")]
    private Transform m_target;

    // 追尾ターゲット
    [SerializeField, Header("速さ")]
    private float m_speed;

    [SerializeField, Header("捕捉する間隔")]
    float m_time;

    [SerializeField, Header("重力量")]
    float m_gravity;

    private Rigidbody m_rb;

    private bool m_targetting;

    private void Awake()
    {
        m_rb = GetComponent<Rigidbody>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        // 重力を無効化する
        m_rb.useGravity = false;
        m_targetting = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        //m_rb.linearVelocity = new (m_rb.linearVelocity.x, m_gravity, m_rb.linearVelocity.z);

        if (m_targetting)
        {
            // プレイヤーへの向き
            Vector3 toPlayerDirection = (m_target.position - transform.position).normalized;

            Vector3 velocity = toPlayerDirection * m_speed;

            m_rb.linearVelocity = velocity;
            StartCoroutine(TimeCounter(m_time));

            if (DebugManager.Instance.isDebugEnabled)
            {
                Debug.DrawRay(transform.position, toPlayerDirection);
            }

           
        }
    }

    IEnumerator TimeCounter(float waitTime)
    {
        m_targetting = false;
        yield return new WaitForSeconds(waitTime); // Sample2()の処理は1秒待機
        m_targetting = true;
        yield break;
    }
}
