using UnityEngine;

/// <summary>
/// S1P1ミサイルの飛行と爆発、およびプールへの返却を管理します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PooledMissile))]
[RequireComponent(typeof(MissileMotor))]
[RequireComponent(typeof(MissileSteering))]
[RequireComponent(typeof(MissileHoming))]
[RequireComponent(typeof(MissileExplosion))]
[RequireComponent(typeof(MissileEffectController))]
public sealed class S1P1MissileController : MonoBehaviour
{
    private enum MissileState
    {
        NONE,
        LAUNCH_UPWARD,
        HOMING
    }

    [SerializeField, Header("飛行時間上限（秒）"), Min(0.1f)]
    private float m_maxFlightDuration = 15.0f;

    private PooledMissile m_pooledMissile;
    private MissileMotor m_motor;
    private MissileSteering m_steering;
    private MissileHoming m_homing;
    private MissileExplosion m_explosion;
    private MissileEffectController m_effectController;

    private MissileState m_currentState;
    private AttackIdentifier m_attackIdentifier;
    private float m_upwardDuration;
    private float m_upwardElapsedTime;
    private float m_flightElapsedTime;
    private bool m_isInitialized;
    private bool m_isTerminating;

    private void Awake()
    {
        m_pooledMissile = GetComponent<PooledMissile>();
        m_motor = GetComponent<MissileMotor>();
        m_steering = GetComponent<MissileSteering>();
        m_homing = GetComponent<MissileHoming>();
        m_explosion = GetComponent<MissileExplosion>();
        m_effectController = GetComponent<MissileEffectController>();
    }

    /// <summary>貸出済みミサイルに必要なデータと共有管理者を設定します。</summary>
    /// <returns>true：初期化成功。false：参照不足。</returns>
    public bool Initialize(
        MissileParameterAsset parameterAsset,
        Transform target,
        float upwardDuration,
        AttackIdentifier attackIdentifier,
        EffectEmitter sceneEmitter,
        ExplosionHitboxPool hitboxPool)
    {
        if (parameterAsset == null || target == null || attackIdentifier == null ||
            sceneEmitter == null || hitboxPool == null ||
            m_pooledMissile == null || !m_pooledMissile.IsSpawned ||
            m_motor == null || m_steering == null || m_homing == null ||
            m_explosion == null || m_effectController == null)
        {
            Debug.LogError("[S1P1MissileController] ミサイル初期化の参照が不足しています。", this);
            return false;
        }

        // プールから再取得された際に前回の設定を持ち越しません。
        m_motor.SetParameters(parameterAsset.MotorParameters);
        m_steering.SetParameters(parameterAsset.SteeringParameters);
        m_homing.SetTarget(target);
        m_explosion.SetHitboxPool(hitboxPool);
        m_effectController.SetEmitter(sceneEmitter);
        m_attackIdentifier = attackIdentifier;
        m_upwardDuration = Mathf.Max(0.0f, upwardDuration);
        m_upwardElapsedTime = 0.0f;
        m_flightElapsedTime = 0.0f;
        m_isInitialized = true;
        m_isTerminating = false;
        m_currentState = MissileState.NONE;
        return true;
    }

    /// <summary>発射時の物理移動とエフェクトを開始します。</summary>
    public bool Launch()
    {
        if (!m_isInitialized || m_currentState != MissileState.NONE)
        {
            return false;
        }

        m_upwardElapsedTime = 0.0f;
        m_flightElapsedTime = 0.0f;
        m_steering.ResetSteering();
        m_motor.Launch(Vector3.up);
        m_effectController.PlayLaunch();
        m_currentState = MissileState.LAUNCH_UPWARD;
        return true;
    }

    private void FixedUpdate()
    {
        if (!m_isInitialized || m_isTerminating || m_currentState == MissileState.NONE)
        {
            return;
        }

        m_flightElapsedTime += Time.fixedDeltaTime;
        if (m_flightElapsedTime >= m_maxFlightDuration)
        {
            FinishFlight(false, transform.position);
            return;
        }

        switch (m_currentState)
        {
            case MissileState.LAUNCH_UPWARD:
                UpdateUpwardLaunch();
                break;
            case MissileState.HOMING:
                UpdateHoming();
                break;
        }
    }

    private void UpdateUpwardLaunch()
    {
        m_upwardElapsedTime += Time.fixedDeltaTime;
        if (m_upwardElapsedTime < m_upwardDuration)
        {
            return;
        }
        m_steering.ResetSteering();
        m_currentState = MissileState.HOMING;
    }

    private void UpdateHoming()
    {
        if (!m_homing.HasTarget)
        {
            // 追尾先が消失しても、最大飛行時間で必ず返却します。
            return;
        }

        Vector3 desiredDirection = m_homing.GetDirection(transform.position);
        Vector3 acceleration = m_steering.CalculateSteering(
            desiredDirection, m_motor.CurrentSpeed, m_motor.Velocity);
        m_motor.AddAcceleration(acceleration);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!m_isInitialized || m_isTerminating || collision.contactCount == 0)
        {
            return;
        }
        FinishFlight(true, collision.GetContact(0).point);
    }

    /// <summary>二重爆発を防ぎ、飛行体のみを返却します。</summary>
    private void FinishFlight(bool shouldExplode, Vector3 position)
    {
        if (m_isTerminating || !m_isInitialized)
        {
            return;
        }
        m_isTerminating = true;
        m_effectController.StopFlight();

        if (shouldExplode)
        {
            m_effectController.PlayExplosion(position);
            if (!m_explosion.Explode(position, m_attackIdentifier))
            {
                Debug.LogWarning("[S1P1MissileController] 爆発のHitbox生成に失敗しました。", this);
            }
        }

        m_homing.ClearTarget();
        m_isInitialized = false;
        m_currentState = MissileState.NONE;
        m_pooledMissile.Release();
    }

    private void OnDisable()
    {
        // シーン破棄・外部停止でも飛行演出や参照を持ち越しません。
        m_effectController?.StopFlight();
        m_homing?.ClearTarget();
        m_isInitialized = false;
        m_currentState = MissileState.NONE;
        m_attackIdentifier = null;
    }
}
