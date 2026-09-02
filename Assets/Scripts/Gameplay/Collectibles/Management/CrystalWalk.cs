using Game.Core.Events;
using Game.Core.Roguelike;
using Game.Gameplay.Collectibles;
using Game.Gameplay.Stage;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
public class CrystalWalk : MonoBehaviour, ICrystalBreakable
{
    private const string FieldWallRootName = "FIELD_WALL";

    // --- 【ここを追加】セグメントごとの移動モード ---
    public enum SegmentMode
    {
        Bezier, // ベジェ曲線（制御点を使用する）
        Linear  // 直線（制御点を無視して真っ直ぐ進む）
    }

    [System.Serializable]
    public class PathSegment
    {
        [Tooltip("この区間の移動モード（直線かベジェ曲線か）")]
        public SegmentMode mode = SegmentMode.Bezier;

        [Tooltip("そこへ向かうためのベジェ制御点（直線モードの場合は未設定でOK）")]
        public Transform controlPosition;

        [Tooltip("目標とする中継地点")]
        public Transform targetPosition;

        [Tooltip("この中継地点に到達したときの待機時間（秒）。0なら止まらず通過。")]
        public float waitTime = 0.0f;
    }

    private sealed class PendingHitStyleEmission
    {
        public readonly Vector3 SpawnPosition;
        public readonly Vector3 Direction;
        public readonly float SpreadAngle;
        public readonly float Power;
        public readonly int TotalCount;
        public readonly float Duration;
        public int EmittedCount;
        public float ElapsedTime;

        public PendingHitStyleEmission(
            Vector3 spawnPosition,
            Vector3 direction,
            float spreadAngle,
            float power,
            int count,
            float duration)
        {
            SpawnPosition = spawnPosition;
            Direction = direction;
            SpreadAngle = spreadAngle;
            Power = power;
            TotalCount = count;
            Duration = duration;
        }
    }

    [Header("移動経路の設定")]
    [Tooltip("スタート地点の座標")]
    public Transform startPosition;

    [Tooltip("スタート地点（始点）に戻ってきたときの待機時間（秒）")]
    [SerializeField] private float _startPositionWaitTime = 1.0f;

    [Tooltip("経由する中継地点と制御点のリスト（何個でも追加可能）")]
    public List<PathSegment> pathSegments = new List<PathSegment>();

    [Tooltip("終点に達した後、自動的に逆再生してスタートに戻るか")]
    [SerializeField] private bool _autoReverseLoop = true;

    private float _currentWaitTime = 0.0f;
    private int _lastSegmentIndex = -1;
    private bool _isReturning = false;
    private bool _isMovementSuspended = false;

    [Header("初期移動状態")]
    [Tooltip("初期移動中だけtrueになります。実行状態の確認用です。")]
    [SerializeField] private bool _isInitialTraversing;

    private int segmentCount = 32;

    [Header("スタート位置(秒)")]
    [SerializeField] public float startCount;
    [Header("一周（あるいは片道）にかかる時間(秒)")]
    [SerializeField] public float maxCount;
    private float _currentCount;

    [Header("ヒットストップ")]
    [SerializeField] private float _hitStop = 0.5f;
    private float _currentHitStop = 0.0f;

    [Header("殴られた時の揺れ")]
    [SerializeField] private Transform _model;
    [SerializeField] private float _shakeAmplitude = 0.1f;
    private Vector3 _modelBaseLocalPos;

    [Header("欠片関連")]
    [SerializeField] private CrystalShardEmitter _emitter;
    [SerializeField] private float _spreadAngle = 25.0f;
    [SerializeField] public int shardBaseCount;
    [SerializeField] public int curShardCount;
    [SerializeField] public float power;
    [SerializeField] private GameObject VFX;
    [SerializeField] private Vector3 _scale;
    [SerializeField] private SceneEventChannel _channel;

    [Header("Shard分割生成")]
    [Tooltip("殴打・敵撃破・コンボによる1回分のShardを、何秒かけて生成するか指定します。")]
    [SerializeField, Min(0.01f)] private float _hitStyleEmissionDurationSeconds = 1f;

    [Header("フィールドの傾き")]
    [SerializeField] private FieldData _fieldData;

    [Header("Collectible射出方向")]
    [Tooltip("通常移動中の生成位置を内側へ寄せる基準です。未設定の場合はステージ中心を使用します。プレイヤー未検出時の射出方向にも使用します。")]
    [FormerlySerializedAs("_fieldCenter")]
    [SerializeField] private Transform _collectibleEmissionTarget;
    [Tooltip("初期移動以外のCollectibleを向けるプレイヤー位置へのワールド座標オフセットです。")]
    [SerializeField] private Vector3 _playerEmissionTargetOffset;
    [SerializeField] private float _emitOffset = 1.5f;
    [SerializeField] private float _upBias = 1.5f;

    [Header("通常移動中のCollectible生成")]
    [Tooltip("通常移動中にCollectibleを自動生成するかどうかです。")]
    [SerializeField] private bool _emitCollectiblesWhileMoving = true;

    [Tooltip("通常移動中に1秒あたり生成するCollectibleの数です。")]
    [SerializeField, Min(0f)] private float _movementCollectiblesPerSecond = 6f;

    [Tooltip("初期移動中・通常移動中に生成するCollectibleの射出力です。")]
    [SerializeField, Min(0f)] private float _movementEmissionPower = 500f;

    [Tooltip("初期移動中・通常移動中の生成位置を、射出先へ向けて内側にずらす距離です。")]
    [SerializeField, Min(0f)] private float _movementEmissionInwardOffset = 2f;

    private float _movementCollectibleEmissionAccumulator;
    private Transform _playerEmissionTarget;
    private Transform _fieldWallRoot;
    private readonly Queue<PendingHitStyleEmission> _pendingHitStyleEmissions = new();

    private Vector3 CollectibleEmissionTargetPosition => _collectibleEmissionTarget != null
        ? _collectibleEmissionTarget.position
        : FieldContext.IsReady ? FieldContext.Center : Vector3.zero;

    private Vector3 PlayerEmissionTargetPosition
    {
        get
        {
            ResolvePlayerEmissionTargetIfNeeded();
            return _playerEmissionTarget != null
                ? _playerEmissionTarget.position + _playerEmissionTargetOffset
                : CollectibleEmissionTargetPosition;
        }
    }

    public void Break(Vector3 hitPoint, Vector3 hitDirection) => Emits(hitPoint);

    public bool IsInitialTraversing => _isInitialTraversing;

    public void SetInitialTraversing(bool isInitialTraversing)
    {
        _isInitialTraversing = isInitialTraversing;
    }

    /// <summary>
    /// クリスタル本体の移動だけを一時停止または再開します。
    /// Break や Emits、初期化、イベント購読は停止しません。
    /// </summary>
    /// <param name="isSuspended">true の場合は移動を停止し、false の場合は移動を再開します。</param>
    public void SetMovementSuspended(bool isSuspended)
    {
        _isMovementSuspended = isSuspended;
    }

    /// <summary>
    /// 通常移動が最初のフレームで使用するワールド座標を取得します。
    /// </summary>
    public bool TryGetNormalMovementStartPosition(out Vector3 worldPosition)
    {
        worldPosition = transform.position;
        if (startPosition == null || pathSegments == null || pathSegments.Count == 0
            || maxCount <= 0f || float.IsNaN(maxCount) || float.IsInfinity(maxCount))
        {
            return false;
        }

        for (int index = 0; index < pathSegments.Count; index++)
        {
            if (pathSegments[index] == null || pathSegments[index].targetPosition == null)
            {
                return false;
            }
        }

        float totalT = Mathf.Clamp01(startCount / maxCount);
        if (_autoReverseLoop)
        {
            totalT *= 2f;
            if (totalT > 1f)
            {
                totalT = 2f - totalT;
            }
        }

        worldPosition = FieldRotation * EvaluatePath(totalT);
        return !(float.IsNaN(worldPosition.x) || float.IsInfinity(worldPosition.x)
            || float.IsNaN(worldPosition.y) || float.IsInfinity(worldPosition.y)
            || float.IsNaN(worldPosition.z) || float.IsInfinity(worldPosition.z));
    }

    private Quaternion FieldRotation
    {
        get
        {
            if (_fieldData != null) return Quaternion.Euler(_fieldData.FieldTilt, 0f, 0f);
            if (FieldContext.IsReady) return FieldContext.Rotation;
            return Quaternion.identity;
        }
    }

    void Start()
    {
        ResolvePlayerEmissionTargetIfNeeded();
        _currentCount = startCount;
        if (_scale.x <= 0.0f || _scale.y <= 0.0f || _scale.z <= 0.0f) _scale = Vector3.one;
        if (_model != null) _modelBaseLocalPos = _model.localPosition;
        curShardCount = shardBaseCount;

        float initT = Mathf.Clamp01(_currentCount / maxCount);
        if (_autoReverseLoop)
        {
            initT *= 2f;
            _isReturning = initT > 1f;
            if (_isReturning) initT = 2f - initT;
        }
        float exactSegment = initT * pathSegments.Count;
        _lastSegmentIndex = Mathf.FloorToInt(exactSegment);

        if (_channel != null)
            _channel.OnExecuteFloat += BaseMultiply;
    }

    private void OnDisable()
    {
        if (_channel != null)
            _channel.OnExecuteFloat -= BaseMultiply;
    }

    void Update()
    {
        bool isHitStyleEmissionActive = _emitter != null
            && _pendingHitStyleEmissions.Count > 0;
        ProcessPendingHitStyleEmissions();

        if (isHitStyleEmissionActive)
        {
            if (_currentHitStop > 0.0f)
            {
                _currentHitStop = Mathf.Max(0.0f, _currentHitStop - Time.deltaTime);
            }

            UpdateModelShake();
            return;
        }

        if (_isMovementSuspended) return;
        if (pathSegments == null || pathSegments.Count == 0) return;

        // 1. 一時停止（待機）タイマーの処理
        if (_currentWaitTime > 0.0f)
        {
            _currentWaitTime -= Time.deltaTime;
            UpdateModelShake();
            return;
        }

        // 現在の正規化時間 (0.0 ～ 1.0)
        float totalT = Mathf.Clamp01(_currentCount / maxCount);

        bool nextIsReturning = _isReturning;
        if (_autoReverseLoop)
        {
            totalT *= 2f;
            nextIsReturning = totalT > 1f;
            if (nextIsReturning) totalT = 2f - totalT;
        }

        // 現在のセグメントインデックスを計算
        float exactSegment = totalT * pathSegments.Count;
        int currentSegmentIndex = Mathf.FloorToInt(exactSegment);
        if (currentSegmentIndex >= pathSegments.Count) currentSegmentIndex = pathSegments.Count - 1;

        // セグメント（経由地）の切り替わり、または折り返しを検知
        if (_lastSegmentIndex != -1 && (currentSegmentIndex != _lastSegmentIndex || nextIsReturning != _isReturning))
        {
            float targetWaitTime = 0f;

            if (currentSegmentIndex == 0 && !nextIsReturning && _isReturning)
            {
                targetWaitTime = _startPositionWaitTime;
            }
            else if (_isReturning && currentSegmentIndex != _lastSegmentIndex)
            {
                targetWaitTime = pathSegments[currentSegmentIndex].waitTime;
            }
            else
            {
                targetWaitTime = pathSegments[_lastSegmentIndex].waitTime;
            }

            _lastSegmentIndex = currentSegmentIndex;
            _isReturning = nextIsReturning;

            if (targetWaitTime > 0.0f)
            {
                _currentWaitTime = targetWaitTime;
                return;
            }
        }

        _lastSegmentIndex = currentSegmentIndex;
        _isReturning = nextIsReturning;

        // パス上の座標計算
        Vector3 flatPoint = EvaluatePath(totalT);
        Vector3 worldPoint = FieldRotation * flatPoint;
        Vector3 previousPosition = transform.position;
        Vector3 moveDir = worldPoint - previousPosition;
        gameObject.transform.position = worldPoint;
        UpdateMovementCollectibleEmission(previousPosition, worldPoint, Time.deltaTime);

        // 時間進行とヒットストップ処理
        if (_currentHitStop > 0.0f)
        {
            _currentHitStop -= Time.deltaTime;
        }
        else
        {
            _currentCount += Time.deltaTime;
            if (_currentCount >= maxCount) _currentCount = 0.0f;
        }

        if (_currentHitStop <= 0.0f) _currentHitStop = 0.0f;

        Vector3 fieldUp = FieldRotation * Vector3.up;
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(moveDir.normalized, fieldUp);
        }

        UpdateModelShake();
    }

    private void UpdateModelShake()
    {
        if (_model != null)
        {
            if (_currentHitStop > 0f)
            {
                float k = _currentHitStop / Mathf.Max(0.0001f, _hitStop);
                _model.localPosition = _modelBaseLocalPos + UnityEngine.Random.insideUnitSphere * _shakeAmplitude * k;
            }
            else
            {
                _model.localPosition = _modelBaseLocalPos;
            }
        }
    }

    private Vector3 EvaluatePath(float t)
    {
        int totalSegments = pathSegments.Count;
        float exactSegment = t * totalSegments;
        int index = Mathf.FloorToInt(exactSegment);

        if (index >= totalSegments) index = totalSegments - 1;
        float localT = exactSegment - index;

        Vector3 p0 = (index == 0) ? startPosition.position : pathSegments[index - 1].targetPosition.position;
        Vector3 p2 = pathSegments[index].targetPosition.position;

        // --- 【ここを修正】モードに応じて計算を分岐 ---
        if (pathSegments[index].mode == SegmentMode.Linear)
        {
            // 直線モードなら、単に始点と終点を線形補間するだけ（制御点は参照しない）
            return Vector3.Lerp(p0, p2, localT);
        }
        else
        {
            // ベジェモードなら、制御点を使って2次ベジェ計算
            Vector3 p1 = pathSegments[index].controlPosition != null ? pathSegments[index].controlPosition.position : p0;
            return CalculateQuadraticBezierPoint(localT, p0, p1, p2);
        }
    }

    private void OnDrawGizmos()
    {
        if (pathSegments == null || pathSegments.Count == 0 || startPosition == null) return;

        Gizmos.color = Color.green;
        Vector3 previousPoint = FieldRotation * startPosition.position;

        for (int index = 0; index < pathSegments.Count; index++)
        {
            if (pathSegments[index].targetPosition == null) continue;

            Vector3 p0 = (index == 0) ? startPosition.position : pathSegments[index - 1].targetPosition.position;
            Vector3 p2 = pathSegments[index].targetPosition.position;

            // --- 【ここを修正】ギズモ描画もモードに対応 ---
            if (pathSegments[index].mode == SegmentMode.Linear)
            {
                // 直線なら1本の線を描くだけでいいのでループ不要
                Vector3 currentPoint = FieldRotation * p2;
                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
            else
            {
                // ベジェなら細かく分割して曲線を描画
                if (pathSegments[index].controlPosition == null) continue;
                Vector3 p1 = pathSegments[index].controlPosition.position;

                for (int i = 1; i <= segmentCount; i++)
                {
                    float t = (float)i / segmentCount;
                    Vector3 currentPoint = FieldRotation * CalculateQuadraticBezierPoint(t, p0, p1, p2);
                    Gizmos.DrawLine(previousPoint, currentPoint);
                    previousPoint = currentPoint;
                }
            }
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(PlayerEmissionTargetPosition, 0.5f);

        Vector3 launchDirection = CreatePlayerLaunchDirection(transform.position);
        if (launchDirection.sqrMagnitude > 0.0001f)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + launchDirection * 3.0f);
        }
    }

    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1.0f - t;
        return u * u * p0 + 2.0f * u * t * p1 + t * t * p2;
    }

    [ContextMenu("DEBUG: Emit Shards")]
    private void DebugEmit() => Emits(transform.position);

    /// <summary>
    /// 移動経路上の指定位置からCollectibleを1個生成します。
    /// </summary>
    /// <param name="spawnPosition">Collectibleを生成する基準位置です。</param>
    public void EmitMovementCollectible(Vector3 spawnPosition)
    {
        if (_emitter == null)
        {
            return;
        }

        Vector3 adjustedSpawnPosition = CreateMovementEmissionSpawnPosition(spawnPosition);
        Vector3 direction = CreatePlayerLaunchDirection(adjustedSpawnPosition);

        _emitter.EmitFromHit(adjustedSpawnPosition, direction, _spreadAngle, _movementEmissionPower, null);
    }

    /// <summary>
    /// 初期移動中のCollectibleを、指定Targetまたはフィールド下方向へ1個生成します。
    /// </summary>
    /// <param name="spawnPosition">Collectibleを生成する基準位置です。</param>
    /// <param name="emissionTarget">射出先です。未設定の場合はフィールド下方向を使用します。</param>
    public void EmitInitialTraversalCollectible(Vector3 spawnPosition, Transform emissionTarget)
    {
        if (_emitter == null)
        {
            return;
        }

        Vector3 adjustedSpawnPosition = CreateMovementEmissionSpawnPosition(spawnPosition);
        Vector3 direction = CreateInitialTraversalLaunchDirection(adjustedSpawnPosition, emissionTarget);

        _emitter.EmitFromHit(adjustedSpawnPosition, direction, _spreadAngle, _movementEmissionPower, null);
    }

    /// <summary>
    /// 初期移動中のCollectible射出方向を計算します。
    /// </summary>
    /// <param name="spawnPosition">Collectibleの生成位置です。</param>
    /// <param name="emissionTarget">射出先です。未設定の場合はフィールド下方向を使用します。</param>
    /// <returns>初期移動中に使用する正規化済み射出方向です。</returns>
    private Vector3 CreateInitialTraversalLaunchDirection(Vector3 spawnPosition, Transform emissionTarget)
    {
        Vector3 fieldDown = -(FieldRotation * Vector3.up);
        if (emissionTarget == null)
        {
            return fieldDown;
        }

        Vector3 directionToTarget = emissionTarget.position - spawnPosition;
        return directionToTarget.sqrMagnitude > 0.0001f
            ? directionToTarget.normalized
            : fieldDown;
    }

    /// <summary>
    /// 常時生成用の座標を、フィールド面に沿って射出先側へ移動します。
    /// </summary>
    /// <param name="spawnPosition">移動経路上の生成予定座標です。</param>
    /// <returns>射出先を通り越さない範囲で内側へ移動した生成座標です。</returns>
    private Vector3 CreateMovementEmissionSpawnPosition(Vector3 spawnPosition)
    {
        float inwardOffset = Mathf.Max(0f, _movementEmissionInwardOffset);
        if (inwardOffset <= 0f)
        {
            return spawnPosition;
        }

        Vector3 fieldUp = FieldRotation * Vector3.up;
        Vector3 directionToTarget = Vector3.ProjectOnPlane(
            CollectibleEmissionTargetPosition - spawnPosition,
            fieldUp);
        float distanceToTarget = directionToTarget.magnitude;
        if (distanceToTarget <= 0.0001f)
        {
            return spawnPosition;
        }

        float moveDistance = Mathf.Min(inwardOffset, distanceToTarget);
        return spawnPosition + directionToTarget / distanceToTarget * moveDistance;
    }

    /// <summary>
    /// 指定位置からプレイヤー位置とオフセットを加えた地点へ向かう射出方向を計算します。
    /// </summary>
    /// <param name="spawnPosition">Collectibleの生成位置です。</param>
    /// <returns>フィールド面に沿うプレイヤー方向へ上向き補正を加えた正規化済み方向です。</returns>
    private Vector3 CreatePlayerLaunchDirection(Vector3 spawnPosition)
    {
        Vector3 fieldUp = FieldRotation * Vector3.up;
        return CreatePlayerLaunchDirection(spawnPosition, fieldUp);
    }

    private Vector3 CreatePlayerLaunchDirection(Vector3 spawnPosition, Vector3 fieldUp)
    {
        Vector3 directionToPlayer = Vector3.ProjectOnPlane(
            PlayerEmissionTargetPosition - spawnPosition,
            fieldUp);
        Vector3 launchDirection = directionToPlayer + fieldUp * _upBias;

        return launchDirection.sqrMagnitude > 0.0001f
            ? launchDirection.normalized
            : fieldUp;
    }

    private void ResolvePlayerEmissionTargetIfNeeded()
    {
        if (_playerEmissionTarget != null)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerEmissionTarget = player.transform;
        }
    }

    private Vector3 GetCurrentFieldUp()
    {
        ResolveFieldWallRootIfNeeded();
        if (_fieldWallRoot != null)
        {
            return _fieldWallRoot.up;
        }

        if (FieldContext.IsReady)
        {
            return FieldContext.Up;
        }

        return FieldRotation * Vector3.up;
    }

    private void ResolveFieldWallRootIfNeeded()
    {
        if (_fieldWallRoot != null)
        {
            return;
        }

        GameObject fieldWall = GameObject.Find(FieldWallRootName);
        if (fieldWall != null)
        {
            _fieldWallRoot = fieldWall.transform;
        }
    }

    /// <summary>
    /// 通常移動中の経過時間に応じてCollectibleを生成します。
    /// </summary>
    /// <param name="previousPosition">このフレームの移動前座標です。</param>
    /// <param name="currentPosition">このフレームの移動後座標です。</param>
    /// <param name="deltaTime">このフレームの経過時間です。</param>
    private void UpdateMovementCollectibleEmission(Vector3 previousPosition, Vector3 currentPosition, float deltaTime)
    {
        if (!_emitCollectiblesWhileMoving || _movementCollectiblesPerSecond <= 0f)
        {
            _movementCollectibleEmissionAccumulator = 0f;
            return;
        }

        if ((currentPosition - previousPosition).sqrMagnitude <= 0.00000001f)
        {
            return;
        }

        float previousAccumulator = _movementCollectibleEmissionAccumulator;
        float emissionAmount = _movementCollectiblesPerSecond * deltaTime;
        float accumulatedAmount = previousAccumulator + emissionAmount;
        int emissionCount = Mathf.FloorToInt(accumulatedAmount);
        _movementCollectibleEmissionAccumulator = accumulatedAmount - emissionCount;

        for (int index = 0; index < emissionCount; index++)
        {
            float movementTime = (1f - previousAccumulator + index) / emissionAmount;
            Vector3 spawnPosition = Vector3.Lerp(previousPosition, currentPosition, Mathf.Clamp01(movementTime));
            EmitMovementCollectible(spawnPosition);
        }
    }

    /// <summary>
    /// クリスタルを殴ったときと同じ位置・方向・力で、指定数のCollectibleを生成します。
    /// VFXとヒットストップは発生させません。
    /// </summary>
    /// <param name="count">生成するCollectible数です。</param>
    public void EmitHitStyleCollectibles(int count)
    {
        EmitHitStyleCollectibles(transform.position, count, 0f);
    }

    public void EmitInwardHitStyleCollectibles(int count, float inwardOffset)
    {
        EmitHitStyleCollectibles(transform.position, count, inwardOffset);
    }

    public bool EmitPlayerOverheadCollectibles(int count, float heightOffset)
    {
        if (_emitter == null || count <= 0)
        {
            return false;
        }

        ResolvePlayerEmissionTargetIfNeeded();
        if (_playerEmissionTarget == null)
        {
            return false;
        }

        Vector3 fieldUp = GetCurrentFieldUp();
        Vector3 spawnPosition = _playerEmissionTarget.position
            + fieldUp * Mathf.Max(0f, heightOffset);
        Vector3 direction = CreatePlayerLaunchDirection(spawnPosition, fieldUp);

        for (int index = 0; index < count; index++)
        {
            _emitter.EmitFromWorldPosition(
                spawnPosition,
                direction,
                _spreadAngle,
                power,
                null,
                fieldUp);
        }

        return true;
    }

    /// <summary>
    /// 指定したヒット地点を基準に、殴打時設定でCollectibleを生成します。
    /// </summary>
    /// <param name="hitPoint">生成位置補正の基準となるヒット地点です。</param>
    /// <param name="count">生成するCollectible数です。</param>
    private void EmitHitStyleCollectibles(Vector3 hitPoint, int count, float inwardOffset)
    {
        if (_emitter == null || count <= 0)
        {
            return;
        }
        Vector3 outward = hitPoint - transform.position;
        outward = outward.sqrMagnitude > 0.0001f ? outward.normalized : Vector3.up;
        Vector3 spawnPosition = hitPoint + outward * _emitOffset;
        spawnPosition = MoveSpawnPositionTowardPlayer(spawnPosition, inwardOffset);
        Vector3 direction = CreatePlayerLaunchDirection(spawnPosition);

        _pendingHitStyleEmissions.Enqueue(new PendingHitStyleEmission(
            spawnPosition,
            direction,
            _spreadAngle,
            power,
            count,
            Mathf.Max(0.01f, _hitStyleEmissionDurationSeconds)));
    }

    private void ProcessPendingHitStyleEmissions()
    {
        if (_emitter == null || _pendingHitStyleEmissions.Count == 0)
        {
            return;
        }

        float remainingDeltaTime = Time.deltaTime;


        EventBus.Publish(new CrystalHitEvent(0));

        while (remainingDeltaTime > 0f && _pendingHitStyleEmissions.Count > 0)
        {
            PendingHitStyleEmission pendingEmission = _pendingHitStyleEmissions.Peek();
            float remainingDuration = pendingEmission.Duration - pendingEmission.ElapsedTime;
            float consumedTime = Mathf.Min(remainingDeltaTime, remainingDuration);
            pendingEmission.ElapsedTime += consumedTime;
            remainingDeltaTime -= consumedTime;

            float progress = Mathf.Clamp01(
                pendingEmission.ElapsedTime / pendingEmission.Duration);
            int targetEmissionCount = progress >= 1f
                ? pendingEmission.TotalCount
                : Mathf.FloorToInt(pendingEmission.TotalCount * progress);
            int currentEmissionCount = targetEmissionCount - pendingEmission.EmittedCount;

            for (int index = 0; index < currentEmissionCount; index++)
            {
                _emitter.EmitFromHit(
                    pendingEmission.SpawnPosition,
                    pendingEmission.Direction,
                    pendingEmission.SpreadAngle,
                    pendingEmission.Power,
                    null);
            }

            pendingEmission.EmittedCount = targetEmissionCount;

            if (pendingEmission.ElapsedTime >= pendingEmission.Duration)
            {
                _pendingHitStyleEmissions.Dequeue();
            }
            else
            {
                break;
            }
        }
    }

    public void Emits(Vector3 hitPoint)
    {
        if (_isInitialTraversing)
        {
            return;
        }

        int hitDropCount = curShardCount + RoguelikeUpgradeRuntime.AdditionalPumpkinDropCount;
        EmitHitStyleCollectibles(hitPoint, hitDropCount, 0f);
        _currentHitStop = _hitStop;
        InitShardCount();
    }

    private Vector3 MoveSpawnPositionTowardPlayer(Vector3 spawnPosition, float inwardOffset)
    {
        float moveDistance = Mathf.Max(0f, inwardOffset);
        if (moveDistance <= 0f)
        {
            return spawnPosition;
        }

        Vector3 fieldUp = FieldRotation * Vector3.up;
        Vector3 directionToPlayer = Vector3.ProjectOnPlane(
            PlayerEmissionTargetPosition - spawnPosition,
            fieldUp);
        float distanceToPlayer = directionToPlayer.magnitude;
        if (distanceToPlayer <= 0.0001f)
        {
            return spawnPosition;
        }

        moveDistance = Mathf.Min(moveDistance, distanceToPlayer);
        return spawnPosition + directionToPlayer / distanceToPlayer * moveDistance;
    }

    public void PlayEffect(Vector3 position, float size)
    {
        if (VFX == null) return;

        GameObject effect = Instantiate(VFX, position, Quaternion.identity);
        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem particle in particles)
        {
            var main = particle.main;
            main.startSizeMultiplier *= size;
        }
        Destroy(effect, 2.0f);
    }

    public void Multiply(float multiply) => curShardCount = (int)(curShardCount * multiply);
    public void BaseMultiply(float multiply) => curShardCount = (int)(shardBaseCount * multiply);
    public void InitShardCount() => curShardCount = shardBaseCount;
}
