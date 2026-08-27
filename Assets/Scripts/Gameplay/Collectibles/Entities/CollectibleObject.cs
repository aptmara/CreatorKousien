// ================================================================================
// File         : CollectibleObject.cs
// Author       : Iwai Shogo
//
// Description  : 収集物の各種パラメータを定義するScriptableObject。
// Created      : 2026-05-06
// ================================================================================

using UnityEngine;
using Game.Data.Collectibles;
using Game.Core.Events;
using System;
using System.Collections.Generic;
using Game.Core.Roguelike;
using Game.Gameplay.Enemy.Boss;

namespace Game.Gameplay.Collectibles
{
    /// <summary>
    /// 物理実体としての収集物。
    /// プレイヤーに拾われるとデータに変換され、オブジェクト自体はPoolへ返却されます。
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class CollectibleObject : MonoBehaviour
    {
        private const string FieldWallRootName = "FIELD_WALL";
        private const string EnemySideWallName = "Wall_Front";
        private const string FieldLeftFrontAnchorTag = "Field_LeftFront";
        private const string FieldRightFrontAnchorTag = "Field_RightFront";
        private const float GummyTargetWorldY = 6f;
        private const float GummyBounceDuration = 3.0f;

        [Header("--- データ ---")]
        [Tooltip("このオブジェクトのマスターデータ")]
        [SerializeField] private CollectibleData _data;

        [Header("--- グミ反射設定 ---")]
        [Tooltip("グミが横へ跳ねる最小距離")]
        [SerializeField, Min(0f)] private float _gummyMinHorizontalDistance = 4f;
        [Tooltip("グミが横へ跳ねる最大距離")]
        [SerializeField, Min(0f)] private float _gummyMaxHorizontalDistance = 12f;

        [Header("--- 落下自動回収設定 ---")]
        [Tooltip("このY座標を下回ったら、自動でプールへ戻すデッドライン")]
        [SerializeField] private float _fallDeadLineY = -20f;


        [Header("--- 生成直後のすり抜け(対プレイヤー) ---")]
        [Tooltip("スポーン時のレイヤー")]
        [SerializeField] private string _spawnLayer = "CollectibleSpawning";
        [Tooltip("通常のレイヤー")]
        [SerializeField] private string _normalLayer = "Collectible";
        [Tooltip("生成直後にプレイヤーのコライダーをすり抜ける時間(秒)")]
        [SerializeField, Min(0f)] private float _passThroughDuration = 0.4f;


        private Rigidbody _rigidbody;
        private Action<CollectibleObject> _returnAction;
        private GameObject _currentVisual;

        private Dictionary<int, GameObject> _visualCache = new Dictionary<int, GameObject>();
        private Vector3 _initialScale;
        private Collider _collider;
        private Transform _fieldWallRoot;
        private Collider[] _fieldWallColliders;
        private Bounds _fieldWallLocalBounds;
        private bool _hasFieldWallBounds;

        // --- 特殊効果用のランタイム変数 ---
        private int _currentBounceCount = 0;
        private bool _isGummyBounceActive;
        private float _gummyBounceElapsed;
        private Vector3 _gummyBounceStartPosition;
        private Vector3 _gummyBounceTargetPosition;
        private float _gummyBaseBounceHeight;
        private CollectableGravity _collectableGravity;
        private bool _restoreCollectableGravityAfterBounce;
        private bool _isCollectableGravitySuspendedForBounce;

        public string Id => _data != null ? _data.Id : string.Empty;
        public float DamageAmount => _data != null ? _data.DamageAmount : 0f;

        public CollectibleType Type => _data != null ? _data.Type : default;
        public float SameItemCooldown => _data != null ? _data.SameItemCooldown : 0.25f;

        public bool CanBeCollectedByPlayer { get; private set; } = true;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _collectableGravity = GetComponent<CollectableGravity>();
            _initialScale = transform.localScale;
        }

        private void FixedUpdate()
        {
            UpdateGummyBounce();

            if (_rigidbody != null && _rigidbody.IsSleeping())
            {
                return;
            }

            Vector3 position = _rigidbody != null
                ? _rigidbody.position
                : transform.position;

            // アイテムがステージ外へ落下した場合は自動クリーンアップ
            if (position.y < _fallDeadLineY)
            {
                Despawn();
                return;
            }

            ResolveFieldWallBoundsIfNeeded();
            if (_fieldWallRoot != null && _hasFieldWallBounds)
            {
                Vector3 localPosition = _fieldWallRoot.InverseTransformPoint(position);
                if (IsOutsideFieldBounds(localPosition))
                {
                    MoveInsideField();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsFieldWall(other.transform)
                || IsEnemySideWall(other.transform))
            {
                return;
            }

            ResolveFieldWallBoundsIfNeeded();
            if (_fieldWallRoot == null || !_hasFieldWallBounds)
            {
                return;
            }

            Vector3 localPosition = _fieldWallRoot.InverseTransformPoint(transform.position);
            if (IsOutsideFieldBounds(localPosition))
            {
                MoveInsideField(other);
            }
        }

        /// <summary>
        /// Poolから取得した際の初期化処理
        /// </summary>
        public void Initialize(CollectibleData data, Action<CollectibleObject> returnAction)
        {
            Initialize(data, returnAction, true);
        }

        /// <summary>
        /// Poolから取得した際の初期化処理
        /// </summary>
        /// <param name="data">収集物データ</param>
        /// <param name="returnAction">Pool返却処理</param>
        /// <param name="canBeCollectedByPlayer">プレイヤー収集を許可するか</param>
        public void Initialize(CollectibleData data, Action<CollectibleObject> returnAction, bool canBeCollectedByPlayer)
        {
            ResolveFieldWallBoundsIfNeeded();
            _data = data;
            _returnAction = returnAction;
            CanBeCollectedByPlayer = canBeCollectedByPlayer;
            _currentBounceCount = 0;
            ResetGummyBounceState();
            transform.localScale *= RoguelikeUpgradeRuntime.CollectibleScaleMultiplier;

            UpdateVisual();
            ApplySpecialPhysics();
            BeginSpawnPassThrough();
        }

        private void ResolveFieldWallBoundsIfNeeded()
        {
            if (_fieldWallRoot != null && _hasFieldWallBounds)
            {
                return;
            }

            GameObject fieldWall = GameObject.Find(FieldWallRootName);
            if (fieldWall == null)
            {
                return;
            }

            _fieldWallRoot = fieldWall.transform;
            Collider[] wallColliders = fieldWall.GetComponentsInChildren<Collider>(true);
            List<Collider> activeWallColliders = new List<Collider>();
            bool hasPoint = false;
            Vector3 minimum = Vector3.zero;
            Vector3 maximum = Vector3.zero;

            foreach (Collider wallCollider in wallColliders)
            {
                if (wallCollider == null
                    || !wallCollider.enabled
                    || !wallCollider.gameObject.activeInHierarchy)
                {
                    continue;
                }

                activeWallColliders.Add(wallCollider);

                if (wallCollider is BoxCollider boxCollider)
                {
                    EncapsulateBoxCollider(boxCollider, ref minimum, ref maximum, ref hasPoint);
                }
                else
                {
                    EncapsulateWorldBounds(wallCollider.bounds, ref minimum, ref maximum, ref hasPoint);
                }
            }

            if (!hasPoint)
            {
                return;
            }

            _fieldWallLocalBounds.SetMinMax(minimum, maximum);
            _fieldWallColliders = activeWallColliders.ToArray();
            _hasFieldWallBounds = true;
        }

        private bool IsOutsideFieldBounds(Vector3 localPosition)
        {
            return localPosition.x < _fieldWallLocalBounds.min.x
                || localPosition.x > _fieldWallLocalBounds.max.x
                || localPosition.z > _fieldWallLocalBounds.max.z;
        }

        private void MoveInsideField(Collider contactedWall = null)
        {
            ResolveFieldWallBoundsIfNeeded();
            if (_fieldWallRoot == null || !_hasFieldWallBounds)
            {
                return;
            }

            Vector3 currentPosition = transform.position;
            Vector3 localPosition = _fieldWallRoot.InverseTransformPoint(currentPosition);
            Vector3 localCenter = _fieldWallLocalBounds.center;
            localCenter.y = localPosition.y;

            Vector3 fieldCenter = _fieldWallRoot.TransformPoint(localCenter);
            Vector3 outwardDirection = currentPosition - fieldCenter;
            float distanceFromCenter = outwardDirection.magnitude;
            if (distanceFromCenter <= 0.0001f)
            {
                return;
            }

            outwardDirection /= distanceFromCenter;
            float insideClearance = GetColliderExtentAlong(outwardDirection) + 0.01f;
            float rayDistance = distanceFromCenter + insideClearance + 0.01f;

            if (!TryGetInnerWallPoint(
                    contactedWall,
                    fieldCenter,
                    outwardDirection,
                    rayDistance,
                    out Vector3 innerWallPoint))
            {
                Vector3 clampedPosition = new Vector3(
                    Mathf.Clamp(localPosition.x, _fieldWallLocalBounds.min.x, _fieldWallLocalBounds.max.x),
                    localPosition.y,
                    Mathf.Clamp(localPosition.z, _fieldWallLocalBounds.min.z, _fieldWallLocalBounds.max.z));
                Vector3 clampedWorldPosition = _fieldWallRoot.TransformPoint(clampedPosition);
                Vector3 inwardDirection = (fieldCenter - clampedWorldPosition).normalized;
                SetPositionAndRemoveOutwardVelocity(
                    clampedWorldPosition + inwardDirection * insideClearance,
                    outwardDirection);
                return;
            }

            SetPositionAndRemoveOutwardVelocity(
                innerWallPoint - outwardDirection * insideClearance,
                outwardDirection);
        }

        private bool TryGetInnerWallPoint(
            Collider contactedWall,
            Vector3 rayOrigin,
            Vector3 rayDirection,
            float rayDistance,
            out Vector3 innerWallPoint)
        {
            Ray ray = new Ray(rayOrigin, rayDirection);
            if (contactedWall != null
                && contactedWall.enabled
                && contactedWall.gameObject.activeInHierarchy
                && contactedWall.Raycast(ray, out RaycastHit contactedHit, rayDistance))
            {
                innerWallPoint = contactedHit.point;
                return true;
            }

            bool found = false;
            float nearestDistance = float.MaxValue;
            innerWallPoint = Vector3.zero;

            if (_fieldWallColliders == null)
            {
                return false;
            }

            foreach (Collider wallCollider in _fieldWallColliders)
            {
                if (wallCollider == null
                    || !wallCollider.enabled
                    || !wallCollider.gameObject.activeInHierarchy
                    || !wallCollider.Raycast(ray, out RaycastHit hit, rayDistance)
                    || hit.distance >= nearestDistance)
                {
                    continue;
                }

                found = true;
                nearestDistance = hit.distance;
                innerWallPoint = hit.point;
            }

            return found;
        }

        private float GetColliderExtentAlong(Vector3 direction)
        {
            if (_collider == null)
            {
                return 0f;
            }

            Vector3 extents = _collider.bounds.extents;
            return Mathf.Abs(direction.x) * extents.x
                + Mathf.Abs(direction.y) * extents.y
                + Mathf.Abs(direction.z) * extents.z;
        }

        private void SetPositionAndRemoveOutwardVelocity(
            Vector3 position,
            Vector3 outwardDirection)
        {
            if (_rigidbody != null)
            {
                _rigidbody.position = position;

                float outwardSpeed = Vector3.Dot(
                    _rigidbody.linearVelocity,
                    outwardDirection);
                if (outwardSpeed > 0f)
                {
                    _rigidbody.linearVelocity -= outwardDirection * outwardSpeed;
                }

                return;
            }

            transform.position = position;
        }

        private void EncapsulateBoxCollider(
            BoxCollider boxCollider,
            ref Vector3 minimum,
            ref Vector3 maximum,
            ref bool hasPoint)
        {
            Vector3 extents = boxCollider.size * 0.5f;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 localCorner = boxCollider.center + Vector3.Scale(
                            extents,
                            new Vector3(x, y, z));
                        Vector3 worldCorner = boxCollider.transform.TransformPoint(localCorner);
                        EncapsulatePoint(
                            _fieldWallRoot.InverseTransformPoint(worldCorner),
                            ref minimum,
                            ref maximum,
                            ref hasPoint);
                    }
                }
            }
        }

        private void EncapsulateWorldBounds(
            Bounds worldBounds,
            ref Vector3 minimum,
            ref Vector3 maximum,
            ref bool hasPoint)
        {
            Vector3 extents = worldBounds.extents;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 worldCorner = worldBounds.center + Vector3.Scale(
                            extents,
                            new Vector3(x, y, z));
                        EncapsulatePoint(
                            _fieldWallRoot.InverseTransformPoint(worldCorner),
                            ref minimum,
                            ref maximum,
                            ref hasPoint);
                    }
                }
            }
        }

        private static void EncapsulatePoint(
            Vector3 point,
            ref Vector3 minimum,
            ref Vector3 maximum,
            ref bool hasPoint)
        {
            if (!hasPoint)
            {
                minimum = point;
                maximum = point;
                hasPoint = true;
                return;
            }

            minimum = Vector3.Min(minimum, point);
            maximum = Vector3.Max(maximum, point);
        }

        private bool IsFieldWall(Transform target)
        {
            while (target != null)
            {
                if (target == _fieldWallRoot || target.name == FieldWallRootName)
                {
                    return true;
                }

                target = target.parent;
            }

            return false;
        }

        private bool IsEnemySideWall(Transform target)
        {
            while (target != null && target != _fieldWallRoot)
            {
                if (target.name == EnemySideWallName)
                {
                    return true;
                }

                target = target.parent;
            }

            return false;
        }

        /// <summary>
        /// アイテムのタイプに応じて物理特性を適用
        /// </summary>
        private void ApplySpecialPhysics()
        {
            if (_data == null) return;

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                // グミ且つPhysicsMaterialが設定されていれば適用
                if (_data.Type == CollectibleType.Gummy && _data.GummyPhysicsMaterial != null)
                {
                    col.material = _data.GummyPhysicsMaterial;
                }
                else
                {
                    col.material = null;
                }
            }
        }

        /// <summary>
        /// エネミーに衝突した際、このアイテム固有の技を発生させる。
        /// EnemyHitReceiverから呼び出されます。
        /// </summary>
        public bool ExecuteHitImpact(string enemyId, float bodyDamage, Vector3 hitPosition, Transform enemyTransform)
        {
            if (_data == null) return false;

            // 1. グミの反射と最大連鎖数チェック
            if (_data.Type == CollectibleType.Gummy && !TryQueueGummyBounce())
            {
                return false;
            }

            // 2. イベントを発行
            EventBus.Publish(new EnemyHitBatchEvent(enemyId, 1, bodyDamage, hitPosition, enemyTransform, _data));

            // 3. 十字架専用ロジック
            if (_data.Type == CollectibleType.Cross)
            {
                SpawnCrossLaser(hitPosition);
            }

            // 4. 天秤弱点専用ロジック
            if(_data.Type == CollectibleType.BossWeak)
            {
                var weaker = enemyTransform != null ? enemyTransform.GetComponentInParent<IBossHittable>() : null;
            }

            return true;
        }

        /// <summary>
        /// グミを命中位置のZ座標に留め、規定高さを頂点とする曲線で跳ね返らせる。
        /// </summary>
        /// <returns>反射を予約できた場合はtrue</returns>
        public bool TryQueueGummyBounce()
        {
            if (_data == null || _data.Type != CollectibleType.Gummy)
            {
                return false;
            }

            if (_currentBounceCount >= _data.MaxBounceChainCount)
            {
                Despawn();
                return false;
            }

            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            _currentBounceCount++;

            SuspendCollectableGravity();
            _isGummyBounceActive = true;
            _gummyBounceElapsed = 0f;
            _gummyBounceStartPosition = _rigidbody.position;
            _gummyBounceTargetPosition = GetGummyBounceTarget(_gummyBounceStartPosition);
            _rigidbody.linearVelocity = Vector3.zero;
            return true;
        }

        private Vector3 GetGummyBounceTarget(Vector3 startPosition)
        {
            Transform leftFrontAnchor = FindStageAnchor(FieldLeftFrontAnchorTag);
            Transform rightFrontAnchor = FindStageAnchor(FieldRightFrontAnchorTag);
            if (_currentBounceCount == 1)
            {
                _gummyBaseBounceHeight = Mathf.Max(
                    0f,
                    GummyTargetWorldY - startPosition.y);
            }

            float bounceHeight = _gummyBaseBounceHeight
                * Mathf.Pow(0.5f, _currentBounceCount - 1);
            float targetY = startPosition.y + bounceHeight;

            if (leftFrontAnchor == null && rightFrontAnchor == null)
            {
                return new Vector3(startPosition.x, targetY, startPosition.z);
            }

            Vector3 leftPosition = leftFrontAnchor != null
                ? leftFrontAnchor.position
                : rightFrontAnchor.position;
            Vector3 rightPosition = rightFrontAnchor != null
                ? rightFrontAnchor.position
                : leftFrontAnchor.position;
            float targetX = GetRandomGummyTargetX(
                startPosition.x,
                Mathf.Min(leftPosition.x, rightPosition.x),
                Mathf.Max(leftPosition.x, rightPosition.x));
            return new Vector3(targetX, targetY, startPosition.z);
        }

        private float GetRandomGummyTargetX(
            float startX,
            float stageMinX,
            float stageMaxX)
        {
            float minDistance = Mathf.Min(
                _gummyMinHorizontalDistance,
                _gummyMaxHorizontalDistance);
            float maxDistance = Mathf.Max(
                _gummyMinHorizontalDistance,
                _gummyMaxHorizontalDistance);
            float availableLeft = Mathf.Min(
                maxDistance,
                Mathf.Max(0f, startX - stageMinX));
            float availableRight = Mathf.Min(
                maxDistance,
                Mathf.Max(0f, stageMaxX - startX));
            bool canMoveLeft = availableLeft >= minDistance;
            bool canMoveRight = availableRight >= minDistance;

            if (!canMoveLeft && !canMoveRight)
            {
                return Mathf.Clamp(startX, stageMinX, stageMaxX);
            }

            bool moveRight = canMoveRight
                && (!canMoveLeft || UnityEngine.Random.value >= 0.5f);
            float availableDistance = moveRight
                ? availableRight
                : availableLeft;
            float distance = UnityEngine.Random.Range(
                minDistance,
                availableDistance);
            return startX + (moveRight ? distance : -distance);
        }

        private static Transform FindStageAnchor(string tag)
        {
            try
            {
                GameObject anchor = GameObject.FindGameObjectWithTag(tag);
                return anchor != null ? anchor.transform : null;
            }
            catch (UnityException)
            {
                return null;
            }
        }

        private void UpdateGummyBounce()
        {
            if (!_isGummyBounceActive || _rigidbody == null)
            {
                return;
            }

            _gummyBounceElapsed = Mathf.Min(
                _gummyBounceElapsed + Time.fixedDeltaTime,
                GummyBounceDuration);
            float bounceRate = _gummyBounceElapsed / GummyBounceDuration;
            float horizontalRate = Mathf.SmoothStep(0f, 1f, bounceRate);
            float heightRate;
            if (bounceRate <= 0.5f)
            {
                heightRate = Mathf.SmoothStep(0f, 1f, bounceRate * 2f);
            }
            else
            {
                float fallRate = (bounceRate - 0.5f) * 2f;
                heightRate = 1f - fallRate * fallRate;
            }

            float bounceHeight = _gummyBounceTargetPosition.y
                - _gummyBounceStartPosition.y;
            Vector3 nextPosition = new Vector3(
                Mathf.Lerp(
                    _gummyBounceStartPosition.x,
                    _gummyBounceTargetPosition.x,
                    horizontalRate),
                Mathf.Lerp(
                    _gummyBounceStartPosition.y,
                    _gummyBounceTargetPosition.y,
                    heightRate),
                _gummyBounceStartPosition.z);

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.MovePosition(nextPosition);

            if (_gummyBounceElapsed < GummyBounceDuration)
            {
                return;
            }

            _isGummyBounceActive = false;
            _rigidbody.linearVelocity = Vector3.down
                * (4f * bounceHeight / GummyBounceDuration);
            RestoreCollectableGravity();
        }

        private void SuspendCollectableGravity()
        {
            if (_collectableGravity == null)
            {
                _collectableGravity = GetComponent<CollectableGravity>();
            }

            if (_collectableGravity == null || _isGummyBounceActive)
            {
                return;
            }

            _restoreCollectableGravityAfterBounce = _collectableGravity.enabled;
            _collectableGravity.enabled = false;
            _isCollectableGravitySuspendedForBounce = true;
        }

        private void RestoreCollectableGravity()
        {
            if (!_isCollectableGravitySuspendedForBounce)
            {
                return;
            }

            if (_collectableGravity != null)
            {
                _collectableGravity.enabled = _restoreCollectableGravityAfterBounce;
            }

            _restoreCollectableGravityAfterBounce = false;
            _isCollectableGravitySuspendedForBounce = false;
        }

        private void ResetGummyBounceState()
        {
            _isGummyBounceActive = false;
            _gummyBounceElapsed = 0f;
            _gummyBounceStartPosition = Vector3.zero;
            _gummyBounceTargetPosition = Vector3.zero;
            _gummyBaseBounceHeight = 0f;
            RestoreCollectableGravity();
        }

        /// <summary>
        /// 十字架レーザーを画面右側の同じ高さに生成する。
        /// </summary>
        private void SpawnCrossLaser(Vector3 hitPosition)
        {
            if (_data.LaserPrefab == null) return;

            Vector3 spawnPosition = new Vector3(15f, hitPosition.y, hitPosition.z);
            GameObject laserObj = Instantiate(_data.LaserPrefab, spawnPosition, Quaternion.LookRotation(Vector3.left));

            // レーザーの生存期間は任意で設定（例: 1.5秒）
            Destroy(laserObj, 1.5f);
        }

        /// <summary>
        /// 外部システムから物理挙動を初期化する。
        /// </summary>
        /// <param name="velocity">初期速度</param>
        /// <param name="angularVelocity">初期角速度</param>
        public void SetInitialMotion(Vector3 velocity, Vector3 angularVelocity)
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            _rigidbody.linearVelocity = velocity;
            _rigidbody.angularVelocity = angularVelocity;
        }

        private void UpdateVisual()
        {
            // 現在表示されている見た目があれば非表示にする
            if (_currentVisual != null)
            {
                _currentVisual.SetActive(false);
            }

            if (_data == null || _data.ViewPrefab == null) return;

            int prefabId = _data.ViewPrefab.GetInstanceID();

            // キャッシュに存在しない場合は新規生成
            if (!_visualCache.TryGetValue(prefabId, out GameObject visualInstance))
            {
                visualInstance = Instantiate(_data.ViewPrefab, transform);
                visualInstance.transform.localPosition = Vector3.zero;
                visualInstance.transform.localRotation = Quaternion.identity;

                // 物理演算の干渉を防ぐ
                Collider visualCollider = visualInstance.GetComponent<Collider>();
                if (visualCollider != null) Destroy(visualCollider);

                _visualCache[prefabId] = visualInstance;
            }

            // 該当する見た目を有効化
            visualInstance.SetActive(true);
            _currentVisual = visualInstance;
        }

        /// <summary>
        /// プレイヤーのCollectorに収集された際に呼ばれる処理
        /// </summary>
        /// <returns>軽量データ化された HeldItem</returns>
        public HeldItem OnCollected()
        {
            if (_data == null || !CanBeCollectedByPlayer)
            {
                return null;
            }

            // 物理オブジェクトを保持せず、軽量データへ変換
            HeldItem heldItem = new HeldItem(_data, this);

            // 自身をPoolへ返却する
            _returnAction?.Invoke(this);

            return heldItem;
        }


        /// <summary>
        /// プレイヤーが物理的に拾い上げる際の処理。
        /// </summary>
        /// <returns>Itemの軽量データ</returns>
        public HeldItem TakeOwnership()
        {
            if (_data == null || !CanBeCollectedByPlayer)
            {
                return null;
            }

            // 自分自身（this）を渡しつつデータ化
            return new HeldItem(_data, this);
        }


        /// <summary>
        /// Pool返却時に状態をリセットします
        /// </summary>
        public void ResetState()
        {
            CancelInvoke(nameof(EndSpawnPassThrough));

            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            CanBeCollectedByPlayer = true;
            ResetGummyBounceState();
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            transform.localScale = _initialScale;

            // プール返却時に見た目も非表示にリセット
            if (_currentVisual != null)
            {
                _currentVisual.SetActive(false);
                _currentVisual = null;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 収集以外の理由でPoolへ返却する。
        /// </summary>
        public void Despawn()
        {
            if (_returnAction != null)
            {
                _returnAction.Invoke(this);
                return;
            }

            ResetState();
        }

        /// <summary>
        /// E担当AvalancheControllerから呼ばれ、物理的な力を受けます。
        /// </summary>
        public void ApplyAvalancheForce(Vector3 force)
        {
            _rigidbody.AddForce(force, ForceMode.Impulse);
        }

        public CollectibleData GetCollectableData()
        {
            return _data;
        }


        /// <summary>
        /// 生成直後のすり抜け時間が経過したら、通常のレイヤーに戻す
        /// </summary>
        private void EndSpawnPassThrough()
        {
            int normal = LayerMask.NameToLayer(_normalLayer);
            if (normal >= 0)
            {
                SetLayerRecursively(gameObject, normal);
            }
        }


        /// <summary>
        /// 指定したGameObjectとその子オブジェクトすべてのレイヤーを再帰的に設定する
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="newLayer"></param>
        private static void SetLayerRecursively(GameObject obj, int newLayer)
        {
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }


        /// <summary>
        /// 生成直後にプレイヤーのコライダーをすり抜けるためのレイヤー設定を行う
        /// </summary>
        private void BeginSpawnPassThrough()
        {
            int spawn = LayerMask.NameToLayer(_spawnLayer);
            if (spawn < 0)
            {
                return;
            }

            SetLayerRecursively(gameObject, spawn);
            CancelInvoke(nameof(EndSpawnPassThrough));
            Invoke(nameof(EndSpawnPassThrough), _passThroughDuration);
        }
    }
}
