using UnityEngine;
using UnityEngine.InputSystem;

namespace Daiki
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.Splines;

    /// <summary>
    /// テスト用プレイヤーコントローラー (Input System対応)
    /// Rigidbodyを使用したWASD移動 + スペースジャンプ
    /// 接地判定は OnCollisionEnter/Stay/Exit + 接触法線で判定
    ///
    /// 【セットアップ】
    /// 1. このスクリプトと PlayerInputActions.inputactions を同じフォルダに配置
    /// 2. .inputactions を選択 → Inspector の "Generate C# Class" にチェック → Apply
    /// 3. PlayerオブジェクトにこのスクリプトとPlayerInputコンポーネントをアタッチ
    /// 4. PlayerInput の Actions に PlayerInputActions を設定
    /// 5. PlayerInput の Behavior を "Invoke Unity Events" に設定
    /// 6. OnMove / OnJump を各Unityイベントに登録
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移動パラメータ")]
        [Tooltip("移動速度 (m/s)")]
        [SerializeField] private float moveSpeed = 5f;

        [Tooltip("加速のなめらかさ (値が小さいほどキビキビ、大きいほどぬめっと動く)")]
        [SerializeField] private float acceleration = 20f;

        [Header("ジャンプパラメータ")]
        [Tooltip("ジャンプ力")]
        [SerializeField] private float jumpForce = 5f;

        [Tooltip("落下時の重力倍率 (大きいほど落下が速くなる)")]
        [SerializeField] private float fallGravityMultiplier = 2.5f;

        [Tooltip("ジャンプボタンを離したときの重力倍率 (短押しジャンプの高さ調整)")]
        [SerializeField] private float lowJumpGravityMultiplier = 2f;

        [Header("接地判定")]
        [Tooltip("「上にいる」とみなす接触法線のY成分の閾値 (0〜1, 大きいほど厳しい)")]
        [SerializeField] private float groundNormalThreshold = 0.6f;

        [Header("カメラ設定")]
        [Tooltip("カメラの向きに合わせて移動するか (falseならワールド軸で移動)")]
        [SerializeField] private bool moveRelativeToCamera = true;

        private SplineAnimate splineAnimate;

        // --- 内部変数 ---
        private Rigidbody rb;
        private Camera mainCamera;

        private Vector2 moveInput;   // Input Systemから受け取る移動入力
        private bool isGrounded;

        // 接地しているオブジェクト数のカウンター
        // OnCollisionEnter/Exit は同フレームに複数発生しうるためカウント方式を採用
        private int groundContactCount;

        // ジャンプ入力バッファ
        private bool jumpPressed;    // started イベントで true になる
        private bool jumpHeld;       // performed 中は true、canceled で false

        // =========================================================
        // Unity ライフサイクル
        // =========================================================

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            mainCamera = Camera.main;

            splineAnimate = GetComponent<SplineAnimate>();
        }

        private void FixedUpdate()
        {
            // isGrounded は Collision コールバックで更新されるため、ここでは参照するだけ
            HandleMovement();
            HandleJump();
            //ApplyGravityModifier();

            if (splineAnimate.IsPlaying)
            {
                
                using var spline = new NativeSpline(splineAnimate.Container.Spline, splineAnimate.Container.transform.localToWorldMatrix);

                SplineUtility.GetNearestPoint(spline, transform.position, out var nearPosition, out var nearT);
                if (0.95f <= nearT)
                {
                    splineAnimate.Pause();
                    rb.AddForce(-SplineUtility.EvaluateAcceleration(spline, nearT) * 1.5f, ForceMode.Impulse);
                }
            }
        }

        // =========================================================
        // 衝突コールバック による接地判定
        // =========================================================

        /// <summary>
        /// 接触開始: 法線のY成分が閾値以上なら「上に乗っている」と判定
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            EvaluateCollision(collision, +1);
        }

        /// <summary>
        /// 接触継続: 接触点が変化した場合に備えて毎フレーム再評価
        /// </summary>
        private void OnCollisionStay(Collision collision)
        {
            EvaluateCollision(collision, 0);
        }

        /// <summary>
        /// 接触終了: カウンターを減らし、0になれば非接地とする
        /// </summary>
        private void OnCollisionExit(Collision collision)
        {
            EvaluateCollision(collision, -1);
        }

        /// <summary>
        /// 接触法線を検査してカウンターを更新し、isGrounded を再計算する
        /// </summary>
        /// <param name="collision">衝突情報</param>
        /// <param name="delta">+1=Enter, 0=Stay, -1=Exit</param>
        private void EvaluateCollision(Collision collision, int delta)
        {
            bool hasGroundNormal = false;

            foreach (ContactPoint contact in collision.contacts)
            {
                // 法線のY成分が閾値以上 = オブジェクトの「上面」に接触している
                if (contact.normal.y >= groundNormalThreshold)
                {
                    hasGroundNormal = true;

                    // レールだった場合
                    if (collision.gameObject.CompareTag("Rail"))
                    {
                        // スプラインアニメーションが動いていれば
                        if (!splineAnimate.IsPlaying)
                        {
                            splineAnimate.Container = collision.gameObject.GetComponent<SplineContainer>();

                            // ネイティブスプラインに変換
                            using var spline = new NativeSpline(splineAnimate.Container.Spline, splineAnimate.Container.transform.localToWorldMatrix);

                            // プレイヤー座標を元にスプライン座標（nearPosition)とノーマライズ座標（nearT)の取得
                            SplineUtility.GetNearestPoint(spline, transform.position, out var nearPosition, out var nearT);

                            // 開始地点の設定
                            splineAnimate.StartOffset = nearT;

                           

                            // 再スタートする
                            splineAnimate.Restart(true);

                            

                            Debug.Log("補間の開始");

                        }
                     
                     

                    }
                    break;
                }
            }

            if (delta == +1 && hasGroundNormal)
            {
                groundContactCount++;
            }
            else if (delta == -1 && hasGroundNormal)
            {
                groundContactCount = Mathf.Max(0, groundContactCount - 1);

                if (collision.gameObject.CompareTag("Rail"))
                {
                

                    splineAnimate.Pause();
                    splineAnimate.Container = null;
                }
            }
            // Stay の場合: カウンターは変更せず isGrounded を現状に合わせるだけ

            isGrounded = groundContactCount > 0;
        }

        // =========================================================
        // Input System コールバック
        // PlayerInput コンポーネントの "Invoke Unity Events" から登録する
        // =========================================================

        /// <summary>Move アクションのコールバック</summary>
        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        /// <summary>Jump アクションのコールバック (started / performed / canceled すべて登録)</summary>
        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                // ボタンを押した瞬間: ジャンプ試行フラグをセット
                jumpPressed = true;
            }

            if (context.performed)
            {
                // ボタンを押し続けている: 長押し判定
                jumpHeld = true;
            }

            if (context.canceled)
            {
                // ボタンを離した
                jumpHeld = false;
            }
        }

        // =========================================================
        // 物理処理 (FixedUpdate から呼ばれる)
        // =========================================================

        /// <summary>WASD移動処理</summary>
        private void HandleMovement()
        {
            Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);

            // カメラ相対移動
            if (moveRelativeToCamera && mainCamera != null)
            {
                Vector3 camForward = mainCamera.transform.forward;
                Vector3 camRight = mainCamera.transform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                input = camForward * moveInput.y + camRight * moveInput.x;
            }

            if (input.magnitude > 1f) input.Normalize();

            Vector3 targetVelocity = input * moveSpeed;

            // 水平速度を滑らかに目標値へ近づける
            Vector3 currentHorizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 newHorizontal = Vector3.MoveTowards(currentHorizontal, targetVelocity, acceleration * Time.fixedDeltaTime);

            rb.linearVelocity = new Vector3(newHorizontal.x, rb.linearVelocity.y, newHorizontal.z);

            // 移動方向を向く
            if (input.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(input);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, acceleration * Time.fixedDeltaTime);
            }
        }

        /// <summary>ジャンプ処理</summary>
        private void HandleJump()
        {
            if (jumpPressed && isGrounded)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
            jumpPressed = false; // バッファリセット (1フレームだけ有効)
        }

        /// <summary>落下・短押しジャンプ用の重力補正</summary>
        private void ApplyGravityModifier()
        {
            if (rb.linearVelocity.y < 0f)
            {
                // 落下中: 重力を強化
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
            }
            else if (rb.linearVelocity.y > 0f && !jumpHeld)
            {
                // 上昇中 + ボタンを離している: 重力を強化して短押しジャンプを実現
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (lowJumpGravityMultiplier - 1f) * Time.fixedDeltaTime;
            }
        }

        // =========================================================
        // ユーティリティ
        // =========================================================

        //private void OnDrawGizmosSelected()
        //{
        //    // 接地状態をギズモで確認 (プレイ中のみ)
        //    if (!Application.isPlaying) return;
        //    Gizmos.color = isGrounded ? Color.green : Color.red;
        //    Gizmos.DrawWireSphere(transform.position, 0.15f);
        //}
    }
}
