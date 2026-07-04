// ------------------------------------------------------------
// File		: PlayerAnimationController.cs
// Summary	: プレイヤーのアニメーションを制御するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-06-24
//
// Notes	:
// - ベースの歩きアニメーションを再生する
// - 7/5  プレイヤーの挙動に応じて歩きアニメーションを変化させる機能の追加 - 浅野
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Player
{
    public class PlayerAnimationController : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");                     ///< AnimatorのSpeedパラメータのハッシュ値
        private static readonly int WalkAnimSpeedHash = Animator.StringToHash("WalkAnimSpeed");     ///< AnimatorのWalkSpeedパラメータのハッシュ値
        private static readonly int ShrunkBlendHash = Animator.StringToHash("ShrunkBlend");         ///< AnimatorのShrunkBlendパラメータのハッシュ値
        private static readonly int PunchHash = Animator.StringToHash("Punch");                     ///< AnimatorのPunchトリガーのハッシュ値

        [Header("アニメーション設定")]
        [Tooltip("Animatorコンポーネント")]
        [SerializeField] private Animator _animator;

        [Tooltip("プレイヤーの移動を制御するコンポーネント")]
        [SerializeField] private PlayerMotor _motor;


        [Header("アニメーションの速度設定")]
        [Tooltip("歩きアニメーションの速度")]
        [SerializeField] private float _baseMoveSpeed = 5f;

        [Tooltip("歩きアニメーションの最低速度")]
        [SerializeField] private float _minWalkAnimSpeed = 0.8f;

        [Tooltip("歩きアニメーションの最高速度")]
        [SerializeField] private float _maxWalkAnimSpeed = 1.8f;

        [Tooltip("プレイヤーのコントローラー")]
        [SerializeField] private PlayerController _playerController;



        /// <summary>
        /// AnimatorコンポーネントとPlayerMotorコンポーネントを取得する
        /// </summary>
        private void Reset()
        {
            _animator = GetComponent<Animator>();
            _motor = GetComponent<PlayerMotor>();
        }


        /// <summary>
        /// 毎フレーム、プレイヤーの速度に応じてアニメーションのSpeedパラメータを更新する
        /// </summary>
        private void Update()
        {
            if (_animator == null || _motor == null)
            {
                return;
            }

            // アタッチメントの拡大縮小状態に応じてShrunkBlendパラメータを更新する
            Vector3 planarVelocity = Vector3.ProjectOnPlane(_motor.Velocity, transform.up);
            float moveSpeed = planarVelocity.magnitude;

            // Speedパラメータを0～1の範囲に正規化し、歩きアニメーションの速度を調整する
            float speed01 = Mathf.Clamp01(moveSpeed / _baseMoveSpeed);
            float animSpeed = Mathf.Clamp(moveSpeed / _baseMoveSpeed, _minWalkAnimSpeed, _maxWalkAnimSpeed);

            // 集め状態なら1, 通常なら0へ滑らかに寄せる
            float shrunkTarget = (_playerController != null && _playerController.IsAttachmentShrunk) ? 1f : 0f;
            _animator.SetFloat(ShrunkBlendHash, shrunkTarget, 0.15f, Time.deltaTime);

            if (moveSpeed < 0.05f)
            {
                speed01 = 0f;
                animSpeed = 1f;
            }

            if (speed01 <= 0)
            {
                _animator.SetFloat(SpeedHash, 0f);
                _animator.SetFloat(WalkAnimSpeedHash, 1f);
                return;
            }
            else
            {
                _animator.SetFloat(SpeedHash, speed01, 0.1f, Time.deltaTime);
            }

            _animator.SetFloat(WalkAnimSpeedHash, animSpeed, 0.1f, Time.deltaTime);
        }



        /// <summary>
        /// パンチアニメーションを再生する
        /// </summary>
        public void PlayPunch()
        {
            if (_animator == null)
            {
                return;
            }

            _animator.ResetTrigger(PunchHash);
            _animator.SetTrigger(PunchHash);
        }
    }

}
