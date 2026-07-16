using Game.Gameplay.Collectibles;
using Game.Gameplay.Stage;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CrystalWalk : MonoBehaviour, ICrystalBreakable
{
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
    [SerializeField]private SceneEventChannel _channel;

    [Header("フィールドの傾き")]
    [SerializeField] private FieldData _fieldData;

    [Header("欠片のあつまる中心")]
    [SerializeField] private Transform _fieldCenter;
    [SerializeField] private float _emitOffset = 1.5f;
    [SerializeField] private float _upBias = 1.5f;

    private Vector3 FieldCenter => _fieldCenter != null ? _fieldCenter.position : Vector3.zero;

    public void Break(Vector3 hitPoint, Vector3 hitDirection) => Emits(hitPoint);

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

        if(_channel != null)
            _channel.OnExecuteFloat += Multiply;
    }

    private void OnDisable()
    {
        if(_channel != null)
            _channel.OnExecuteFloat -= Multiply;
    }

    void Update()
    {
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
        Vector3 moveDir = worldPoint - transform.position;
        gameObject.transform.position = worldPoint;

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

        if (Keyboard.current.spaceKey.wasPressedThisFrame) Emits(transform.position);

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
        Gizmos.DrawWireSphere(FieldCenter, 0.5f);

        Vector3 toCenter = Vector3.ProjectOnPlane(FieldCenter - transform.position, Vector3.up);
        if (toCenter.sqrMagnitude > 0.0001f)
        {
            Vector3 launchDir = (toCenter.normalized + Vector3.up * _upBias).normalized;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + launchDir * 3.0f);
        }
    }

    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1.0f - t;
        return u * u * p0 + 2.0f * u * t * p1 + t * t * p2;
    }

    [ContextMenu("DEBUG: Emit Shards")]
    private void DebugEmit() => Emits(transform.position);

    public void Emits(Vector3 hitPoint)
    {
        if (_emitter != null)
        {
            Vector3 toCenter = FieldCenter - transform.position;
            Vector3 horizontal = Vector3.ProjectOnPlane(toCenter, Vector3.up);
            horizontal = horizontal.sqrMagnitude > 0.0001f ? horizontal.normalized : Vector3.zero;

            Vector3 dir = (horizontal + Vector3.up * _upBias).normalized;

            Vector3 outward = hitPoint - transform.position;
            outward = outward.sqrMagnitude > 0.0001f ? outward.normalized : Vector3.up;
            Vector3 spawnPos = hitPoint + outward * _emitOffset;

            for (int i = 0; i < curShardCount; i++)
                _emitter.EmitFromHit(spawnPos, dir, _spreadAngle, power, null);
        }
        _currentHitStop = _hitStop;
        InitShardCount();
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
