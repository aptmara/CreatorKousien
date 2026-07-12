using System.Collections;
using System.Collections.Generic;
using Game.Gameplay.Collectibles;
using Game.Gameplay.Stage;
using UnityEngine;
// ================================================================================
// File         : JackFlowerBine.cs
// Author       : Y_Akira
//
// Description  : ボスの総合コントローラー。
// Created      : 2026-07-11
// ================================================================================
namespace Game.Core.Enemy
{
    /// <summary>
    /// 崖下の左右からボス本体へ伸びる蔦と、蔦到達時のボス攻撃を管理する。
    /// </summary>
    public sealed class JackFlowerBossVineSpawner : MonoBehaviour
    {
        [Header("蔦の生成")]
        [SerializeField] private GameObject _vinePrefab;
        [SerializeField, Min(0.1f)] private float _spawnInterval = 4f;
        [SerializeField, Min(1)] private int _maxActiveVines = 2;
        [SerializeField, Min(0.1f)] private float _vineExtendSpeed = 6f;
        [SerializeField, Min(0.01f)] private float _vineHitBackDistance = 1.5f;
        [SerializeField, Min(0.01f)] private float _vineMaxShrinkDistance = 4.5f;

        [Header("蔦の到達点")]
        [SerializeField] private Transform _vineTargetOverride;
        [SerializeField] private Vector3 _vineTargetOffset;

        [Header("崖下の発生位置")]
        [SerializeField] private string _leftAnchorTag = "Field_LeftFront";
        [SerializeField] private string _rightAnchorTag = "Field_RightFront";
        [SerializeField] private string _cliffAnchorTag = "Floor";
        [SerializeField, Min(0f)] private float _spawnBelowCliffSurface = 1f;
        [SerializeField, Min(0f)] private float _startDepthOffset = 0.5f;
        [SerializeField, Min(0f)] private float _cliffEdgeInset = 1.5f;
        [SerializeField] private Vector3 _leftSpawnOffset;
        [SerializeField] private Vector3 _rightSpawnOffset;

        [Header("蔦を壊し切った時のボーナス")]
        [SerializeField, Min(0)] private int _collectibleCount = 12;

        private readonly List<JackFlowerVine> _activeVines = new();
        private EnemyController _enemyController;
        private EnemyRising _rising;
        private CollectibleSpawner _collectibleSpawner;
        private Animator _bossAnimator;
        private Coroutine _spawnCoroutine;
        private bool _spawnLeftNext = true;

        private void Awake()
        {
            _enemyController = GetComponentInParent<EnemyController>();
            _rising = GetComponentInParent<EnemyRising>();
            _bossAnimator = GetComponentInParent<Animator>();
        }

        private void OnEnable()
        {
            _spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        private void OnDisable()
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }
        }

        private IEnumerator SpawnRoutine()
        {
            while (enabled)
            {
                SpawnVine();
                yield return new WaitForSeconds(_spawnInterval);
            }
        }

        private void SpawnVine()
        {
            _activeVines.RemoveAll(vine => vine == null);
            if (_vinePrefab == null || _activeVines.Count >= _maxActiveVines)
            {
                return;
            }

            bool spawnLeft = _spawnLeftNext;
            Transform anchor = FindAnchor(spawnLeft ? _leftAnchorTag : _rightAnchorTag);
            _spawnLeftNext = !_spawnLeftNext;
            if (anchor == null)
            {
                Debug.LogWarning("[JackFlowerBossVineSpawner] 左右の崖アンカーが見つからないため、蔦を生成できません。", this);
                return;
            }

            Vector3 startPosition = anchor.position;
            if (TryGetCliffBounds(out Bounds cliffBounds))
            {
                startPosition.y = cliffBounds.max.y - _spawnBelowCliffSurface;
                startPosition.z = transform.position.z - _startDepthOffset;
            }
            else
            {
                Transform cliffAnchor = FindAnchor(_cliffAnchorTag);
                if (cliffAnchor != null)
                {
                    startPosition.y = cliffAnchor.position.y - _spawnBelowCliffSurface;
                    startPosition.z = transform.position.z - _startDepthOffset;
                }
            }
            startPosition.x = GetCliffEdgeX(spawnLeft, anchor.position.x);
            startPosition += spawnLeft ? _leftSpawnOffset : _rightSpawnOffset;
            GameObject vineObject = Instantiate(_vinePrefab, startPosition, Quaternion.identity);
            if (!vineObject.TryGetComponent(out JackFlowerVine vine))
            {
                Destroy(vineObject);
                return;
            }

            vine.Initialize(this, startPosition, _vineTargetOverride != null ? _vineTargetOverride : transform,
                _vineTargetOffset, _vineExtendSpeed, _vineHitBackDistance, _vineMaxShrinkDistance);
            _activeVines.Add(vine);
        }

        private float GetCliffEdgeX(bool left, float fallback)
        {
            if (!TryGetCliffBounds(out Bounds bounds))
            {
                return fallback;
            }

            return left
                ? bounds.min.x + _cliffEdgeInset
                : bounds.max.x - _cliffEdgeInset;
        }

        private bool TryGetCliffBounds(out Bounds bounds)
        {
            CliffGrassBlendMesh cliff = FindFirstObjectByType<CliffGrassBlendMesh>();
            Renderer renderer = cliff != null ? cliff.GetComponent<Renderer>() : null;
            if (renderer != null)
            {
                bounds = renderer.bounds;
                return true;
            }

            bounds = default;
            return false;
        }

        private Transform FindAnchor(string tag)
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

        /// <summary>
        /// パンチが蔦に当たった時に呼ばれる。
        /// </summary>
        public void HandleVinePunched(JackFlowerVine vine)
        {
            if (vine != null)
            {
                vine.ApplyHit();
            }
        }

        /// <summary>
        /// 蔦がボス本体へ到達した時に、攻撃モーションと防衛ライン攻撃を発生させる。
        /// </summary>
        public void HandleVineReached(JackFlowerVine vine)
        {
            if (vine == null)
            {
                return;
            }

            _enemyController ??= GetComponentInParent<EnemyController>();
            _bossAnimator ??= GetComponentInParent<Animator>();
            _bossAnimator?.Play("Base Layer.Hit", 0, 0f);
            _enemyController?.AttackNow();
        }

        /// <summary>
        /// 蔦を最大まで縮ませた時のボーナス落ちものを生成する。
        /// </summary>
        public void HandleVineFullyShrunk(JackFlowerVine vine, Vector3 position)
        {
            _activeVines.Remove(vine);
            _rising ??= GetComponentInParent<EnemyRising>();
            _rising?.DamageDrop(transform);

            _collectibleSpawner ??= FindFirstObjectByType<CollectibleSpawner>();
            if (_collectibleSpawner == null)
            {
                Debug.LogWarning("[JackFlowerBossVineSpawner] CollectibleSpawner が見つからないため、ボーナスを生成できません。", this);
                return;
            }

            _collectibleSpawner.SpawnCollectiblesAt(position, _collectibleCount);
        }
    }
}
