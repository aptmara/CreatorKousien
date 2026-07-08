// ------------------------------------------------------------
// File		: PlayerAttachmentController.cs
// Summary	: プレイヤーのアタッチメントを管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6 : ベース作成
// - 5/24: アタッチメントの拡大縮小機能の作成
// - 6/19: PlayerRuntimeDataを参照、アタッチメントのサイズをステータスに基づいて変化させる機能の追加
// - 7/8 : エフェクト出てから腕出す感じで作ってみる！
// ------------------------------------------------------------
using UnityEngine;
using Game.Gameplay.Player.Progression;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// プレイヤーのアタッチメントを管理するクラス
    /// </summary>
    public class PlayerAttachmentController : MonoBehaviour
    {
        // 変数宣言
        // ------------------------------------------------------------
        [Header("アタッチメント設定")]
        [Tooltip("装備するアタッチメントのプレハブ")]
        [SerializeField] private PhysicalAttachment _attachmentPrefab;

        [Tooltip("アタッチメントの装着ポイント")]
        [SerializeField] private Transform _attachmentSocket;

        [Header("アタッチメントのスケール設定")]
        [Tooltip("アタッチメントの通常スケール")]
        [SerializeField] private Vector3 _normalScale = Vector3.one;

        [Tooltip("アタッチメントの拡大スケール")]
        [SerializeField] private Vector3 _shrinkScale = new Vector3(0.45f, 0.45f, 0.45f);

        [Tooltip("アタッチメントの拡大縮小にかかる時間")]
        [SerializeField] private float _scaleSpeed = 12f;


        [Header("開閉設定")]
        [Tooltip("開閉を切り替えられる最短間隔(連打対策)")]
        [SerializeField] private float _toggleCooldown = 0.3f;

        [Tooltip("エフェクトを見せてから腕が大きくなり始めるまでの溜め時間")]
        [SerializeField] private float _expandDelay = 0.05f;

        [Tooltip("出現エフェクトを出す位置(左手)")]
        [SerializeField] private Transform _startVfxPointL;

        [Tooltip("出現エフェクトを出す位置(右手)")]
        [SerializeField] private Transform _startVfxPointR;


        private float _nextToggleTime;                  ///< 次に開閉を切り替えられる時間
        private float _expandStartTime;                 ///< 腕が大きくなり始める時刻

        private PhysicalAttachment _currentAttachment;  ///< 現在装備しているアタッチメント


        /// <summary>
        /// 現在装備しているアタッチメントを取得する
        /// </summary>
        public PhysicalAttachment CurrentAttachment => _currentAttachment;


        private Vector3 _targetScale;                   ///< アタッチメントの目標スケール

        private PlayerRuntimeData _runtimeData;         ///< プレイヤーのランタイムデータ（強化のアタッチメントのサイズ倍率）
        private bool _isShrunkInternal;                 ///< 内部的な拡大縮小状態のフラグ（SetShrunk()で更新される）
        private float _forceLargeUntil;                 ///< 強制的に拡大状態にする時間（0以下なら強制拡大状態ではない）
        private bool _forceLargeByPunch;                ///< パンチによる強制拡大状態のフラグ（trueなら強制拡大状態、falseなら通常状態）

        // 関数処理
        // ------------------------------------------------------------
        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Start()
        {
            _targetScale = _normalScale;

            // 初期化処理
            SpawnAttachment();
        }


        /// <summary>
        /// 更新処理 - アタッチメントのスケールを滑らかに変化させる
        /// </summary>
        private void Update()
        {
            if (_currentAttachment == null)
            {
                return;
            }

            // 強化によるサイズ倍率（RuntimeData未注入時は1.0として扱う）
            float upgradeScale = _runtimeData != null ? _runtimeData.AttachmentScaleMultiplier : 1f;

            // 基準スケール（通常 or 縮小）に強化倍率を掛けたものを目標にする
            bool forceLarge = _forceLargeByPunch || Time.time < _forceLargeUntil;
            bool expandReady = Time.time >= _expandStartTime;
            Vector3 baseScale = ((_isShrunkInternal && expandReady) || forceLarge) ? _shrinkScale : _normalScale;
            _targetScale = baseScale * upgradeScale;

            _currentAttachment.transform.localScale = Vector3.Lerp(
                    _currentAttachment.transform.localScale,
                    _targetScale,
                    Time.deltaTime * _scaleSpeed
                );
        }


        /// <summary>
        /// アタッチメントを生成してセットアップする
        /// </summary>
        public void SpawnAttachment()
        {
            if (_attachmentPrefab == null || _attachmentSocket == null)
            {
                Debug.LogWarning("[PlayerAttachmentController] PrefabまたはSocketが設定されていません！");
                return;
            }

            // 1. Prefabを生成する（この時点ではプレイヤーの子にはせず、独立したオブジェクトとして生成）
            _currentAttachment = Instantiate(_attachmentPrefab, _attachmentSocket.position, _attachmentSocket.rotation);
            _currentAttachment.transform.localScale = _normalScale;         // 初期スケールを設定
            _targetScale = _normalScale;                                    // 目標スケールも初期スケールに設定

            // 2. 生成したアタッチメントに、追従先となるソケット（目印）を教える
            _currentAttachment.Initialize(_attachmentSocket);

            Debug.Log("[PlayerAttachmentController] アタッチメントの生成と紐付けが完了しました。");
        }


        /// <summary>
        /// 強化倍率を読むためのRuntimeDataをセットする。PlayerFacadeから呼ばれる。
        /// </summary>
        public void SetRuntimeData(PlayerRuntimeData runtimeData)
        {
            _runtimeData = runtimeData;
        }


        /// <summary>
        /// アタッチメントのスケールを更新する関数
        /// </summary>
        /// <param name="shrunk">拡大縮小状態のフラグ</param>
        public void SetShrunk(bool shrunk)
        {
            // 状態が変わらないなら何もしない
            if (shrunk == _isShrunkInternal)
            {
                return;
            }

            // クールダウン中は切り替えを受け付けない
            if (Time.time < _nextToggleTime)
            {
                return;
            }

            _isShrunkInternal = shrunk;
            _nextToggleTime = Time.time + _toggleCooldown;

            // 開く方向なら、先にエフェクトを見せて腕の拡大は溜め時間だけ遅らせる
            if (shrunk)
            {
                _expandStartTime = Time.time + _expandDelay;
                PlayAuraStart();
            }
        }


        /// <summary>
        /// アタッチメントの出現エフェクトの再生
        /// </summary>
        private void PlayAuraStart()
        {
            if (_currentAttachment != null && _currentAttachment.TryGetComponent<BigHandAuraController>(out var aura))
            {
                // aura.PlayStartEffect(_startVfxPointL, _startVfxPointR);
            }
        }



        /// <summary>
        /// プレイヤーが破棄されるとき、独立しているアタッチメントも一緒に破棄する
        /// </summary>
        private void OnDestroy()
        {
            if (_currentAttachment != null)
            {
                Destroy(_currentAttachment.gameObject);
            }
        }


        /// <summary>
        /// アタッチメントを強制的に拡大状態にする
        /// </summary>
        /// <param name="duration">指定時間</param>
        public void ForceLargeFor(float duration)
        {
            _forceLargeUntil = Mathf.Max(_forceLargeUntil, Time.time + duration);
        }


        /// <summary>
        /// アタッチメントをパンチによる強制拡大状態にするかどうかを設定する
        /// </summary>
        /// <param name="forceLarge">腕の強制拡大状態フラグ</param>
        public void SetPunchForceLarge(bool forceLarge)
        {
            _forceLargeByPunch = forceLarge;
        }
    }
}
