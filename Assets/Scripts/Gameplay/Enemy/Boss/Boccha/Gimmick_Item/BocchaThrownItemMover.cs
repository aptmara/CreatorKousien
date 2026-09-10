// ------------------------------------------------------------
// File		: BocchaThrownItemMover.cs
// Summary	: 投げられたアイテムを放物線上に直接動かす
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - 速度を積分せず時刻から位置を直接求めて、デバッグ表示の軌道と完全に一致させる!!
// - 着地した瞬間にエフェクトを再生するのを追加！！9/9
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 放物線上を直接移動させるコンポーネント。飛翔中だけ物理を止める。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BocchaThrownItemMover : MonoBehaviour
    {
        // ランタイム状態
        private Rigidbody _rigidbody;
        private CollectableGravity _collectableGravity;

        private Vector3 _startPosition;
        private Vector3 _velocity;
        private Vector3 _gravity;
        private float _flightTime;
        private float _elapsed;

        private bool _isFlying;
        private GameObject _landingVfxPrefab;
        private float _landingVfxLifeTime = 3.0f;
        private bool _wasKinematic;
        private bool _wasCollectableGravityEnabled;


        /// <summary>飛翔中かどうか。</summary>
        public bool IsFlying => _isFlying;


        /// <summary>
        /// 放物線飛行を開始する。
        /// </summary>
        /// <param name="from">投げ出す位置</param>
        /// <param name="velocity">初速</param>
        /// <param name="gravity">軌道計算に使う重力</param>
        /// <param name="flightTime">着地までの時間</param>
        public void Begin(Vector3 from, Vector3 velocity, Vector3 gravity, float flightTime)
        {
            CacheComponents();

            _startPosition = from;
            _velocity = velocity;
            _gravity = gravity;
            _flightTime = Mathf.Max(0.01f, flightTime);
            _elapsed = 0.0f;
            _isFlying = true;

            enabled = true;

            // 飛翔中は物理に干渉させない
            if (_rigidbody != null)
            {
                _wasKinematic = _rigidbody.isKinematic;

                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
            }

            // CollectableGravityが有効な場合は無効化する
            if (_collectableGravity != null)
            {
                _wasCollectableGravityEnabled = _collectableGravity.enabled;
                _collectableGravity.enabled = false;
            }

            transform.position = from;
        }


        /// <summary>
        /// 飛翔中の位置を更新
        /// </summary>
        private void Update()
        {
            if (!_isFlying)
            {
                return;
            }

            _elapsed += Time.deltaTime;

            if (_elapsed < _flightTime)
            {
                // デバッグ表示の軌道と同じ式で位置を直接求める
                transform.position = EvaluatePosition(_elapsed);

                return;
            }

            Finish();
        }


        // プールへ戻された場合など、飛翔中に無効化されても状態を戻す
        private void OnDisable() => Restore(Vector3.zero, false);


        private void Finish()
        {
            transform.position = EvaluatePosition(_flightTime);

            // 着地VFXを出す
            if (_landingVfxPrefab != null)
            {
                GameObject vfx = Instantiate(_landingVfxPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, _landingVfxLifeTime);
            }

            // 着地時点の速度をそのまま渡し、跳ね方が不自然にならないようにする
            Vector3 landingVelocity = _velocity + _gravity * _flightTime;

            Restore(landingVelocity, true);
        }


        private Vector3 EvaluatePosition(float time)
            => _startPosition + _velocity * time + 0.5f * _gravity * time * time;


        private void Restore(Vector3 landingVelocity, bool applyVelocity)
        {
            if (!_isFlying)
            {
                return;
            }

            _isFlying = false;

            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = _wasKinematic;

                if (applyVelocity && !_rigidbody.isKinematic)
                {
                    _rigidbody.linearVelocity = landingVelocity;
                    _rigidbody.angularVelocity = Random.insideUnitSphere * Random.Range(2.0f, 8.0f);
                }
            }

            if (_collectableGravity != null)
            {
                _collectableGravity.enabled = _wasCollectableGravityEnabled;
            }

            enabled = false;
        }


        private void CacheComponents()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            if (_collectableGravity == null)
            {
                _collectableGravity = GetComponent<CollectableGravity>();
            }
        }


        /// <summary>
        /// 着地した瞬間に出すVFXを設定する
        /// </summary>
        /// <param name="prefab">着地VFX</param>
        /// <param name="lifeTime">VFXを破棄するまでの時間</param>
        public void SetLandingVfx(GameObject prefab, float lifeTime)
        {
            _landingVfxPrefab = prefab;
            _landingVfxLifeTime = lifeTime;
        }
    }
}
