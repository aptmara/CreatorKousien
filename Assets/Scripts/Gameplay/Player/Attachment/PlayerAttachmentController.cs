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
// - 7/16: ゲームオーバー演出用のForceDestroyAttachment()を追加 - Iwai
// ------------------------------------------------------------
using UnityEngine;
using Game.Gameplay.Player.Progression;
using System.Collections;
using Game.Core.Events;
using Game.Core.Roguelike;

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


        [Header("クリア演出")]
        [Tooltip("クリア時の生成する専用の腕も出る")]
        [SerializeField] private GameObject _clearAttachmentPrefab;

        [Tooltip("既存の腕を閉じるのにかける時間")]
        [SerializeField] private float _clearCloseDuration = 0.25f;

        [Tooltip("クリア専用腕の生成オフセット")]
        [SerializeField] private Vector3 _clearSpawnOffset = new Vector3(0f, 0.5f, -0.5f);

        [Tooltip("クリア専用腕を出してからアニメ再生までの時間")]
        [SerializeField] private float _clearAnimStartDelay = 1.0f;



        private GameObject _clearAttachmentInstance;    ///< 生成したクリア専用腕

        private Coroutine _clearRoutine;                ///< クリア演出コルーチン



        private static readonly int ClearHash = Animator.StringToHash("Clear");     ///< クリア時のアニメーションのハッシュ値
        private Animator _attachmentAnimator;                                       ///< アタッチメントのAnimatorコンポーネント
        private bool _isClearPlaying;                                               ///< クリア時のアニメーション再生中かどうかのフラグ


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
        private float _defenseLineHpRatio = 1f;

        private void OnEnable()
        {
            EventBus.Subscribe<DefenseLineHealthChangedEvent>(OnDefenseLineHealthChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DefenseLineHealthChangedEvent>(OnDefenseLineHealthChanged);
        }

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
            if (_isClearPlaying || _currentAttachment == null)
            {
                return;
            }

            // 強化によるサイズ倍率
            float upgradeScale = _runtimeData != null ? _runtimeData.AttachmentScaleMultiplier : 1f;

            bool forceLarge = _forceLargeByPunch || Time.time < _forceLargeUntil;
            bool expandReady = Time.time >= _expandStartTime;

            bool useExpandedScale = (_isShrunkInternal && expandReady) || forceLarge;

            // 入力中または強制拡大中は大きいスケール、未入力時はベース縮小スケール
            Vector3 baseScale = useExpandedScale ? _shrinkScale : _normalScale;

            // 入力していない時だけ強化倍率を乗せない。
            // 入力中/強制拡大中は腕強化を反映する。
            float pinchRatio = Mathf.InverseLerp(0.25f, 0f, _defenseLineHpRatio);
            float pinchMultiplier = Mathf.Lerp(
                1f,
                RoguelikeUpgradeRuntime.PinchAttachmentMultiplier,
                pinchRatio);
            float finalScaleMultiplier = useExpandedScale ? upgradeScale * pinchMultiplier : 1f;

            _targetScale = baseScale * finalScaleMultiplier;

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
            _attachmentAnimator = _currentAttachment.GetComponentInChildren<Animator>();

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

        private void OnDefenseLineHealthChanged(DefenseLineHealthChangedEvent ev)
        {
            _defenseLineHpRatio = ev.Ratio;
        }


        /// <summary>
        /// 既存の腕をとじる
        /// </summary>
        public void RetractAttachmentForClear()
        {
            if (_clearRoutine != null)
            {
                StopCoroutine(_clearRoutine);
            }
            _clearRoutine = StartCoroutine(RetractRoutine());
        }


        private IEnumerator RetractRoutine()
        {
            _isClearPlaying = true;

            _forceLargeByPunch = false;
            _forceLargeUntil = 0f;
            _isShrunkInternal = false;

            // 既存の腕を閉じてからしまう
            if (_currentAttachment != null)
            {
                Vector3 startScale = _currentAttachment.transform.localScale;
                float elapsed = 0f;

                while (elapsed < _clearCloseDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(elapsed / _clearCloseDuration);
                    _currentAttachment.transform.localScale = Vector3.Lerp(startScale, _normalScale, k);

                    yield return null;
                }

                _currentAttachment.gameObject.SetActive(false);
            }

            _clearRoutine = null;
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

            if (_clearAttachmentInstance != null)
            {
                Destroy(_clearAttachmentInstance);
            }
        }

        /// <summary>
        /// ゲームオーバー用: アタッチメントとそのオーラエフェクトを強制的に破棄する
        /// </summary>
        public void ForceDestroyAttachment()
        {
            if (_clearRoutine != null)
            {
                StopCoroutine(_clearRoutine);
                _clearRoutine = null;
            }

            if (_currentAttachment != null)
            {
                Destroy(_currentAttachment.gameObject);
                _currentAttachment = null;
            }

            if (_clearAttachmentInstance != null)
            {
                Destroy(_clearAttachmentInstance);
                _clearAttachmentInstance = null;
            }

            _isClearPlaying = false;
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
        /// 腕のクリアアニメーション再生
        /// </summary>
        public void PlayClearAnimation()
        {
            if (_clearRoutine != null)
            {
                StopCoroutine(_clearRoutine);
            }
            _clearRoutine = StartCoroutine(ClearSpawnRoutine());
        }



        private IEnumerator ClearSpawnRoutine()
        {
            _isClearPlaying = true;

            // 念のため既存腕が出てたらしまう
            if (_currentAttachment != null && _currentAttachment.gameObject.activeSelf)
            {
                _currentAttachment.gameObject.SetActive(false);
            }

            if (_clearAttachmentPrefab != null && _attachmentSocket != null)
            {
                Vector3 spawnPos = _attachmentSocket.position + _attachmentSocket.rotation * _clearSpawnOffset;
                _clearAttachmentInstance = Instantiate(_clearAttachmentPrefab, spawnPos, _attachmentSocket.rotation);

                var animator = _clearAttachmentInstance.GetComponentInChildren<Animator>();

                // 腕をしまいきる猶予として少し待つ
                yield return new WaitForSecondsRealtime(_clearAnimStartDelay);

                if (animator != null)
                {
                    animator.ResetTrigger(ClearHash);
                    animator.SetTrigger(ClearHash);
                }
            }

            _clearRoutine = null;
        }



        /// <summary>
        /// アタッチメントをパンチによる強制拡大状態にするかどうかを設定する
        /// </summary>
        /// <param name="forceLarge">腕の強制拡大状態フラグ</param>
        public void SetPunchForceLarge(bool forceLarge)
        {
            _forceLargeByPunch = forceLarge;
        }


        /// <summary>
        /// クリア演出の準備を行う
        /// </summary>
        public void PrepareClearAttachment()
        {
            // クリア演出の準備
            if (_clearRoutine != null)
            {
                StopCoroutine(_clearRoutine);
                _clearRoutine = null;
            }

            // クリア演出中フラグを立てる
            _isClearPlaying = true;

            if (_currentAttachment != null)
            {
                _currentAttachment.gameObject.SetActive(false);
            }

            // クリア専用腕の生成
            if (_clearAttachmentInstance == null && _clearAttachmentPrefab != null && _attachmentSocket != null)
            {
                Vector3 spawnPos = _attachmentSocket.position + _attachmentSocket.rotation * _clearSpawnOffset;
                _clearAttachmentInstance = Instantiate(_clearAttachmentPrefab, spawnPos, _attachmentSocket.rotation);
            }

            // クリア専用腕を非表示にする
            if (_clearAttachmentInstance != null)
            {
                _clearAttachmentInstance.SetActive(false);
            }
        }


        /// <summary>
        /// クリア演出のアニメーションを再生する
        /// </summary>
        public void PlayPreparedClearAnimation()
        {
            if (_clearAttachmentInstance == null)
            {
                return;
            }

            _clearAttachmentInstance.SetActive(true);

            // クリア専用腕を表示する
            Animator animator = _clearAttachmentInstance.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                return;
            }

            animator.ResetTrigger(ClearHash);
            animator.Play("Clear", 0, 0f);
            animator.Update(0f);
        }


        public void RestoreFromClear()
        {
            // 再生途中のクリア演出コルーチンを止める
            if (_clearRoutine != null)
            {
                StopCoroutine(_clearRoutine);
                _clearRoutine = null;
            }

            // クリア演出中フラグを下ろす
            if (_clearAttachmentInstance != null)
            {
                Destroy(_clearAttachmentInstance);
                _clearAttachmentInstance = null;
            }

            // 通常の腕は状態が崩れているので、作り直す
            if (_currentAttachment != null)
            {
                Destroy(_currentAttachment.gameObject);
                _currentAttachment = null;
            }

            _isClearPlaying = false;
            _isShrunkInternal = false;
            _forceLargeByPunch = false;
            _forceLargeUntil = 0f;
            _nextToggleTime = 0f;
            _expandStartTime = 0f;

            SpawnAttachment();
        }
    }
}
