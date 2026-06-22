// ------------------------------------------------------------
// File		: CrystalInteractable.cs
// Summary	: プレイヤーが近くで入力した時にクリスタルからかけらを発生させる
//
// Author	: [浅野 勇生]
// Created	: 2026-05-30
//
// Notes	:
// - このクラスは、プレイヤーが近くで特定の入力を行ったときに、クリスタルからかけらを発生させるためのものです。
// - [6/23] Crystalの一括ばらまき機能を追加。CrystalOneBreakforScaleを設定すると、Interactで一括ばらまきが呼ばれる。
// ------------------------------------------------------------
using Game.Gameplay.Player;
using UnityEngine;
using Game.Data.Collectibles;

namespace Game.Gameplay.Collectibles
{
    /// <summary>
    /// Trigger範囲内にいるプレイヤーの入力を受け、CrystalShardEmitterを実行します。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class CrystalInteractable : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("クリスタルからかけらを発生させるためのエミッター")]
        [SerializeField] private CrystalShardEmitter _emitter;
        [Tooltip("プレイヤーの入力を受け取るための当たり判定のオブジェクト")]
        [SerializeField] private Transform _hitOrigin;

        [Header("Emission")]
        [Tooltip("クリスタルからかけらが発生する際の力の大きさ")]
        [SerializeField, Min(0.1f)] private float _interactionPower = 1.0f;
        [Tooltip("プレイヤーがこのキーを押してから、次に押せるようになるまでのクールダウン時間（秒）")]
        [SerializeField, Min(0f)] private float _interactCooldown = 0.35f;

        [Tooltip("だんだんでかくなるクリスタル。設定すると、一括ばらまきを呼ぶ！")]
        [SerializeField] private CrystalOneBreakforScale _oneBreak;


        [Header(("Ray判定"))]
        [Tooltip("Rayの最大距離")]
        [SerializeField, Min(0f)] private float _rayMaxDistance = 30.0f;
        [Tooltip("Rayの当たり判定に使うレイヤー")]
        [SerializeField] private LayerMask _rayMask = ~0;

        private PlayerFacade _nearbyPlayer;
        private PlayerInputReader _nearbyInput;
        private float _nextInteractTime;

        /// <summary>
        /// コンポーネントの初期化
        /// </summary>
        private void Awake()
        {
            if (_emitter == null)
            {
                _emitter = GetComponent<CrystalShardEmitter>();
            }

            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }


        /// <summary>
        /// 更新処理
        /// </summary>
        private void Update()
        {
            if (_nearbyPlayer == null || _emitter == null || _nearbyInput == null)
                return;

            if (Time.time < _nextInteractTime || !_nearbyInput.ConsumeInteractPressed())
                return;

            if (!TryRaycastThisCrystal())
                return;

            // クールダウンをリセットして、エミッターを発動
            _nextInteractTime = Time.time + _interactCooldown;
            EmitFromPlayerInteraction(_nearbyPlayer.transform);
        }


        /// <summary>
        /// プレイヤー正面へRayを飛ばし、当たったのがこのクリスタルなら true
        /// </summary>
        private bool TryRaycastThisCrystal()
        {
            Transform p = _nearbyPlayer.transform;

            // 原点は足元から少し上,向きはプレイヤー正面
            Ray ray = new Ray(p.position + p.up * 0.5f, p.forward);

            // Triggerは無視して、実体に当たったかで判定
            if (!Physics.Raycast(ray, out RaycastHit hit, _rayMaxDistance, _rayMask, QueryTriggerInteraction.Ignore))
                return false;

            return hit.collider.GetComponentInParent<CrystalInteractable>() == this;
        }


        /// <summary>
        /// プレイヤーがトリガー範囲内に入ったときの処理
        /// </summary>
        /// <param name="other">入ってきたオブジェクト</param>
        private void OnTriggerEnter(Collider other)
        {
            if (_nearbyPlayer != null)
                return;

            _nearbyPlayer = other.GetComponentInParent<PlayerFacade>();
            if (_nearbyPlayer != null)
            {
                _nearbyInput = _nearbyPlayer.GetComponent<PlayerInputReader>();

                _nearbyInput?.ConsumeInteractPressed();
            }
        }


        /// <summary>
        /// プレイヤーがトリガー範囲から出たときの処理
        /// </summary>
        /// <param name="other">出ていったオブジェクト</param>
        private void OnTriggerExit(Collider other)
        {
            PlayerFacade exitingPlayer = other.GetComponentInParent<PlayerFacade>();
            if (exitingPlayer == _nearbyPlayer)
            {
                _nearbyPlayer = null;
                _nearbyInput = null;
            }
        }


        /// <summary>
        /// プレイヤーがトリガー範囲内で入力したときに、クリスタルからかけらを発生させる処理
        /// </summary>
        /// <param name="playerTransform"></param>
        private void EmitFromPlayerInteraction(Transform playerTransform)
        {
            // いくせい型があれば、育った分を一括ばらまき
            if (_oneBreak != null)
            {
                _oneBreak.Emits();
                return;
            }

            Vector3 origin = _hitOrigin != null ? _hitOrigin.position : transform.position;
            Vector3 direction = origin - playerTransform.position;
            direction.y = Mathf.Max(0.25f, direction.y);

            if (direction.sqrMagnitude < 0.01f)
            {
                direction = transform.up;
            }

            _emitter.EmitFromHit(origin, direction.normalized, _interactionPower);
        }

    }
}
