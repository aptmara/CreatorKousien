using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(CharacterController))]
public class PlayerActionController : MonoBehaviour
{
    /// <summary>
    /// プレイヤーの現在のアクション状態を定義します
    /// </summary>
    public enum PlayerState
    {
        FreeMode,   // 通常の地上/空中移動
        OnRail,     // レールに乗っている状態
    }

    [Header("--- State Management ---")]
    [Tooltip("現在のプレイヤーの状態。")]
    [SerializeField] private PlayerState currentState = PlayerState.FreeMode;

    [Header("--- Free Movement Settings ---")]
    [Tooltip("地上での基本移動速度")]
    [SerializeField] private float moveSpeed = 15f;
    [Tooltip("キャラクターが移動方向を向く際の回転スピード(高いほど機敏)")]
    [SerializeField] private float rotationSpeed = 15f;

    [Header("--- Jump & Gravity Settings ---")]
    [Tooltip("ジャンプ力")]
    [SerializeField] private float jumpPower = 12f;
    [Tooltip("下方向への重力加速度")]
    [SerializeField] private float gravityMultipler = 2.5f;
    [Tooltip("落下時の最大速度")]
    [SerializeField] private float maxFallSpeed = -50f;
    [Tooltip("ジャンプ後のクールダウン(2段ジャンプ防止用)")]
    [SerializeField] private float jumpCooldown = 0.2f;

    [Header("--- Rail Action Settings ---")]
    [Tooltip("レール乗車時のプレイヤーの高さ(Y軸)のオフセット")]
    [SerializeField] private float railOffsetY = 1.0f;

    [Tooltip("レール走行中の基本速度")]
    [SerializeField] private float baseRailSpeed = 30f;
    [Tooltip("レール上の最大速度(前入力時)")]
    [SerializeField] private float maxRailSpeed = 50f;
    [Tooltip("レール上の最小速度(後ろ入力時)")]
    [SerializeField] private float minRailSpeed = 10f;
    [Tooltip("レール上の加減速の機敏さ")]
    [SerializeField] private float railAcceleration = 20f;

    [Tooltip("左右入力時の最大傾き角度(度)")]
    [SerializeField] private float maxLeanAngle = 45f;
    [Tooltip("体を傾けるスピード")]
    [SerializeField] private float leanSpeed = 10f;

    [Tooltip("レールから降りた際のジャンプ力")]
    [SerializeField] private float railExitJumpPower = 8f;
    [Tooltip("レール走行中の手動ジャンプ力")]
    [SerializeField] private float railManualJumpPower = 15f;

    [Tooltip("レールから飛び出す際の前方への慣性の強さ(0で真上に飛び、1.0でレールと同速で吹っ飛ぶ)")]
    [SerializeField] private float railExitForwardInertiaRatio = 0.4f;
    [Tooltip("レールからジャンプ・落下後、再びレールに乗れるようになるまでのクールダウン時間(秒)")]
    [SerializeField] private float railReentryCooldown = 0.5f;

    [Header("--- References ---")]
    [Tooltip("メインカメラ")]
    [SerializeField] private Transform mainCameraTransform;

    // 内部コンポーネントと変数
    private CharacterController characterController;
    private Vector3 currentVelocity;
    private float verticalVelocity;

    // Input System 用変数
    private InputAction moveAction;
    private InputAction jumpAction;

    // レール移動用変数
    private SplineContainer currentSpline;
    private float currentSplineTime = 0f;
    private float splineLength = 0f;
    private int splineDirection = 1;
    private float currentRailSpeed;
    private float currentLeanAngle = 0f;
    private float currentCooldownTimer = 0f;
    private float currentJumpCooldownTimer = 0f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (mainCameraTransform == null && Camera.main != null )
        {
            mainCameraTransform = Camera.main.transform;
        }

        moveAction = new InputAction("Move");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");

        // アクションを有効化
        moveAction.Enable();
        jumpAction.Enable();
    }

    private void OnDestroy()
    {
        moveAction?.Dispose();
        jumpAction?.Dispose();
    }

    void Update()
    {
        if (currentCooldownTimer > 0f)
        {
            currentCooldownTimer -= Time.deltaTime;
        }

        if (currentJumpCooldownTimer > 0f) currentJumpCooldownTimer -= Time.deltaTime;

        switch (currentState)
        {
            case PlayerState.FreeMode:
                HandleFreeMovement();
                break;
            case PlayerState.OnRail:
                HandleRailMovement();
                break;
        }
    }

    /// <summary>
    /// 通常のフリー移動(歩行・ジャンプ・重力)の処理
    /// </summary>
    private void HandleFreeMovement()
    {
        // 入力の取得
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        // カメラの向きを基準にした移動ベクトルの計算
        if (inputDirection.magnitude >= 0.1f && mainCameraTransform != null)
        {
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + mainCameraTransform.eulerAngles.y;
            float angle = Mathf.LerpAngle(transform.eulerAngles.y, targetAngle, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            if (characterController.isGrounded)
            {
                currentVelocity = moveDirection * moveSpeed;
            }
            else
            {
                // 空中での移動制御
                Vector3 targetAirVelocity = moveDirection * moveSpeed;
                currentVelocity.x = Mathf.Lerp(currentVelocity.x, targetAirVelocity.x, Time.deltaTime * 2f);
                currentVelocity.z = Mathf.Lerp(currentVelocity.z, targetAirVelocity.z, Time.deltaTime * 2f);
            }
        }
        else
        {
            if (characterController.isGrounded)
            {
                // 地面で入力をやめたらピタッと止まる
                currentVelocity = new Vector3(0f, currentVelocity.y, 0f);
            }
        }

        if (characterController.isGrounded && verticalVelocity <= 0f && currentJumpCooldownTimer <= 0f)
        {
            verticalVelocity = -2f;

            if (jumpAction.WasPressedThisFrame())
            {
                verticalVelocity = jumpPower;
                currentJumpCooldownTimer = jumpCooldown;
            }
        }
        else
        {
            verticalVelocity += Physics.gravity.y * gravityMultipler * Time.deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, maxFallSpeed);
        }

        currentVelocity.y = verticalVelocity;
        characterController.Move(currentVelocity * Time.deltaTime);
    }

    // レールアクション
    // ------------------------------------------------------------

    /// <summary>
    /// 当たり判定(トリガー)に触れた際の処理
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        // フリー移動中に、SplineContainerを持つオブジェクトに触れたら
        if (currentState == PlayerState.FreeMode && currentCooldownTimer <= 0f)
        {
            if (other.TryGetComponent<SplineContainer>(out SplineContainer spline))
            {
                EnterRail(spline);
            }
        }
    }

    /// <summary>
    /// レールへの乗車処理
    /// </summary>
    /// <param name="spline"></param>
    private void EnterRail(SplineContainer spline)
    {
        currentSpline = spline;
        splineLength = currentSpline.CalculateLength();

        // プレイヤーのワールド座標を、レールのローカル座標に変換
        float3 localPos = currentSpline.transform.InverseTransformPoint(transform.position);

        // 接触した地点に最も近いレールの上の位置を計算
        SplineUtility.GetNearestPoint(currentSpline.Spline, localPos, out float3 nearestPoint, out float t);
        currentSplineTime = t;

        // プレイヤーがレールに対して順走か逆走かを判定
        float3 localTangent = SplineUtility.EvaluateTangent(currentSpline.Spline, t);
        Vector3 worldTangent = currentSpline.transform.TransformDirection(localTangent).normalized;
        splineDirection = Vector3.Dot(transform.forward, worldTangent) >= 0 ? 1 : -1;

        // 乗車時に速度とオフセットをリセット
        currentRailSpeed = baseRailSpeed;
        currentLeanAngle = 0f;

        ChangeState(PlayerState.OnRail);
    }

    /// <summary>
    /// レール上の移動処理
    /// </summary>
    private void HandleRailMovement()
    {
        if (currentSpline == null) return;

        // ジャンプ入力の検知
        if (jumpAction.WasPressedThisFrame())
        {
            PerformRailJump();
            return;
        }

        // 1. 入力の取得
        Vector2 moveInput = moveAction.ReadValue<Vector2>();

        // 2. 加減速の処理 (W/Sキー)
        float targetSpeed = baseRailSpeed;
        if (moveInput.y > 0.1f) targetSpeed = maxRailSpeed;         // 加速
        else if (moveInput.y < -0.1f) targetSpeed = minRailSpeed;   // 減速

        // 滑らかに目標速度へ変化させる
        currentRailSpeed = Mathf.MoveTowards(currentRailSpeed, targetSpeed, railAcceleration * Time.deltaTime);

        // 目標の傾き角度
        float targetLean = moveInput.x * maxLeanAngle;
        currentLeanAngle = Mathf.Lerp(currentLeanAngle, targetLean, leanSpeed * Time.deltaTime);

        // 進行度の更新
        currentSplineTime += (currentRailSpeed * splineDirection / splineLength) * Time.deltaTime;

        // レール終端の判定
        if (currentSplineTime > 1f || currentSplineTime < 0f)
        {
            ExitRail();
            return;
        }

        // --- 位置と回転の適用 ---
        float3 localPos = SplineUtility.EvaluatePosition(currentSpline.Spline, currentSplineTime);
        float3 localTangent = SplineUtility.EvaluateTangent(currentSpline.Spline, currentSplineTime);
        float3 localUp = SplineUtility.EvaluateUpVector(currentSpline.Spline, currentSplineTime);

        Vector3 worldPos = currentSpline.transform.TransformPoint(localPos);
        Vector3 worldTangent = currentSpline.transform.TransformDirection(localTangent).normalized;
        Vector3 worldUp = currentSpline.transform.TransformDirection(localUp).normalized;

        if (splineDirection < 0) worldTangent = -worldTangent;

        transform.position = worldPos + (worldUp * railOffsetY);

        if (worldTangent != Vector3.zero)
        {
            Quaternion baseRotation = Quaternion.LookRotation(worldTangent, worldUp);
            Quaternion leanRotation = Quaternion.Euler(0f, 0f, -currentLeanAngle);

            transform.rotation = baseRotation * leanRotation;
        }
    }

    private void PerformRailJump()
    {
        currentSpline = null;
        verticalVelocity = railManualJumpPower;
        // 傾きを元に戻すために、少し上を向かせる
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        currentVelocity = transform.forward * (currentRailSpeed * railExitForwardInertiaRatio);
        currentCooldownTimer = railReentryCooldown;

        currentJumpCooldownTimer = jumpCooldown;
        ChangeState(PlayerState.FreeMode);
    }

    /// <summary>
    /// レールから降りる処理
    /// </summary>
    private void ExitRail()
    {
        currentSpline = null;
        verticalVelocity = railExitJumpPower;
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        currentVelocity = transform.forward * (currentRailSpeed * railExitForwardInertiaRatio);
        currentVelocity = transform.forward * currentRailSpeed;
        ChangeState(PlayerState.FreeMode);
    }

    /// <summary>
    /// 外部からプレイヤーの状態を変更するための公開メソッド
    /// </summary>
    /// <param name="newState"></param>
    public void ChangeState(PlayerState newState)
    {
        currentState = newState;
    }
}
