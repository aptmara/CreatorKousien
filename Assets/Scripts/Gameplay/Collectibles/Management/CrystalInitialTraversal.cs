using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 初期移動を行った後に CrystalWalk へ制御を引き渡します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CrystalWalk))]
public sealed class CrystalInitialTraversal : MonoBehaviour
{
    /// <summary>
    /// 初期移動を構成する二次ベジェ曲線の1区間です。
    /// </summary>
    [Serializable]
    private sealed class PathSegment
    {
        [Tooltip("この区間の曲がり方を決める制御点です。")]
        public Transform ControlPosition = null;

        [Tooltip("この区間の到達地点です。最後の区間には InitialTarget を設定します。")]
        public Transform TargetPosition = null;
    }

    [Header("開始位置")]
    [Tooltip("初期移動の開始位置です。")]
    [SerializeField] private Transform _origin;

    [Header("ベジェ経路")]
    [Tooltip("制御点と到達地点を移動順に設定します。最後の到達地点は CrystalWalk の startPosition と同じ地点にします。")]
    [SerializeField] private List<PathSegment> _pathSegments = new List<PathSegment>();

    [Header("移動設定")]
    [Tooltip("初期移動にかける時間です。0 以下の場合は即座に終了位置へ移動して CrystalWalk を開始します。")]
    [SerializeField, Min(0f)] private float _duration = 5f;

    [Tooltip("初期移動の補間カーブです。")]
    [SerializeField] private AnimationCurve _traversalCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Collectible生成")]
    [Tooltip("初期移動中のCollectibleを向けるTargetです。未設定の場合はフィールドの下方向へ射出します。")]
    [SerializeField] private Transform _initialEmissionTarget;

    [Tooltip("初期移動1回を通して生成するCollectible数の基礎値です。")]
    [SerializeField, Min(0)] private int _initialTraversalBaseCollectibleCount = 30;

    [Tooltip("初期移動時の基礎値へ掛ける倍率です。")]
    [SerializeField, Min(0f)] private float _initialTraversalCollectibleMultiplier = 1f;

    [Tooltip("初期移動1回で実際に生成される個数です。基礎値と倍率から自動計算されます。")]
    [SerializeField, Min(0)] private int _initialTraversalActualCollectibleCount = 30;

    [Header("実行状態")]
    [Tooltip("初期移動中だけtrueになります。実行状態の確認用です。")]
    [SerializeField] private bool _isInitialTraversing;

    private CrystalWalk _crystalWalk;
    private bool _hasCompleted;
    private bool _hasLoggedInvalidCurveValue;
    private int _emittedCollectibleCount;
    private float _elapsedTime;
    private Vector3 _originPosition;
    private Vector3[] _segmentStartPositions;
    private Vector3[] _segmentControlPositions;
    private Vector3[] _segmentTargetPositions;

    /// <summary>
    /// 現在、初期移動中かどうかを取得します。
    /// </summary>
    public bool IsInitialTraversing => _isInitialTraversing;

    /// <summary>
    /// 初期移動時の生成数倍率を外部から設定します。
    /// </summary>
    /// <param name="multiplier">初期移動時の基礎値へ掛ける倍率です。</param>
    public void SetInitialTraversalCollectibleMultiplier(float multiplier)
    {
        _initialTraversalCollectibleMultiplier = Mathf.Max(0f, multiplier);
        RefreshActualCollectibleCount();
    }

    /// <summary>
    /// Inspector上の設定変更を実生成数へ反映します。
    /// </summary>
    private void OnValidate()
    {
        RefreshActualCollectibleCount();
    }

    /// <summary>
    /// CrystalWalk を取得し、移動だけを停止します。
    /// 座標の検証やキャッシュ、位置変更は Start まで行いません。
    /// </summary>
    private void Awake()
    {
        RefreshActualCollectibleCount();
        _crystalWalk = GetComponent<CrystalWalk>();
        if (_crystalWalk == null)
        {
            Debug.LogError("[CrystalInitialTraversal] CrystalWalk の取得に失敗したため、初期移動を開始できません。", this);
            return;
        }

        _elapsedTime = 0f;
        _isInitialTraversing = false;
        _hasCompleted = false;
        _hasLoggedInvalidCurveValue = false;
        _emittedCollectibleCount = 0;
        _crystalWalk.SetInitialTraversing(false);
        _crystalWalk.SetMovementSuspended(true);
    }

    /// <summary>
    /// FieldBuilder の配置完了後に設定を検証し、初期移動を開始します。
    /// </summary>
    private void Start()
    {
        if (_crystalWalk == null)
        {
            return;
        }

        if (!TryValidateTraversal())
        {
            _hasCompleted = true;
            _crystalWalk.SetInitialTraversing(false);
            _crystalWalk.SetMovementSuspended(false);
            return;
        }

        _elapsedTime = 0f;
        _isInitialTraversing = true;
        _crystalWalk.SetInitialTraversing(true);
        _hasCompleted = false;
        _hasLoggedInvalidCurveValue = false;
        _emittedCollectibleCount = 0;
        transform.position = _originPosition;

        if (_duration <= 0f)
        {
            CompleteTraversal();
            return;
        }

    }

    /// <summary>
    /// 初期移動を更新します。
    /// </summary>
    private void Update()
    {
        if (!_isInitialTraversing || _hasCompleted)
        {
            return;
        }

        _elapsedTime += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(_elapsedTime / _duration);
        float evaluatedTime = EvaluateTraversalCurve(normalizedTime);
        transform.position = EvaluatePath(evaluatedTime);
        EmitCollectiblesForProgress(normalizedTime);

        if (normalizedTime >= 1f)
        {
            CompleteTraversal();
        }
    }

    /// <summary>
    /// 無効化時に途中キャンセルし、移動停止を解除します。
    /// Awake 後 Start 前に無効化された場合も安全に解除します。
    /// </summary>
    private void OnDisable()
    {
        if (!_hasCompleted)
        {
            CancelTraversal();
            return;
        }

        ReleaseMovementSuspension();
    }

    /// <summary>
    /// 破棄時に途中キャンセルし、移動停止を解除します。
    /// Awake 後 Start 前に破棄された場合も安全に解除します。
    /// </summary>
    private void OnDestroy()
    {
        if (!_hasCompleted)
        {
            CancelTraversal();
            return;
        }

        ReleaseMovementSuspension();
    }

    /// <summary>
    /// 初期移動の設定を検証し、ワールド座標をキャッシュします。
    /// </summary>
    /// <returns>初期移動を安全に開始できる場合は true です。</returns>
    private bool TryValidateTraversal()
    {
        if (_origin == null)
        {
            Debug.LogError("[CrystalInitialTraversal] 初期移動の開始位置 Origin が未設定です。移動停止を解除して通常移動へフォールバックします。", this);
            return false;
        }

        if (_pathSegments == null || _pathSegments.Count == 0)
        {
            Debug.LogError("[CrystalInitialTraversal] 初期移動のベジェ経路が未設定です。移動停止を解除して通常移動へフォールバックします。", this);
            return false;
        }

        if (_traversalCurve == null)
        {
            Debug.LogError("[CrystalInitialTraversal] 初期移動の補間カーブが未設定です。移動停止を解除して通常移動へフォールバックします。", this);
            return false;
        }

        _originPosition = _origin.position;
        if (!IsFinite(_originPosition))
        {
            Debug.LogError("[CrystalInitialTraversal] Origin の座標に NaN または Infinity が含まれています。移動停止を解除して通常移動へフォールバックします。", this);
            return false;
        }

        int segmentCount = _pathSegments.Count;
        _segmentStartPositions = new Vector3[segmentCount];
        _segmentControlPositions = new Vector3[segmentCount];
        _segmentTargetPositions = new Vector3[segmentCount];

        if (!_crystalWalk.TryGetNormalMovementStartPosition(out Vector3 normalMovementStartPosition))
        {
            Debug.LogError("[CrystalInitialTraversal] 通常移動の開始位置を取得できません。移動停止を解除して通常移動へフォールバックします。", this);
            return false;
        }

        Vector3 startPosition = _originPosition;
        for (int index = 0; index < segmentCount; index++)
        {
            PathSegment segment = _pathSegments[index];
            if (segment == null || segment.ControlPosition == null || segment.TargetPosition == null)
            {
                Debug.LogError($"[CrystalInitialTraversal] 初期移動の区間 {index} に制御点または到達地点が設定されていません。移動停止を解除して通常移動へフォールバックします。", this);
                return false;
            }

            Vector3 controlPosition = segment.ControlPosition.position;
            Vector3 targetPosition = index == segmentCount - 1
                ? normalMovementStartPosition
                : segment.TargetPosition.position;
            if (!IsFinite(controlPosition) || !IsFinite(targetPosition))
            {
                Debug.LogError($"[CrystalInitialTraversal] 初期移動の区間 {index} に NaN または Infinity を含む座標があります。移動停止を解除して通常移動へフォールバックします。", this);
                return false;
            }

            _segmentStartPositions[index] = startPosition;
            _segmentControlPositions[index] = controlPosition;
            _segmentTargetPositions[index] = targetPosition;
            startPosition = targetPosition;
        }

        if (float.IsNaN(_duration) || float.IsInfinity(_duration))
        {
            Debug.LogError("[CrystalInitialTraversal] 初期移動時間に NaN または Infinity が含まれています。移動停止を解除して通常移動へフォールバックします。", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 初期移動経路上の座標を評価します。
    /// </summary>
    /// <param name="pathTime">経路全体に対する補間係数です。</param>
    /// <returns>評価したワールド座標です。</returns>
    private Vector3 EvaluatePath(float pathTime)
    {
        int segmentCount = _segmentTargetPositions.Length;
        float scaledTime = pathTime * segmentCount;
        int segmentIndex;
        float segmentTime;

        if (scaledTime <= 0f)
        {
            segmentIndex = 0;
            segmentTime = scaledTime;
        }
        else if (scaledTime >= segmentCount)
        {
            segmentIndex = segmentCount - 1;
            segmentTime = scaledTime - segmentIndex;
        }
        else
        {
            segmentIndex = Mathf.FloorToInt(scaledTime);
            segmentTime = scaledTime - segmentIndex;
        }

        return CalculateQuadraticBezierPoint(
            segmentTime,
            _segmentStartPositions[segmentIndex],
            _segmentControlPositions[segmentIndex],
            _segmentTargetPositions[segmentIndex]);
    }

    /// <summary>
    /// 二次ベジェ曲線上の座標を計算します。
    /// </summary>
    /// <param name="time">区間内の補間係数です。</param>
    /// <param name="start">区間の開始地点です。</param>
    /// <param name="control">区間の制御点です。</param>
    /// <param name="target">区間の到達地点です。</param>
    /// <returns>二次ベジェ曲線上のワールド座標です。</returns>
    private static Vector3 CalculateQuadraticBezierPoint(float time, Vector3 start, Vector3 control, Vector3 target)
    {
        float inverseTime = 1f - time;
        return inverseTime * inverseTime * start
            + 2f * inverseTime * time * control
            + time * time * target;
    }

    /// <summary>
    /// 移動進捗に応じて、未生成分のCollectibleを生成します。
    /// </summary>
    /// <param name="normalizedTime">初期移動時間の進捗率です。</param>
    private void EmitCollectiblesForProgress(float normalizedTime)
    {
        int totalCount = _initialTraversalActualCollectibleCount;
        int targetEmittedCount = Mathf.FloorToInt(Mathf.Clamp01(normalizedTime) * totalCount);

        while (_emittedCollectibleCount < targetEmittedCount)
        {
            float collectibleProgress = (float)(_emittedCollectibleCount + 1) / totalCount;
            float collectiblePathTime = EvaluateTraversalCurve(collectibleProgress);
            Vector3 collectiblePosition = EvaluatePath(collectiblePathTime);
            _crystalWalk.EmitInitialTraversalCollectible(collectiblePosition, _initialEmissionTarget);
            _emittedCollectibleCount++;
        }
    }

    /// <summary>
    /// 基礎値と倍率から初期移動時の実生成数を更新します。
    /// </summary>
    private void RefreshActualCollectibleCount()
    {
        _initialTraversalActualCollectibleCount = Mathf.FloorToInt(
            Mathf.Max(0, _initialTraversalBaseCollectibleCount)
            * Mathf.Max(0f, _initialTraversalCollectibleMultiplier));
    }

    /// <summary>
    /// 補間カーブを評価します。
    /// </summary>
    /// <param name="normalizedTime">0 から 1 の補間係数です。</param>
    /// <returns>評価後の補間係数です。</returns>
    private float EvaluateTraversalCurve(float normalizedTime)
    {
        float evaluatedTime = _traversalCurve.Evaluate(normalizedTime);
        if (float.IsNaN(evaluatedTime) || float.IsInfinity(evaluatedTime))
        {
            if (!_hasLoggedInvalidCurveValue)
            {
                Debug.LogError("[CrystalInitialTraversal] 移動カーブの評価結果が NaN または Infinity になったため、線形補間へフォールバックします。", this);
                _hasLoggedInvalidCurveValue = true;
            }

            return normalizedTime;
        }

        return evaluatedTime;
    }

    /// <summary>
    /// 初期移動を完了し、CrystalWalk の移動停止を解除します。
    /// </summary>
    private void CompleteTraversal()
    {
        transform.position = _segmentTargetPositions[_segmentTargetPositions.Length - 1];
        EmitCollectiblesForProgress(1f);
        _isInitialTraversing = false;
        _crystalWalk.SetInitialTraversing(false);
        _hasCompleted = true;
        ReleaseMovementSuspension();
    }

    /// <summary>
    /// Sceneビュー上に初期移動のベジェ経路を表示します。
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (_origin == null || _pathSegments == null || _pathSegments.Count == 0)
        {
            return;
        }

        const int curveDivisionCount = 24;
        Gizmos.color = Color.yellow;
        Vector3 startPosition = _origin.position;

        for (int index = 0; index < _pathSegments.Count; index++)
        {
            PathSegment segment = _pathSegments[index];
            if (segment == null || segment.ControlPosition == null || segment.TargetPosition == null)
            {
                continue;
            }

            Vector3 targetPosition = segment.TargetPosition.position;
            if (index == _pathSegments.Count - 1)
            {
                CrystalWalk crystalWalk = _crystalWalk != null ? _crystalWalk : GetComponent<CrystalWalk>();
                if (crystalWalk != null
                    && crystalWalk.TryGetNormalMovementStartPosition(out Vector3 normalMovementStartPosition))
                {
                    targetPosition = normalMovementStartPosition;
                }
            }

            Vector3 previousPosition = startPosition;
            for (int division = 1; division <= curveDivisionCount; division++)
            {
                float time = (float)division / curveDivisionCount;
                Vector3 currentPosition = CalculateQuadraticBezierPoint(
                    time,
                    startPosition,
                    segment.ControlPosition.position,
                    targetPosition);
                Gizmos.DrawLine(previousPosition, currentPosition);
                previousPosition = currentPosition;
            }

            startPosition = targetPosition;
        }
    }

    /// <summary>
    /// 初期移動を途中キャンセルし、CrystalWalk の移動停止を解除します。
    /// </summary>
    private void CancelTraversal()
    {
        _isInitialTraversing = false;
        if (_crystalWalk != null)
        {
            _crystalWalk.SetInitialTraversing(false);
        }
        _hasCompleted = true;
        ReleaseMovementSuspension();
    }

    /// <summary>
    /// CrystalWalk の移動停止を安全に解除します。
    /// </summary>
    private void ReleaseMovementSuspension()
    {
        if (_crystalWalk == null)
        {
            return;
        }

        _crystalWalk.SetMovementSuspended(false);
    }

    /// <summary>
    /// ベクトルに有限値のみが含まれているかを判定します。
    /// </summary>
    /// <param name="value">判定対象の座標です。</param>
    /// <returns>有限値のみで構成されている場合は true です。</returns>
    private static bool IsFinite(Vector3 value)
    {
        return !(float.IsNaN(value.x) || float.IsInfinity(value.x)
            || float.IsNaN(value.y) || float.IsInfinity(value.y)
            || float.IsNaN(value.z) || float.IsInfinity(value.z));
    }
}
