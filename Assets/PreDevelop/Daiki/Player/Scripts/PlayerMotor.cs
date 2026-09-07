using UnityEngine;

/// <summary>
/// �v���C���[�̕����ړ������s���܂��B
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerMotor : MonoBehaviour
{
    // ���ԃp�����[�^�̍ŏ��l
    private const float MIN_TIME = 0.01f;

    // �����x�N�g���̗L������Ɏg�p����臒l
    private const float DIRECTION_SQR_THRESHOLD = 0.0001f;

    // �J������ړ��Ɏg�p����Transform
    [SerializeField, Header("�ړ��")]
    private Transform m_movementReference;

    // �v���C���[�̕����{�f�B
    private Rigidbody m_playerRigidbody;

    // ���ݎg�p���Ă���ō��ړ����x
    private float m_currentMaxMoveSpeed;

    // ����������Ă��邩
    private bool m_isInitialized;

    /// <summary>
    /// ���ݎg�p���Ă���ō��ړ����x���擾���܂��B
    /// </summary>
    public float MaxMoveSpeed => m_currentMaxMoveSpeed;

    /// <summary>
    /// ����������Ă��邩���擾���܂��B
    /// </summary>
    public bool IsInitialized => m_isInitialized;

    /// <summary>
    /// ���݂̐��������̈ړ������i���K���ς݁j���擾���܂��B
    /// ���x���ق�0�̏ꍇ�͌��݂̐��ʕ�����Ԃ��܂��B
    /// �h���t�g�J�n���ȂǁA���݂̐i�s��������ɂ������ꍇ�Ɏg�p���܂��B
    /// </summary>
    public Vector3 HorizontalDirection
    {
        get
        {
            if (!m_isInitialized ||
                m_playerRigidbody == null)
            {
                return Vector3.forward;
            }

            Vector3 velocity =
                m_playerRigidbody.linearVelocity;

            velocity.y = 0.0f;

            if (velocity.sqrMagnitude <=
                DIRECTION_SQR_THRESHOLD)
            {
                return m_playerRigidbody.transform.forward;
            }

            return velocity.normalized;
        }
    }
    /// ���݂̐������x���擾���܂��B
    /// </summary>
    public float VerticalVelocity =>
        m_isInitialized
            ? m_playerRigidbody.linearVelocity.y
            : 0.0f;

    /// <summary>
    /// PlayerMotor�����������܂��B
    /// </summary>
    /// <param name="playerRigidbody">�v���C���[�̕����{�f�B�B</param>
    public void Initialize(Rigidbody playerRigidbody)
    {
        if (playerRigidbody == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerMotor)}] Rigidbody���w�肳��Ă��܂���B",
                this);

            m_isInitialized = false;
            return;
        }

        m_playerRigidbody = playerRigidbody;
        m_isInitialized = true;

        if (m_playerRigidbody.isKinematic)
        {
            Debug.LogWarning(
                $"[{nameof(PlayerMotor)}] " +
                "Rigidbody��Is Kinematic�ł��B",
                this);
        }
    }

    /// <summary>
    /// �w�肵���p�����[�^�Ńv���C���[���ړ������܂��B
    /// </summary>
    /// <param name="moveInput">�ړ����́B</param>
    /// <param name="parameters">�ړ��p�����[�^�B</param>
    /// <param name="deltaTime">�����X�V�̌o�ߎ��ԁB</param>
    public void Move(
        Vector2 moveInput,
        PlayerMoveParameters parameters,
        float deltaTime)
    {
        if (!m_isInitialized)
        {
            return;
        }

        Vector2 normalizedInput =
            Vector2.ClampMagnitude(moveInput, 1.0f);

        // ���͂��J������̃��[���h�����֕ϊ�
        Vector3 moveDirection =
            CalculateCameraRelativeDirection(normalizedInput);

        float inputMagnitude = normalizedInput.magnitude;

        float maxMoveSpeed =
            Mathf.Max(parameters.MaxMoveSpeed, 0.0f);

        m_currentMaxMoveSpeed = maxMoveSpeed;

        // �ڕW�������x���v�Z
        Vector3 targetHorizontalVelocity =
            moveDirection *
            (maxMoveSpeed * inputMagnitude);

        Vector3 currentHorizontalVelocity =
            GetHorizontalVelocity();

        float acceleration = CalculateAcceleration(
            maxMoveSpeed,
            parameters.TimeToMaxSpeed);

        Vector3 nextHorizontalVelocity =
            Vector3.MoveTowards(
                currentHorizontalVelocity,
                targetHorizontalVelocity,
                acceleration * deltaTime);

        ApplyHorizontalVelocity(nextHorizontalVelocity);

        // �ړ������֏��X�ɉ�]
        RotateTowardsMoveDirection(
            moveDirection,
            parameters.RotationSpeed,
            deltaTime);
    }

    /// <summary>
    /// �w�肵���p�����[�^�Ő������x�����������܂��B
    /// </summary>
    /// <param name="parameters">�ړ��p�����[�^�B</param>
    /// <param name="deltaTime">�����X�V�̌o�ߎ��ԁB</param>
    public void Decelerate(
        PlayerMoveParameters parameters,
        float deltaTime)
    {
        if (!m_isInitialized)
        {
            return;
        }

        float maxMoveSpeed =
            Mathf.Max(parameters.MaxMoveSpeed, 0.0f);

        m_currentMaxMoveSpeed = maxMoveSpeed;

        Vector3 currentHorizontalVelocity =
            GetHorizontalVelocity();

        float deceleration = CalculateAcceleration(
            maxMoveSpeed,
            parameters.TimeToStop);

        Vector3 nextHorizontalVelocity =
            Vector3.MoveTowards(
                currentHorizontalVelocity,
                Vector3.zero,
                deceleration * deltaTime);

        ApplyHorizontalVelocity(nextHorizontalVelocity);
    }

    /// <summary>
    /// �v���C���[�փW�����v�̏�����^���܂��B
    /// </summary>
    /// <param name="jumpPower">�W�����v��(����)�B</param>
    public void Jump(float jumpPower)
    {
        if (!m_isInitialized)
        {
            return;
        }

        Vector3 nextVelocity =
            m_playerRigidbody.linearVelocity;

        nextVelocity.y = jumpPower;

        m_playerRigidbody.linearVelocity =
            nextVelocity;
    }

    /// <summary>
    /// �������x�̒����ƃW�����v�̑����ł��؂�̂��߂̒ǉ��d�͂�K�p���܂��B
    /// </summary>
    /// <param name="parameterAsset">�ړ��p�����[�^�A�Z�b�g�B</param>
    /// <param name="isJumpHeld">�W�����v���͂��p�������ǂ����B</param>
    /// <param name="deltaTime">�����X�V�̌o�ߎ��ԁB</param>
    public void ApplyExtraGravity(
        PlayerMovementParameterAsset parameterAsset,
        bool isJumpHeld,
        float deltaTime)
    {
        if (!m_isInitialized)
        {
            return;
        }

        Vector3 velocity =
            m_playerRigidbody.linearVelocity;

        if (velocity.y < 0.0f)
        {
            // �������͒ǉ��̏d�͂����Z����
            // (Use Gravity�ɂ��1�{���̓G���W�����������ς�)
            velocity.y += Physics.gravity.y *
                (parameterAsset.FallGravityMultiplier - 1.0f) *
                deltaTime;
        }
        else if (velocity.y > 0.0f && !isJumpHeld)
        {
            // �㏸���ɓ��͂𗣂����ꍇ�͒ǉ��̏d�͂����Z����
            velocity.y += Physics.gravity.y *
                (parameterAsset.LowJumpMultiplier - 1.0f) *
                deltaTime;
        }

        nextVelocity.x =
            m_playerRigidbody.linearVelocity.x;
        // velocity.y > 0.0f && isJumpHeld �̏ꍇ��
        // �G���W���̎����d��(1�{)�݂̂�����(�ʏ�̏㏸)

        float maxFallSpeed =
            Mathf.Max(parameterAsset.MaxFallSpeed, 0.0f);

        if (velocity.y < -maxFallSpeed)
        {
            velocity.y = -maxFallSpeed;
        }

        m_playerRigidbody.linearVelocity = velocity;
    }

    /// <summary>
    /// �v���C���[�̐������x�𑦍��ɒ�~���܂��B
    /// </summary>
    public void StopImmediately()
    {
        if (!m_isInitialized)
        {
            return;
        }

        ApplyHorizontalVelocity(Vector3.zero);
    }

    /// <summary>
    /// �ړ����͂��J������̃��[���h�����֕ϊ����܂��B
    /// �h���t�g�����ȂǁA�����v�Z�������O���ł����p�������ꍇ�̂��߂Ɍ��J���Ă��܂��B
    /// </summary>
    /// <param name="moveInput">�ړ����́B</param>
    /// <returns>���[���h��Ԃ̈ړ������B</returns>
    public Vector3 CalculateCameraRelativeDirection(
        Vector2 moveInput)
    {
        Vector3 referenceForward =
            m_movementReference != null
                ? m_movementReference.forward
                : Vector3.forward;

        Vector3 forward = Vector3.ProjectOnPlane(
            referenceForward,
            Vector3.up);

        if (forward.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        Vector3 right = Vector3.Cross(
            Vector3.up,
            forward);

        right.Normalize();

        Vector3 moveDirection =
            (right * moveInput.x) +
            (forward * moveInput.y);

        if (moveDirection.sqrMagnitude > 1.0f)
        {
            moveDirection.Normalize();
        }

        return moveDirection;
    }

    /// <summary>
    /// �ړ������փv���C���[�����X�ɉ�]�����܂��B
    /// </summary>
    /// <param name="moveDirection">�ړ������B</param>
    /// <param name="rotationSpeed">1�b�Ԃ̍ő��]�p�x�B</param>
    /// <param name="deltaTime">�����X�V�̌o�ߎ��ԁB</param>
    private void RotateTowardsMoveDirection(
        Vector3 moveDirection,
        float rotationSpeed,
        float deltaTime)
    {
        if (moveDirection.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            return;
        }

        moveDirection.y = 0.0f;
        moveDirection.Normalize();

        Quaternion targetRotation =
            Quaternion.LookRotation(
                moveDirection,
                Vector3.up);

        Quaternion nextRotation =
            Quaternion.RotateTowards(
                m_playerRigidbody.rotation,
                targetRotation,
                Mathf.Max(rotationSpeed, 0.0f) *
                deltaTime);

        m_playerRigidbody.MoveRotation(nextRotation);
    }

    /// <summary>
    /// ���݂̐������x���擾���܂��B
    /// </summary>
    /// <returns>���݂̐������x�B</returns>
    private Vector3 GetHorizontalVelocity()
    {
        Vector3 horizontalVelocity =
            m_playerRigidbody.linearVelocity;

        horizontalVelocity.y = 0.0f;

        return horizontalVelocity;
    }

    /// <summary>
    /// �������x��Rigidbody�֓K�p���܂��B
    /// </summary>
    /// <param name="horizontalVelocity">�K�p���鐅�����x�B</param>
    private void ApplyHorizontalVelocity(
        Vector3 horizontalVelocity)
    {
        Vector3 nextVelocity = horizontalVelocity;

        nextVelocity.y =
            m_playerRigidbody.linearVelocity.y;

        m_playerRigidbody.linearVelocity =
            nextVelocity;
    }

    /// <summary>
    /// �w�莞�ԂŖڕW���x�֓��B��������x���v�Z���܂��B
    /// </summary>
    /// <param name="targetSpeed">�ڕW���x�B</param>
    /// <param name="requiredTime">���B���ԁB</param>
    /// <returns>�����x�B</returns>
    private float CalculateAcceleration(
        float targetSpeed,
        float requiredTime)
    {
        float safeTime =
            Mathf.Max(requiredTime, MIN_TIME);

        return targetSpeed / safeTime;
    }

    /// <summary>
    /// �v���C���[�̌��݂̐����ړ����x���擾���܂��B
    /// </summary>
    public float HorizontalSpeed
    {
        get
        {
            if (!m_isInitialized ||
                m_playerRigidbody == null)
            {
                return 0.0f;
            }

            Vector3 velocity =
                m_playerRigidbody.linearVelocity;

            velocity.y = 0.0f;

            return velocity.magnitude;
        }
    }

    /// <summary>
    /// �w�肵�����x�ňړ����܂��B
    /// �����E�������s�킸�A�������x�����ɂ��܂��B
    /// </summary>
    /// <param name="moveInput">�ړ����́B</param>
    /// <param name="speed">�ړ����x�B</param>
    /// <param name="rotationSpeed">��]���x�B</param>
    /// <param name="deltaTime">�����X�V���ԁB</param>
    public void MoveAtFixedSpeed(
        Vector2 moveInput,
        float speed,
        float rotationSpeed,
        float deltaTime)
    {
        if (!m_isInitialized)
        {
            return;
        }

        Vector2 normalizedInput =
            Vector2.ClampMagnitude(
                moveInput,
                1.0f);

        Vector3 moveDirection =
            CalculateCameraRelativeDirection(
                normalizedInput);

        float inputMagnitude =
            normalizedInput.magnitude;

        Vector3 targetHorizontalVelocity =
            moveDirection *
            (Mathf.Max(speed, 0.0f) * inputMagnitude);

        ApplyHorizontalVelocity(
            targetHorizontalVelocity);

        RotateTowardsMoveDirection(
            moveDirection,
            rotationSpeed,
            deltaTime);

        m_currentMaxMoveSpeed =
            Mathf.Max(speed, 0.0f);
    }

    /// <summary>
    /// �w�肵�����[���h�����ցA��葬�x�ňړ����܂��B
    /// �X�e�B�b�N���͂�J���������ɂ͉e������܂���B
    /// </summary>
    /// <param name="worldDirection">���[���h��Ԃł̈ړ������B</param>
    /// <param name="speed">�ړ����x�B</param>
    /// <param name="rotationSpeed">��]���x�B</param>
    /// <param name="deltaTime">�����X�V���ԁB</param>
    public void MoveAtFixedWorldDirection(
        Vector3 worldDirection,
        float speed,
        float rotationSpeed,
        float deltaTime)
    {
        if (!m_isInitialized)
        {
            return;
        }

        worldDirection.y = 0.0f;

        if (worldDirection.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            return;
        }

        worldDirection.Normalize();

        Vector3 targetHorizontalVelocity =
            worldDirection *
            Mathf.Max(speed, 0.0f);

        ApplyHorizontalVelocity(
            targetHorizontalVelocity);

        RotateTowardsMoveDirection(
            worldDirection,
            rotationSpeed,
            deltaTime);

        m_currentMaxMoveSpeed =
            Mathf.Max(speed, 0.0f);
    }

    /// <summary>
    /// �h���t�g�̂悤�ɁA�����ڂ̌���(Facing)��
    /// ���ۂ̐i�s����(Velocity)��ʁX�ɐ��䂵�Ȃ���
    /// ��葬�x�ňړ����܂��B
    /// �����ڂ̌����͓��͕����֒ʏ�ʂ�Ǐ]���������A
    /// ���ۂ̐i�s�����͂���Ƃ͓Ɨ�����
    /// �������Ƃ����ω����Ȃ��悤�ɂł��邽�߁A
    /// �u�ԑ̂͋Ȃ��肽�������������Ă���̂�
    /// ���ۂɂ͊O���֊����Ă����v�Ƃ���
    /// �h���t�g�̌����ځE���슴��\���ł��܂��B
    /// </summary>
    /// <param name="velocityDirection">
    /// ���ۂɐi�ޕ����i�����ł������ω��������������j�B
    /// </param>
    /// <param name="speed">�ړ����x�B</param>
    /// <param name="facingDirection">
    /// �����ڂ̌����̖ڕW�����i�ʏ�͓��͕����j�B
    /// </param>
    /// <param name="facingRotationSpeed">
    /// �����ڂ̌�����1�b�Ԃ̍ő��]�p�x�B
    /// </param>
    /// <param name="deltaTime">�����X�V���ԁB</param>
    public void MoveWithDriftAtFixedSpeed(
        Vector3 velocityDirection,
        float speed,
        Vector3 facingDirection,
        float facingRotationSpeed,
        float deltaTime)
    {
        if (!m_isInitialized)
        {
            return;
        }

        Vector3 normalizedVelocityDirection =
            velocityDirection.sqrMagnitude >
                DIRECTION_SQR_THRESHOLD
                ? velocityDirection.normalized
                : Vector3.forward;

        Vector3 targetHorizontalVelocity =
            normalizedVelocityDirection *
            Mathf.Max(speed, 0.0f);

        ApplyHorizontalVelocity(
            targetHorizontalVelocity);

        // �����ڂ̌����́A���ۂ̐i�s�����Ƃ͓Ɨ�����
        // ���͕����֒ʏ�̑��x�ŒǏ]������
        RotateTowardsMoveDirection(
            facingDirection,
            facingRotationSpeed,
            deltaTime);

        m_currentMaxMoveSpeed =
            Mathf.Max(speed, 0.0f);
    }
}