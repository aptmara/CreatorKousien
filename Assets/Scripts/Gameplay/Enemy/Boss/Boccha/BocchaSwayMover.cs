// ------------------------------------------------------------
// File		: BocchaSwayMover.cs
// Summary	: キング・ボッチャの「イダイなる横揺れ」(左右移動)を制御する
//            いやイダイなる横揺れってなんだよｗ
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - 登場演出が終わりInBattleになってから移動を開始する。
// - Intro / Down / Victory / Defeat 中は自動で停止する。
// - 分身ギミック中など一時的に止めたい場合は SetMoveEnabled(false) を呼ぶ。
// - フィールドが傾く構成のため、移動軸はFieldContextの回転に追従させる。
// - Animatorは未設定でも動作する(演出は後付け前提)。
// ------------------------------------------------------------
using Game.Gameplay.Stage;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 左右移動の方式。
    /// </summary>
    public enum BocchaSwayMode
    {
        /// <summary>等速で往復し、端で折り返す</summary>
        PingPong = 0,

        /// <summary>正弦波で滑らかに揺れる</summary>
        Sine = 1,
    }

    /// <summary>
    /// キング・ボッチャの左右移動を制御するコンポーネント
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BocchaSwayMover : MonoBehaviour
    {
        [Header("==== 参照 ====")]

        [SerializeField]
        [Tooltip("ボス戦のフロー制御。状態に応じて移動を自動で停止する。未設定なら同じGameObjectから取得する")]
        private BossBattleFlowController _flowController;

        [SerializeField]
        [Tooltip("移動させるTransform。未設定なら自身のTransformを使用する")]
        private Transform _moveTarget;


        [Header("==== 移動設定 ====")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("左右移動速度")]
        private float _moveSpeed = 2.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("中心位置から片側へ動ける距離")]
        private float _moveRangeX = 6.0f;

        [SerializeField]
        [Tooltip("移動方式。PingPong=等速往復 / Sine=滑らかな揺れ")]
        private BocchaSwayMode _swayMode = BocchaSwayMode.PingPong;

        [SerializeField]
        [Min(0f)]
        [Tooltip("PingPong時、端の手前この距離から減速を始める。0で減速なし")]
        private float _turnEaseWidth = 1.0f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("PingPong時、端で減速しきった時の速度倍率")]
        private float _turnMinSpeedRate = 0.25f;

        [SerializeField]
        [Tooltip("開始時に右方向へ動き出すかどうか")]
        private bool _startToRight = true;


        [Header("==== 開始条件 ====")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("戦闘開始(InBattle)から動き出すまでの待ち時間")]
        private float _startDelay = 0.0f;

        [SerializeField]
        [Tooltip("フィールドの傾きに移動軸を追従させるかどうか")]
        private bool _useFieldAxis = true;


        [Header("==== 演出 ====")]

        [SerializeField]
        [Tooltip("移動アニメーション用のAnimator。未設定なら何もしない")]
        private Animator _animator;

        [SerializeField]
        [Tooltip("移動中にtrueにするbool名。空なら何もしない")]
        private string _animBoolMoving = string.Empty;

        [SerializeField]
        [Tooltip("折り返した瞬間に鳴らすTrigger名。空なら何もしない")]
        private string _animTriggerTurn = string.Empty;


        // ランタイム状態
        // ------------------------------------------------------------

        private Vector3 _centerPosition;
        private float _offsetX;
        private float _sineTime;
        private int _direction = 1;
        private float _startDelayTimer;
        private bool _isMoveEnabled = true;
        private bool _hasStarted;
        private bool _wasMovingLastFrame;


        // 公開プロパティ
        // ------------------------------------------------------------

        /// <summary>
        /// 実際に移動している最中かどうか。
        /// </summary>
        public bool IsMoving { get; private set; }

        /// <summary>
        /// 中心位置からの現在のオフセット量。
        /// </summary>
        public float CurrentOffsetX => _offsetX;


        private void Awake()
        {
            if (_moveTarget == null)
            {
                _moveTarget = transform;
            }

            if (_flowController == null)
            {
                _flowController = GetComponent<BossBattleFlowController>();
            }

            _centerPosition = _moveTarget.position;
            _direction = _startToRight ? 1 : -1;
        }


        private void OnEnable()
        {
            if (_flowController != null)
            {
                _flowController.OnStateChanged += HandleStateChanged;
            }
        }


        private void OnDisable()
        {
            if (_flowController != null)
            {
                _flowController.OnStateChanged -= HandleStateChanged;
            }
        }


        private void Update()
        {
            if (!_hasStarted) return;

            if (_startDelayTimer > 0.0f)
            {
                _startDelayTimer -= Time.deltaTime;
                return;
            }

            IsMoving = _isMoveEnabled && CanMoveInCurrentState();

            ApplyMovingAnimation();

            if (!IsMoving) return;

            UpdateOffset(Time.deltaTime);
            ApplyPosition();
        }


        // 公開メソッド
        // ------------------------------------------------------------

        /// <summary>
        /// 移動を有効/無効にする。falseにすると停止する
        /// </summary>
        /// <param name="isEnabled">有効にするか</param>
        public void SetMoveEnable(bool isEnabled)
        {
            _isMoveEnabled = isEnabled;
        }


        /// <summary>
        /// 現在位置を新しい中心位置として再設定する
        /// </summary>
        private void ResetCenter()
        {
            _centerPosition = _moveTarget != null ? _moveTarget.position : transform.position;
            _offsetX = 0.0f;
            _sineTime = 0.0f;
        }




        // 内部処理
        // ------------------------------------------------------------

        // ボス戦の状態が変化したときに呼ばれる
        private void HandleStateChanged(BossBattleFlowState previousState, BossBattleFlowState newState)
        {
            // ボス戦の状態がInBattleになった瞬間に移動開始の準備をする
            if (_hasStarted || newState != BossBattleFlowState.InBattle) return;

            _hasStarted = true;
            _startDelayTimer = _startDelay;
            _centerPosition = _moveTarget.position;
        }


        // ボスが移動可能な状態かどうかを返す
        private bool CanMoveInCurrentState()
        {
            if (_flowController == null) return true;

            if (!_flowController.IsBattleActive) return false;

            return _flowController.CurrentState == BossBattleFlowState.InBattle;
        }


        // 座標更新処理
        private void UpdateOffset(float deltaTime)
        {
            float speed = _moveSpeed * GetSpeedMultiplier();

            if (_swayMode == BocchaSwayMode.Sine)
            {
                // 振れ幅で割ることで、_moveSpeedをPingPong時と同じ体感速度に揃える
                _sineTime += deltaTime * speed / Mathf.Max(0.01f, _moveRangeX);
                _offsetX = Mathf.Sin(_sineTime) * _moveRangeX * (_startToRight ? 1.0f : -1.0f);

                return;
            }

            float rate = 1.0f;

            if (_turnEaseWidth > 0.0f)
            {
                float distanceToEdge = Mathf.Max(0.0f, _moveRangeX - Mathf.Abs(_offsetX));

                rate = Mathf.Lerp(_turnMinSpeedRate, 1.0f, Mathf.Clamp01(distanceToEdge / _turnEaseWidth));
            }

            _offsetX += _direction * speed * rate * deltaTime;

            if (Mathf.Abs(_offsetX) < _moveRangeX) return;

            _offsetX = Mathf.Sign(_offsetX) * _moveRangeX;
            _direction = -_direction;

            ApplyTurnAnimation();
        }



        // 移動対象のTransformの位置を更新する
        private void ApplyPosition()
        {
            Vector3 axis = _useFieldAxis && FieldContext.IsReady ? FieldContext.Rotation * Vector3.right : Vector3.right;
            _moveTarget.position = _centerPosition + axis * _offsetX;
        }


        // スピードの倍率を取得する。フェーズデータがあればそこから、なければ1.0fを返す
        private float GetSpeedMultiplier()
        {
            BossFlowPhaseData phaseData = _flowController != null ? _flowController.CurrentPhaseData : null;

            return phaseData != null ? Mathf.Max(0.0f, phaseData.Multipliers.SpeedMultiplier) : 1.0f;
        }


        private void ApplyMovingAnimation()
        {
            if (_animator == null || string.IsNullOrEmpty(_animBoolMoving))
                return;

            if (IsMoving == _wasMovingLastFrame) return;

            _wasMovingLastFrame = IsMoving;
            _animator.SetBool(_animBoolMoving, IsMoving);
        }


        private void ApplyTurnAnimation()
        {
            if (_animator == null || string.IsNullOrEmpty(_animTriggerTurn))
                return;

            _animator.SetTrigger(_animTriggerTurn);
        }

#if UNITY_EDITOR
        // デバッグ用ギズモ描画
        private void OnDrawGizmosSelected()
        {
            // デバッグ用ギズモ描画
            Vector3 center = Application.isPlaying ? _centerPosition : (_moveTarget != null ? _moveTarget.position : transform.position);

            Vector3 axis = _useFieldAxis && FieldContext.IsReady ? FieldContext.Rotation * Vector3.right : Vector3.right;

            // 中心位置
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(center - axis * _moveRangeX, center + axis * _moveRangeX);
            Gizmos.DrawWireSphere(center - axis * _moveRangeX, 0.3f);
            Gizmos.DrawWireSphere(center + axis * _moveRangeX, 0.3f);
        }
#endif
    }
}
