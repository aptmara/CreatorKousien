/**
 * File: BalanceTrayContentBase.cs
 * 
 * 皿の上に乗る中身の基底クラス
 * 
 * 
 */
using UnityEngine;
using UnityEngine.Events;

namespace Game.Gameplay.Enemy.Boss
{

    public abstract class BalanceTrayContentBase : MonoBehaviour , IBossTrayItem
    {
        [Header("===== 表示情報 =====")]
        [SerializeField]
        private string _displayName = "テンビン";
        [SerializeField]
        private Sprite _icon;

        [Header("===== 演出フック =====")]
        [SerializeField, Tooltip("ふたが外れて中身が確定した瞬間に呼ばれる")]
        private UnityEvent _onRevealed;

        private Renderer[] _renderers = System.Array.Empty<Renderer>();
        private bool[] _rendererDefaults = System.Array.Empty<bool>();
        private Collider[] _colliders = System.Array.Empty<Collider>();
        private bool[] _colliderDefaults = System.Array.Empty<bool>();
        private Rigidbody[] _rigidbodies = System.Array.Empty<Rigidbody>();
        private bool[] _rigidbodyKinematicDefaults = System.Array.Empty<bool>();

        protected BalanceShellContext Ctx { get; private set; }

        public TraySide Side { get; private set; } = TraySide.Level;
        public bool IsRevealed { get; private set; }

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;

        public virtual bool IsWeakness => false;

        public void Bind(BalanceShellContext context)
        {
            Ctx = context;

            _renderers = GetComponentsInChildren<Renderer>(true);
            _rendererDefaults = new bool[_renderers.Length];
            for (int i = 0; i < _renderers.Length; ++i)
            {
                _rendererDefaults[i] = _renderers[i] != null && _renderers[i].enabled;
            }

            _colliders = GetComponentsInChildren<Collider>(true);
            _colliderDefaults = new bool[_colliders.Length];
            for (int i = 0; i < _colliders.Length; ++i)
            {
                _colliderDefaults[i] = _colliders[i] != null && _colliders[i].enabled;
            }

            _rigidbodies = GetComponentsInChildren<Rigidbody>(true);
            _rigidbodyKinematicDefaults = new bool[_rigidbodies.Length];
            for(int i = 0;i < _rigidbodies.Length; ++i)
            {
                _rigidbodyKinematicDefaults[i] = _rigidbodies[i] != null && _rigidbodies[i].isKinematic;
            }

            SetCollidersEnabled(false);
            SetRigidbodyKinematic(true);
            OnBound();
        }

        public void SetVisible(bool visible)
        {
            for (int i = 0; i < _renderers.Length; ++i)
            {
                if (_renderers[i] == null) continue;
                _renderers[i].enabled = visible && _rendererDefaults[i];
            }
        }

        private void SetCollidersEnabled(bool enabledState)
        {
            for(int i = 0;i < _colliders.Length;++i)
            {
                if (_colliders[i] == null) continue;
                _colliders[i].enabled = enabledState && _colliderDefaults[i];
            }
        }

        private void SetRigidbodyKinematic(bool forceKinematic)
        {
            for(int i = 0;i < _rigidbodies.Length;++i)
            {
                if(_rigidbodies[i] == null)continue;
                _rigidbodies[i].isKinematic = forceKinematic ? true : _rigidbodyKinematicDefaults[i];

                if(forceKinematic)
                {
                    _rigidbodies[i].linearVelocity = Vector3.zero;
                    _rigidbodies[i].angularVelocity = Vector3.zero;
                }
            }
        }

        public void Reveal(TraySide side)
        {
            if(IsRevealed) return;

            Side = side;
            IsRevealed = true;
            SetCollidersEnabled(true);
            SetRigidbodyKinematic(false);

            OnRevealed();
            _onRevealed?.Invoke();
        }

        public void EndRound()
        {
            if (!IsRevealed) return;

            IsRevealed = false;
            OnRoundEnd();
        }

        public void OnTrayRaised()
        {
            if (IsRevealed) HandleTrayRaised();
        }

        public void OnTrayLowered()
        {
            if (IsRevealed) HandleTrayLowered();
        }

        protected virtual void OnBound() { }
        protected virtual void OnRevealed() { }
        protected virtual void OnRoundEnd() { }
        protected virtual void HandleTrayRaised() { }
        protected virtual void HandleTrayLowered() { }
    }

}
