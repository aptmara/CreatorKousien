// ------------------------------------------------------------
// File		: PlayerAttachmentController.cs
// Summary	: プレイヤーのアタッチメントを管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6: ベース作成
// - 5/24: アタッチメントの拡大縮小機能の作成
// - 6/19: PlayerRuntimeDataを参照、アタッチメントのサイズをステータスに基づいて変化させる機能の追加
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
            Vector3 baseScale = (_isShrunkInternal || forceLarge) ? _shrinkScale : _normalScale;
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
            _isShrunkInternal = shrunk;
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
