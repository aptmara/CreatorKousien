using Game.Gameplay.Collectibles;
using UnityEngine;
// ================================================================================
// File         : JackFlowerBine.cs
// Author       : Y_Akira
//
// Description  : ボスの左右伸縮ツタ生成コントローラー。
// Created      : 2026-07-11
// ================================================================================
namespace Game.Core.Enemy
{
    /// <summary>
    /// 崖下からボスへ伸び、パンチでヒットバックする蔦。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class JackFlowerVine : MonoBehaviour, ICrystalBreakable
    {
        private JackFlowerBossVineSpawner _owner;
        private Transform _target;
        private Vector3 _targetOffset;
        private Vector3 _startPosition;
        private float _extendSpeed;
        private float _hitBackDistance;
        private float _maxShrinkDistance;
        private float _currentLength;
        private float _shrinkDistance;
        private bool _hasReached;
        private bool _isBroken;

        public void Initialize(
            JackFlowerBossVineSpawner owner,
            Vector3 startPosition,
            Transform target,
            Vector3 targetOffset,
            float extendSpeed,
            float hitBackDistance,
            float maxShrinkDistance)
        {
            _owner = owner;
            _target = target;
            _targetOffset = targetOffset;
            _startPosition = startPosition;
            _extendSpeed = extendSpeed;
            _hitBackDistance = hitBackDistance;
            _maxShrinkDistance = maxShrinkDistance;
            _currentLength = 0f;
            UpdateVisual();
        }

        private void Update()
        {
            if (_isBroken || _target == null)
            {
                return;
            }

            float maxLength = Vector3.Distance(_startPosition, GetTargetPosition());
            _currentLength = Mathf.MoveTowards(_currentLength, maxLength, _extendSpeed * Time.deltaTime);
            UpdateVisual();

            if (!_hasReached && maxLength > 0.01f && _currentLength >= maxLength - 0.05f)
            {
                _hasReached = true;
                _owner?.HandleVineReached(this);
            }
        }

        /// <summary>
        /// パンチで蔦を縮ませる。縮み量は蓄積し、最大値で破壊される。
        /// </summary>
        public void ApplyHit()
        {
            if (_isBroken)
            {
                return;
            }

            _shrinkDistance += _hitBackDistance;
            if (_shrinkDistance >= _maxShrinkDistance)
            {
                _isBroken = true;
                _owner?.HandleVineFullyShrunk(this, transform.position);
                Destroy(gameObject);
                return;
            }

            _currentLength = Mathf.Max(0f, _currentLength - _hitBackDistance);
            _hasReached = false;
            UpdateVisual();
        }

        /// <summary>
        /// プレイヤーのパンチシステムから呼ばれる共通入口。
        /// </summary>
        public void Break(Vector3 hitPoint, Vector3 hitDirection)
        {
            _owner?.HandleVinePunched(this);
        }

        private void UpdateVisual()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 delta = GetTargetPosition() - _startPosition;
            float maxLength = delta.magnitude;
            if (maxLength <= 0.01f)
            {
                return;
            }

            Vector3 direction = delta / maxLength;
            transform.position = _startPosition + direction * (_currentLength * 0.5f);
            transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);

            // 蔦プレハブを現在長に合わせて伸縮する。
            Vector3 scale = transform.localScale;
            transform.localScale = new Vector3(scale.x, Mathf.Max(0.01f, _currentLength * 0.5f), scale.z);
        }

        private Vector3 GetTargetPosition()
        {
            return _target.position + _targetOffset;
        }
    }
}
