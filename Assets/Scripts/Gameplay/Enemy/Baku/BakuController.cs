// ------------------------------------------------------------
// File		: BakuController.cs
// Summary	: 敵「バク」の本体制御クラス
//
// Author	: [浅野勇生]
// Created	: 2026-08-22
//
// Notes	:
// - ベース作成
// ------------------------------------------------------------
using System.Collections;
using Game.Core.Enemy;
using Game.Data.Collectibles;
using Game.Gameplay.Collectibles;
using UnityEngine;

namespace Game.Gameplay.Enemy.Baku
{
    /// <summary>
    /// バクの固有ギミック。EnemyBodyのPrefab側にアタッチする
    /// </summary>
    public class BakuController : MonoBehaviour
    {
        [Header("データ")]
        [SerializeField] private BakuData _data;

        [Header("参照")]
        [Tooltip("口のTrigger。未設定なら子から自動取得。")]
        [SerializeField] private BakuMouth _mouth;
        [Tooltip("膨張表示。未設定なら子から自動取得。")]
        [SerializeField] private BakuBellyView _belly;
        [Tooltip("捕食・破裂モーション用のAnimator。未設定なら自動取得。")]
        [SerializeField] private Animator _animator;

        [Header("Animatorパラメータ名")]
        [SerializeField] private string _eatTriggerName = "Eat";
        [SerializeField] private string _burstTriggerName = "Burst";


        // --- 内部変数 ---
        private EnemyController _enemyController;
        private EnemyRising _rising;
        private readonly BakuStomach _stomach = new BakuStomach();

        private bool _isInitialized;
        private bool _isBursting;
        private bool _isMovePaused;
        private float _eatPauseTimer;
        private float _nextEatableTime;

        private int _eatTriggerHash;
        private int _burstTriggerHash;


        // ライフサイクル
        // ------------------------------------------------------------

        private void Awake()
        {
            if (_mouth == null)
            {
                _mouth = GetComponentInChildren<BakuMouth>(true);
            }
            if (_belly == null)
            {
                _belly = GetComponentInChildren<BakuBellyView>(true);
            }
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>(true);
            }

            _eatTriggerHash = Animator.StringToHash(_eatTriggerName);
            _burstTriggerHash = Animator.StringToHash(_burstTriggerName);

            if (_mouth != null)
            {
                _mouth.CollectibleEntered += HandleCollectibleEntered;
            }
        }


        private void Start()
        {
            // EnemyControllerとEnemyRisingはEnemySpawnerがセットするので、Startで取得
            TryResolveEnemyComponents();
        }


        private void OnDestroy()
        {
            if (_mouth != null) _mouth.CollectibleEntered -= HandleCollectibleEntered;
        }

        private void OnDisable()
        {
            // 一時停止を残したまま無効化されると敵が動かなくなるため、必ず解除する。
            CancelMovePause();
        }


        private void TryResolveEnemyComponents()
        {
            if (_isInitialized)
                return;

            if (_data == null)
            {
                Debug.LogError("[BakuController] BakuDataが未設定です。", this);
                enabled = false;
                return;
            }

            _enemyController = GetComponentInParent<EnemyController>();
            _rising = GetComponentInParent<EnemyRising>();

            if (_enemyController == null || _rising == null)
            {
                Debug.LogWarning("[BakuController] EnemyControllerまたはEnemyRisingが見つかりません。EnemySpawnerで生成されたPrefabにアタッチしてください。", this);
                return;
            }

            _stomach.Initialize(_data.MaxEatCount, HandleFillChanged, HandleOverfed);

            if (_belly != null)
            {
                _belly.Initialize(_data.MaxBellyScale, _data.BellyScaleCurve, _data.BellyScaleLerpTime);
            }

            // 上昇中は常に口を開けている
            if (_mouth != null) _mouth.SetOpen(true);

            _isInitialized = true;
        }


        private void Update()
        {
            if (!_isInitialized)
            {
                TryResolveEnemyComponents();
                return;
            }

            // ダウン・撃破など通常状態を抜けたら、捕食の一時停止を必ず解除する！
            if (!IsNormal())
            {
                CancelMovePause();
                if (_mouth != null && !_isMovePaused)
                    _mouth.SetOpen(false);

                return;
            }

            if (_mouth != null && !_isBursting)
                _mouth.SetOpen(true);

            if (_eatPauseTimer > 0f)
            {
                _eatPauseTimer -= Time.deltaTime;
                if (_eatPauseTimer <= 0f)
                {
                    _eatPauseTimer = 0f;
                    CancelMovePause();
                }
            }
        }


        // 捕食
        // ------------------------------------------------------------

        private void HandleCollectibleEntered(CollectibleObject collectible)
        {
            if (!_isInitialized || _isBursting || collectible == null)
                return;

            if (!IsNormal())
                return;

            if (Time.time < _nextEatableTime)
                return;

            CollectibleData data = collectible.GetCollectableData();
            if (data == null) return;

            // 食べられない種類は無視する
            if (!_data.EatableType.Contains(data.Type))
                return;

            if (!_stomach.TryEat())
                return;

            // Despawn()はCollectiblePool.Return()経由でSetActive(false)になるので、ここでDestroy()してはいけない
            collectible.Despawn();

            _nextEatableTime = Time.time + _data.EatCooldown;

            // 食べ過ぎで破裂へ入った場合は、捕食モーションをスキップして破裂処理へ移行する
            if (_isBursting)
                return;

            BeginEatPause();
        }


        private void BeginEatPause()
        {
            _eatPauseTimer = _data.EatPauseDuration;
            ApplyMovePause();

            if (_animator != null)
            {
                _animator.SetTrigger(_eatTriggerHash);
            }

            if (_data.EatPauseDuration <= 0f)
            {
                _eatPauseTimer = 0f;
                CancelMovePause();
            }
        }


        private void HandleFillChanged(float fillRatio)
        {
            if (_belly != null)
            {
                _belly.SetFill(fillRatio);
            }
        }


        // バ・ク・レ・ツ
        // ------------------------------------------------------------

        private void HandleOverfed()
        {
            if (_isBursting)
                return;

            _isBursting = true;
            _eatPauseTimer = 0f;

            if (_mouth != null)
            {
                _mouth.SetOpen(false);
            }

            if (_animator != null)
            {
                _animator.SetTrigger(_burstTriggerHash);
            }

            // 予兆の間は動きは止める
            ApplyMovePause();

            // 破裂処理を遅延実行
            StartCoroutine(BurstRoutine());
        }



        private IEnumerator BurstRoutine()
        {
            if (_data.BurstDelay > 0f)
            {
                yield return new WaitForSeconds(_data.BurstDelay);
            }

            Vector3 burstPosition = transform.position;

            // 1. 周囲の敵へ範囲ダメージ
            int hitCount = BakuBurstResolver.ApplyBurst(_enemyController, burstPosition, _data.BurstRadius, _data.BurstDamage);

            // 2. VFX
            if (_data.BurstVfxPrefab != null)
            {
                GameObject vfx = Instantiate(_data.BurstVfxPrefab, burstPosition, Quaternion.identity);
                if (_data.BurstVfxLifetime > 0f)
                {
                    Destroy(vfx, _data.BurstVfxLifetime);
                }
            }

            Debug.Log($"[BakuController] 破裂。巻き込んだ敵={hitCount}体, damage={_data.BurstDamage}, radius={_data.BurstRadius}");

            // 3. 自身を破壊
            CancelMovePause();

            if (_enemyController != null)
            {
                _enemyController.OnBodyHit(float.MaxValue); // 体力を0にする
            }
        }


        // 移動の一時停止
        // ------------------------------------------------------------

        private void ApplyMovePause()
        {
            if (_isMovePaused || _rising == null)
                return;

            _isMovePaused = true;
            _rising.PauseMove();
        }


        private void CancelMovePause()
        {
            if (!_isMovePaused || _rising == null)
                return;

            _isMovePaused = false;
            _eatPauseTimer = 0f;
            _rising.UnpauseMove();
        }


        private bool IsNormal()
        {
            return _enemyController != null && _enemyController.CurrentState == EnemyState.Normal;
        }


        // デバッグ表示
        // ------------------------------------------------------------

        private void OnDrawGizmosSelected()
        {
            if (_data == null)
                return;

            Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, _data.BurstRadius);
        }
    }

}
