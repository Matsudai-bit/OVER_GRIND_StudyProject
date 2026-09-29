using System;
using UnityEngine;

/// <summary>
/// S1P3ドレット攻撃で使用するオイルの飛行を制御します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P3OilController :
    MonoBehaviour
{
    // 着地点探索の最大試行回数
    private const int LANDING_POSITION_SEARCH_COUNT = 8;

    /// <summary>
    /// オイルの飛行状態です。
    /// </summary>
    private enum OilFlightState
    {
        NONE,
        ASCENDING,
        MOVING_OVER_LANDING_POINT,
        FALLING,
        LANDED,
        FAILED
    }

    // 攻撃判定
    [SerializeField, Header("参照")]
    private AttackHitbox m_attackHitbox;

    // Player
    private Transform m_playerTransform;

    // Stateから渡されたパラメータ
    private S1P3BossDreadAttackStateParameters m_parameters;

    // 着地点予告Prefab
    private TargetDecalController m_targetDecalPrefab;

    // 着弾痕Prefab
    private S1P3OilDecalController m_oilDecalPrefab;

    // 生成した着地点予告
    private TargetDecalController m_targetDecal;

    // 地面Layer
    private LayerMask m_groundLayerMask;

    // 攻撃ダメージ
    private int m_damage;

    // 現在の飛行状態
    private OilFlightState m_currentState;

    // 現在の移動開始地点
    private Vector3 m_moveStartPosition;

    // 現在の移動目標地点
    private Vector3 m_moveTargetPosition;

    // 最終的な着地点
    private Vector3 m_landingPosition;

    // 現在状態の経過時間
    private float m_elapsedTime;

    // 初期化済みか
    private bool m_isInitialized;

    /// <summary>
    /// オイルが着弾したときに通知します。
    /// </summary>
    public event Action<S1P3OilController> Landed;

    /// <summary>
    /// オイルの飛行に失敗したときに通知します。
    /// </summary>
    public event Action<S1P3OilController> FlightFailed;

    /// <summary>
    /// 必要な参照を取得します。
    /// </summary>
    private void Awake()
    {
        if (m_attackHitbox == null)
        {
            m_attackHitbox =
                GetComponentInChildren<AttackHitbox>(
                    true);
        }
    }

    /// <summary>
    /// オイルを初期化します。
    /// </summary>
    /// <param name="playerTransform">Player。</param>
    /// <param name="parameters">ドレット攻撃パラメータ。</param>
    /// <param name="groundLayerMask">地面Layer。</param>
    /// <param name="damage">攻撃ダメージ。</param>
    /// <param name="targetDecalPrefab">着地点予告Prefab。</param>
    /// <param name="oilDecalPrefab">着弾痕Prefab。</param>
    /// <returns>
    /// true：初期化しました。
    /// false：必要な設定が不足しています。
    /// </returns>
    public bool Initialize(
        Transform playerTransform,
        S1P3BossDreadAttackStateParameters parameters,
        LayerMask groundLayerMask,
        int damage,
        TargetDecalController targetDecalPrefab,
        S1P3OilDecalController oilDecalPrefab)
    {
        if (playerTransform == null)
        {
            Debug.LogError(
                "Playerが設定されていません。",
                this);

            return false;
        }

        if (parameters == null)
        {
            Debug.LogError(
                "ドレット攻撃パラメータが設定されていません。",
                this);

            return false;
        }

        if (m_attackHitbox == null)
        {
            Debug.LogError(
                $"{nameof(AttackHitbox)}が見つかりません。",
                this);

            return false;
        }

        if (targetDecalPrefab == null)
        {
            Debug.LogError(
                $"{nameof(TargetDecalController)}のPrefabが" +
                "設定されていません。",
                this);

            return false;
        }

        if (oilDecalPrefab == null)
        {
            Debug.LogError(
                $"{nameof(S1P3OilDecalController)}のPrefabが" +
                "設定されていません。",
                this);

            return false;
        }

        m_playerTransform =
            playerTransform;

        m_parameters =
            parameters;

        m_groundLayerMask =
            groundLayerMask;

        m_damage =
            Mathf.Max(
                0,
                damage);

        m_targetDecalPrefab =
            targetDecalPrefab;

        m_oilDecalPrefab =
            oilDecalPrefab;

        m_currentState =
            OilFlightState.NONE;

        m_isInitialized =
            true;

        return true;
    }

    /// <summary>
    /// オイルを発射します。
    /// </summary>
    public void Launch()
    {
        if (!m_isInitialized)
        {
            Debug.LogError(
                "オイルが初期化されていません。",
                this);

            return;
        }

        if (m_currentState !=
            OilFlightState.NONE)
        {
            return;
        }

        m_elapsedTime =
            0.0f;

        m_moveStartPosition =
            transform.position;

        m_moveTargetPosition =
            m_moveStartPosition +
            Vector3.up *
            m_parameters.FlightHeight;

        m_currentState =
            OilFlightState.ASCENDING;

        // 発射から着弾まで攻撃判定を有効にする
        m_attackHitbox.EnableHitbox(
            m_damage);
    }

    /// <summary>
    /// オイルの飛行を更新します。
    /// </summary>
    private void Update()
    {
        switch (m_currentState)
        {
            case OilFlightState.ASCENDING:
                UpdateAscending(
                    Time.deltaTime);
                break;

            case OilFlightState.MOVING_OVER_LANDING_POINT:
                UpdateMovingOverLandingPoint(
                    Time.deltaTime);
                break;

            case OilFlightState.FALLING:
                UpdateFalling(
                    Time.deltaTime);
                break;
        }
    }

    /// <summary>
    /// 上昇を更新します。
    /// </summary>
    /// <param name="deltaTime">経過時間。</param>
    private void UpdateAscending(
        float deltaTime)
    {
        if (!UpdateLinearMovement(
                deltaTime,
                m_parameters.AscentDuration))
        {
            return;
        }

        /*
         * このタイミングでPlayer位置を取得します。
         * 以降Playerが移動しても着地点は変更しません。
         */
        if (!TryDetermineLandingPosition(
                out m_landingPosition))
        {
            FailFlight();

            return;
        }

        CreateTargetDecal();

        StartMovingOverLandingPoint();
    }

    /// <summary>
    /// 着地点上空への移動を開始します。
    /// </summary>
    private void StartMovingOverLandingPoint()
    {
        m_elapsedTime =
            0.0f;

        m_moveStartPosition =
            transform.position;

        m_moveTargetPosition =
            m_landingPosition +
            Vector3.up *
            m_parameters.FlightHeight;

        m_currentState =
            OilFlightState.MOVING_OVER_LANDING_POINT;
    }

    /// <summary>
    /// 着地点上空への移動を更新します。
    /// </summary>
    /// <param name="deltaTime">経過時間。</param>
    private void UpdateMovingOverLandingPoint(
        float deltaTime)
    {
        if (!UpdateLinearMovement(
                deltaTime,
                m_parameters.HorizontalMoveDuration))
        {
            return;
        }

        StartFalling();
    }

    /// <summary>
    /// 落下を開始します。
    /// </summary>
    private void StartFalling()
    {
        m_elapsedTime =
            0.0f;

        m_moveStartPosition =
            transform.position;

        m_moveTargetPosition =
            m_landingPosition;

        m_currentState =
            OilFlightState.FALLING;
    }

    /// <summary>
    /// 落下を更新します。
    /// </summary>
    /// <param name="deltaTime">経過時間。</param>
    private void UpdateFalling(
        float deltaTime)
    {
        if (!UpdateLinearMovement(
                deltaTime,
                m_parameters.FallDuration))
        {
            return;
        }

        Land();
    }

    /// <summary>
    /// 現在の移動開始地点から目標地点まで線形移動します。
    /// </summary>
    /// <param name="deltaTime">経過時間。</param>
    /// <param name="duration">移動時間。</param>
    /// <returns>
    /// true：目標地点へ到達しました。
    /// false：移動中です。
    /// </returns>
    private bool UpdateLinearMovement(
        float deltaTime,
        float duration)
    {
        m_elapsedTime +=
            deltaTime;

        float progress;

        if (duration <= Mathf.Epsilon)
        {
            progress =
                1.0f;
        }
        else
        {
            progress =
                Mathf.Clamp01(
                    m_elapsedTime /
                    duration);
        }

        transform.position =
            Vector3.Lerp(
                m_moveStartPosition,
                m_moveTargetPosition,
                progress);

        return progress >= 1.0f;
    }

    /// <summary>
    /// 現在のPlayer位置を基準にランダムな着地点を決定します。
    /// </summary>
    /// <param name="landingPosition">決定した着地点。</param>
    /// <returns>
    /// true：着地点を決定しました。
    /// false：有効な地面が見つかりませんでした。
    /// </returns>
    private bool TryDetermineLandingPosition(
        out Vector3 landingPosition)
    {
        landingPosition =
            Vector3.zero;

        Vector3 playerPosition =
            m_playerTransform.position;

        for (int i = 0;
             i < LANDING_POSITION_SEARCH_COUNT;
             i++)
        {
            Vector2 randomOffset =
                UnityEngine.Random.insideUnitCircle *
                m_parameters.LandingRadius;

            Vector3 candidatePosition =
                new(
                    playerPosition.x +
                    randomOffset.x,
                    playerPosition.y,
                    playerPosition.z +
                    randomOffset.y);

            if (TryGetGroundPosition(
                    candidatePosition,
                    out landingPosition))
            {
                return true;
            }
        }

        // ランダム位置で取得できなかった場合は
        // Player直下を最後に確認する
        return TryGetGroundPosition(
            playerPosition,
            out landingPosition);
    }

    /// <summary>
    /// 指定したXZ位置から地面座標を取得します。
    /// </summary>
    /// <param name="targetPosition">探索するXZ位置。</param>
    /// <param name="groundPosition">取得した地面位置。</param>
    /// <returns>
    /// true：地面を取得しました。
    /// false：地面を取得できませんでした。
    /// </returns>
    private bool TryGetGroundPosition(
        Vector3 targetPosition,
        out Vector3 groundPosition)
    {
        Vector3 rayOrigin =
            targetPosition +
            Vector3.up *
            m_parameters.GroundRaycastStartHeight;

        if (Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                m_parameters.GroundRaycastDistance,
                m_groundLayerMask,
                QueryTriggerInteraction.Ignore))
        {
            groundPosition =
                hit.point;

            return true;
        }

        groundPosition =
            Vector3.zero;

        return false;
    }

    /// <summary>
    /// 着地点予告Decalを生成します。
    /// </summary>
    private void CreateTargetDecal()
    {
        if (m_targetDecalPrefab == null)
        {
            return;
        }

        m_targetDecal =
            Instantiate(
                m_targetDecalPrefab,
                m_landingPosition,
                m_targetDecalPrefab.transform.rotation);

        m_targetDecal.Show(
            transform,
            m_landingPosition,
            m_parameters.TargetDecalMinScale,
            m_parameters.TargetDecalMaxScale,
            m_parameters.TargetDecalMinAlpha,
            m_parameters.TargetDecalMaxAlpha,
            m_parameters.DecalGroundOffset);
    }

    /// <summary>
    /// 着地点予告Decalを削除します。
    /// </summary>
    private void RemoveTargetDecal()
    {
        if (m_targetDecal == null)
        {
            return;
        }

        m_targetDecal.Hide();

        Destroy(
            m_targetDecal.gameObject);

        m_targetDecal =
            null;
    }

    /// <summary>
    /// 着弾痕Decalを生成します。
    /// </summary>
    private void CreateOilDecal()
    {
        if (m_oilDecalPrefab == null)
        {
            return;
        }

        S1P3OilDecalController oilDecal =
            Instantiate(
                m_oilDecalPrefab,
                m_landingPosition,
                m_oilDecalPrefab.transform.rotation);

        oilDecal.Show(
            m_landingPosition,
            m_parameters.OilDecalDuration,
            m_parameters.DecalGroundOffset);

        /*
         * OilDecalControllerが表示時間経過後に非表示にします。
         * 非表示後のGameObjectをシーンへ残さないため、
         * 表示終了後に破棄します。
         */
        Destroy(
            oilDecal.gameObject,
            m_parameters.OilDecalDuration);
    }

    /// <summary>
    /// オイルを着弾状態にします。
    /// </summary>
    private void Land()
    {
        if (m_currentState ==
            OilFlightState.LANDED)
        {
            return;
        }

        m_currentState =
            OilFlightState.LANDED;

        transform.position =
            m_landingPosition;

        // 着地点予告を削除する
        RemoveTargetDecal();

        // 攻撃判定を終了する
        m_attackHitbox.DisableHitbox();

        // 着弾痕を生成する
        CreateOilDecal();

        /*
         * Stateへ着弾を通知します。
         * Stateは全オイルの着弾数を監視します。
         */
        Landed?.Invoke(
            this);

        /*
         * 着弾痕は独立したGameObjectなので、
         * 飛翔物本体はここで破棄できます。
         */
        Destroy(
            gameObject);
    }

    /// <summary>
    /// オイルの飛行をキャンセルします。
    /// </summary>
    public void Cancel()
    {
        if (m_currentState ==
                OilFlightState.LANDED ||
            m_currentState ==
                OilFlightState.FAILED)
        {
            return;
        }

        m_currentState =
            OilFlightState.FAILED;

        RemoveTargetDecal();

        if (m_attackHitbox != null)
        {
            m_attackHitbox.DisableHitbox();
        }

        Destroy(
            gameObject);
    }

    /// <summary>
    /// オイルの飛行失敗を処理します。
    /// </summary>
    private void FailFlight()
    {
        if (m_currentState ==
            OilFlightState.FAILED)
        {
            return;
        }

        Debug.LogError(
            "オイルの着地点を取得できませんでした。",
            this);

        m_currentState =
            OilFlightState.FAILED;

        RemoveTargetDecal();

        if (m_attackHitbox != null)
        {
            m_attackHitbox.DisableHitbox();
        }

        FlightFailed?.Invoke(
            this);

        Destroy(
            gameObject);
    }
}