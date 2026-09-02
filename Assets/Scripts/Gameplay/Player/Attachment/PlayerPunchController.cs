// ------------------------------------------------------------
// File		: PlayerPunchController.cs
// Summary	: プレイヤーのパンチを管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-06-24
//
// Notes	:
// - プレイヤーのパンチを管理するクラスを作成
// - インタラクト入力でいつでも殴れるように改修。ヒット判定は前方Ray。
//   壊す処理はアニメイベント(PunchHit)のタイミングで ICrystalBreakable.Break を呼ぶ。
// - 7/5 プレイヤーのパンチのアニメ実装
// ------------------------------------------------------------
using System;
using UnityEngine;
using Game.Gameplay.Collectibles;
using Game.Core.Events;

namespace Game.Gameplay.Player
{
    public sealed class PlayerPunchController : MonoBehaviour
    {
        [Header("パンチ設定")]
        [Tooltip("プレイヤーのコントローラー")]
        [SerializeField] private PlayerController _playerController;
        [Tooltip("パンチのアタッチメントのコントローラー")]
        [SerializeField] private PlayerAttachmentController _attachmentController;
        [Tooltip("インタラクト入力を読むリーダー")]
        [SerializeField] private PlayerInputReader _inputReader;

        [Header("ヒット判定(Ray)")]
        [Tooltip("ON: 前方にクリスタルがある時だけ殴る / OFF: いつでも殴る(空振りあり)")]
        [SerializeField] private bool _requireTarget = true;
        [Tooltip("Rayの最大距離(仮。あとで調整)")]
        [SerializeField, Min(0f)] private float _rayMaxDistance = 30.0f;
        [Tooltip("Rayの当たり判定に使うレイヤー")]
        [SerializeField] private LayerMask _rayMask = ~0;
        [Tooltip("Ray原点の高さ(足元からの上げ幅)")]
        [SerializeField] private float _rayOriginHeight = 0.5f;
        [Tooltip("連続で殴れるようになるまでのクールダウン(秒)")]
        [SerializeField, Min(0f)] private float _cooldown = 0.35f;
        [Tooltip("殴りの当たり判定の太さ(半径)。大きいほど当てやすい")]
        [SerializeField, Min(0f)] private float _rayRadius = 1.0f;


        [Tooltip("本体(Visual)のアニメーション制御")]
        [SerializeField] private PlayerAnimationController _playerAnimationController;

        [Tooltip("表情(BlendShape)の制御")]
        [SerializeField] private PlayerFaceController _faceController;



        private AttachmentPunchAnimator _activePunchAnimator;
        private Action _pendingHitAction;   // レガシー(クリスタル駆動)用
        private bool _isPunching;
        private float _nextPunchTime;
        private ICrystalBreakable _pendingTarget;
        private Vector3 _pendingHitPoint;

        // --- SphereCast デバッグ表示用 ---
        private Vector3 _dbgOrigin;
        private Vector3 _dbgDir;
        private float _dbgLength;
        private Vector3 _dbgHitPoint;
        private bool _dbgHit;
        private bool _dbgValid;





        /// <summary>
        /// 毎フレーム、インタラクト入力を見て殴りを起動する
        /// </summary>
        private void Update()
        {
            if (_inputReader == null)
                return;

            // 入力は毎フレーム必ず消費
            bool pressed = _inputReader.ConsumeInteractPressed();

            // パンチ中・クールダウン中は、押下を判定しない！
            if (_isPunching || Time.time < _nextPunchTime)
                return;

            if (!pressed)
                return;

            // 押した瞬間にターゲットを確定して保持する
            // (アニメの当たり判定時に再キャストすると、動くクリスタルや向きのズレで外すため)
            bool hasTarget = TryRaycastCrystal(out _, out ICrystalBreakable target);

            // 空振り禁止: 前方にクリスタルが無ければ殴らない
            if (_requireTarget && !hasTarget)
                return;

            _pendingTarget = target;
            _pendingHitPoint = _dbgHitPoint;
            _nextPunchTime = Time.time + _cooldown;
            StartPunch();
        }


        /// <summary>
        /// 【互換用】クリスタル側から起動する旧API。コールバックでヒット処理を行う。
        /// </summary>
        public bool TryPlayPunch(Action onHit)
        {
            if (_isPunching)
                return false;

            _pendingHitAction = onHit;
            _pendingTarget = null;
            return StartPunch();
        }


        /// <summary>
        /// パンチを開始する(移動ロック＋アニメ再生)
        /// </summary>
        private bool StartPunch()
        {
            _isPunching = true;

            _playerController.SetCanMove(false);
            _attachmentController.SetPunchForceLarge(true);

            _playerAnimationController?.PlayPunch();

            if (_faceController == null)
                Debug.LogWarning("PlayerPunchController: FaceController is not assigned. Punch face will not be set.");
            else
                _faceController?.SetFace("Punch");

            if (_pendingTarget != null && _pendingTarget is CrystalWalk crystalWalk)
            {
                crystalWalk.PlayEffect(_pendingHitPoint, crystalWalk.transform.localScale.x * 3.0f);
            }

            _activePunchAnimator = _attachmentController.CurrentAttachment.GetComponentInChildren<AttachmentPunchAnimator>();

            if (_activePunchAnimator == null)
            {
                FinishPunch();
                return false;
            }

            _activePunchAnimator.PunchHit += OnPunchHit;
            _activePunchAnimator.PunchFinished += OnPunchFinished;
            _activePunchAnimator.PlayPunch();

            return true;
        }


        /// <summary>
        /// パンチが当たったとき(アニメイベント)の処理
        /// </summary>
        private void OnPunchHit()
        {
            // レガシー: コールバックがあればそれを実行して終わり
            if (_pendingHitAction != null)
            {
                Action hitAction = _pendingHitAction;
                _pendingHitAction = null;
                SoundManager.instance?.PlaySE("PlayerPunchHit");
                hitAction.Invoke();
                return;
            }

            // 新方式: 押した瞬間に捕まえたターゲットを壊す(再キャストしない)
            if (_pendingTarget != null)
            {
                Vector3 dir = AimTransform.forward;
                SoundManager.instance?.PlaySE("PlayerPunchHit");
                _pendingTarget.Break(_pendingHitPoint, dir);
                _pendingTarget = null;
            }
        }


        /// <summary>
        /// パンチアニメーションが終了したとき(アニメイベント)の処理
        /// </summary>
        private void OnPunchFinished()
        {
            FinishPunch();
        }


        /// <summary>
        /// パンチの終了処理
        /// </summary>
        private void FinishPunch()
        {
            if (_activePunchAnimator != null)
            {
                _activePunchAnimator.PunchHit -= OnPunchHit;
                _activePunchAnimator.PunchFinished -= OnPunchFinished;
            }

            _playerController.SetCanMove(true);
            _attachmentController.SetPunchForceLarge(false);

            _faceController?.ResetFace();

            _activePunchAnimator = null;
            _pendingHitAction = null;
            _pendingTarget = null;
            _isPunching = false;
        }


        /// <summary>
        /// Rayの基準にするTransform(プレイヤー本体)
        /// </summary>
        private Transform AimTransform => _playerController != null ? _playerController.transform : transform;


        /// <summary>
        /// 前方へRayを飛ばし、ICrystalBreakable に当たったか判定する
        /// </summary>
        private bool TryRaycastCrystal(out RaycastHit hit, out ICrystalBreakable breakable)
        {
            breakable = null;
            hit = default;

            Transform aim = AimTransform;
            Vector3 origin = aim.position + aim.up * _rayOriginHeight;
            Ray ray = new Ray(origin, aim.forward);

            // 球の中心が止まる距離を取得するために SphereCast を使う
            bool isHit = Physics.SphereCast(ray, _rayRadius, out hit, _rayMaxDistance, _rayMask, QueryTriggerInteraction.Ignore);

            if (isHit) breakable = hit.collider.GetComponentInParent<ICrystalBreakable>();
                
                


            if (breakable == null)
            {
                Collider[] overlaps = Physics.OverlapSphere(origin, _rayRadius, _rayMask, QueryTriggerInteraction.Ignore);
                foreach (Collider col in overlaps)
                {
                    ICrystalBreakable b = col.GetComponentInParent<ICrystalBreakable>();
                    if (b != null)
                    {
                        breakable = b;
                        _dbgHitPoint = col.ClosestPoint(origin);
                        _dbgLength = Vector3.Distance(origin, _dbgHitPoint);
                        _dbgDir = ray.direction;
                        _dbgHit = true;
                        _dbgValid = true;
                        return true;
                    }
                }
            }


            // デバッグ表示用に保持
            _dbgOrigin = ray.origin;
            _dbgDir = ray.direction;
            _dbgLength = isHit ? hit.distance : _rayMaxDistance;   // 球の中心が止まる距離
            _dbgHitPoint = isHit ? hit.point : ray.origin + ray.direction * _rayMaxDistance;
            _dbgHit = isHit;
            _dbgValid = true;

            return breakable != null;
        }

        /// <summary>
        /// デバッグ用: Rayの当たり判定をGizmoで描画する
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_dbgValid) return;

            Vector3 start = _dbgOrigin;
            Vector3 end = _dbgOrigin + _dbgDir * _dbgLength;   // 球の中心が止まる位置

            // 向きに直交する2軸を作る(筒の断面用)
            Vector3 axisA = Vector3.Cross(_dbgDir, Vector3.up);
            if (axisA.sqrMagnitude < 0.001f) axisA = Vector3.Cross(_dbgDir, Vector3.right);
            axisA = axisA.normalized * _rayRadius;
            Vector3 axisB = Vector3.Cross(_dbgDir, axisA).normalized * _rayRadius;

            Gizmos.color = _dbgHit ? Color.green : Color.red;

            // 始点・終点の球
            Gizmos.DrawWireSphere(start, _rayRadius);
            Gizmos.DrawWireSphere(end, _rayRadius);

            // 掃引した筒(4本)
            Gizmos.DrawLine(start + axisA, end + axisA);
            Gizmos.DrawLine(start - axisA, end - axisA);
            Gizmos.DrawLine(start + axisB, end + axisB);
            Gizmos.DrawLine(start - axisB, end - axisB);

            // 実際に当たった点
            if (_dbgHit)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_dbgHitPoint, 0.1f);
            }


        }
    }
}
