/**
 * File:BalanceSshellCover
 * 
 * 皿の上に被せて中身を隠すためのオブジェクト。落ち物を落として破壊できる
 */
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Game.Gameplay.Collectibles;

namespace Game.Gameplay.Enemy.Boss
{

    public sealed class BalanceShellCover : MonoBehaviour
    {
        [Header("===== 参照 =====")]
        [SerializeField,Tooltip("中身を子にする位置。未設定なら自身")]
        private Transform _contentSocket;
        [SerializeField, Tooltip("被弾時に揺らす見た目。未設定なら自身")]
        private Transform _visual;

        [Header("===== 被弾設定 ======")]
        [SerializeField,Min(1)] private int _hitsToBreak = 3;
        [SerializeField, Min(0.0f)] private float _minImpactSpeed = 1.0f;
        [SerializeField, Min(0.0f), Tooltip("他店接触で一気にカウントされないための間隔")]
        private float _impactColldown = 0.05f;
        [SerializeField]
        private GameObject _breakVfxPrefab;

        [Header("===== 演出フック ======")]
        [SerializeField] private UnityEvent _onLanded;
        [SerializeField] private UnityEvent _onImpact;
        [SerializeField] private UnityEvent _onBroken;

        private bool _impactEnabled;
        private int _hitCount;
        private float _nextImpactTime;
        private Coroutine _punchRoutine;

        public Transform ContentSocket => _contentSocket != null ? _contentSocket : transform;
        public bool IsBroken { get; private set; }

        public float DamageProgress => Mathf.Clamp01(_hitCount / (float)_hitsToBreak);

        public event System.Action<BalanceShellCover> Broken;

        public void SetImpactEnable(bool enabledState) => _impactEnabled = enabledState;

        public void NotifyLanded() => _onLanded?.Invoke();

        public void OnCollisionEnter(Collision collision)
        {
            if (!_impactEnabled || IsBroken) return;
            if (Time.time < _nextImpactTime) return;
            if (collision.relativeVelocity.magnitude < _minImpactSpeed) return;
            if(collision.collider.GetComponentInParent<CollectibleObject>() == null) return;

            _nextImpactTime = Time.time + _impactColldown;
            _hitCount++;
            _onImpact?.Invoke();
            Punch();

            if(_hitCount >= _hitsToBreak)
            {
                Break();
            }
        }

        public void Break()
        {
            if (IsBroken) return;
            IsBroken = true;

            Broken?.Invoke(this);
            _onBroken?.Invoke();

            if(_breakVfxPrefab != null)
            {
                Instantiate(_breakVfxPrefab, transform.position, transform.rotation);
            }

            Destroy(gameObject);
        }

        private void Punch()
        {
            if(_punchRoutine != null) StopCoroutine(_punchRoutine);
            _punchRoutine = StartCoroutine(PunchRoutine());
        }

        private IEnumerator PunchRoutine()
        {
            Transform target = _visual != null ? _visual : transform;
            Vector3 baseScale = target.localScale;
            const float duration = 0.15f;

            float elapsed = 0.0f;
            while(elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float squash = 1.0f + Mathf.Sin(t * Mathf.PI) * 0.15f;
                target.localScale = new Vector3(baseScale.x * squash, baseScale.y / squash, baseScale.z * squash);
                yield return null;
            }

            target.localScale = baseScale;
            _punchRoutine = null;
        }

    }

}
