// ------------------------------------------------------------
// File		: BakuRegen.cs
// Summary	: 吸収した分を時間経過でだんだんHPへ還元するバク固有のギミック
//
// Author	: [浅野 勇生]
// Created	: 2026-09-14
//
// Notes	:
// - BakuControllerのCollectibleEatenを受けて回復ストックを溜める
// - 溜まったストックを立ち上がりカーブに沿って徐々に消費しHPへ変換する
// ------------------------------------------------------------
using Game.Core.Enemy;
using Game.Gameplay.Collectibles;
using UnityEngine;

namespace Game.Gameplay.Enemy.Baku
{
    /// <summary>
    /// 吸収した落し物を時間経過でHPへ還元する
    /// BakuBodyのPrefabへBakuControllerと一緒にアタッチする
    /// </summary>
    [RequireComponent(typeof(BakuController))]
    public class BakuRegen : MonoBehaviour
    {
        [Header("--- 吸収 ---")]
        [Tooltip("1回吸収するごとに増える回復ストック量[HP]")]
        [SerializeField, Min(0f)] private float _healPerAbsorb = 40f;

        [Tooltip("溜められる回復ストックの上限[HP]")]
        [SerializeField, Min(0f)] private float _maxHealStock = 200f;

        [Header("--- 回復の立ち上がり ---")]
        [Tooltip("吸収してから回復が始まるまでの待機時間[秒]")]
        [SerializeField, Min(0f)] private float _regenStartDelay = 1.0f;

        [Tooltip("回復速度が最大になるまでの時間[秒]。だんだん速くなる")]
        [SerializeField, Min(0.01f)] private float _rampUpDuration = 3.0f;

        [Tooltip("最大時の毎秒回復量[HP/秒]")]
        [SerializeField, Min(0f)] private float _maxHealPerSecond = 20f;

        [Tooltip("回復速度の立ち上がり方（横軸:経過割合 縦軸:速度倍率0-1）")]
        [SerializeField] private AnimationCurve _rampCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("--- 中断条件 ---")]
        [Tooltip("被弾したら回復を中断し、待機時間と立ち上がりをやり直す")]
        [SerializeField] private bool _resetOnDamage = true;

        [Header("--- 演出 ---")]
        [Tooltip("回復中だけ表示するVFX。未設定なら何もしない")]
        [SerializeField] private GameObject _regenVfxRoot;


        // --- 内部変数 ---
        private BakuController _bakuController;
        private EnemyController _enemyController;

        // 未消化の回復ストック[HP]
        private float _healStock;
        // 回復開始までの残り待機時間[秒]
        private float _waitTimer;
        // 立ち上がりの経過時間[秒]
        private float _rampTimer;
        // 被弾検知用。負値なら未初期化
        private float _lastKnownHp = -1f;

        private bool _isInitialized;


        private void Awake()
        {
            _bakuController = GetComponent<BakuController>();
            SetVfxActive(false);
        }


        private void Start()
        {
            // EnemyControllerはEnemySpawnerが親側へつけるのでStart以降で取得する
            TryResolve();
        }


        private void OnDestroy()
        {
            if (_bakuController != null)
                _bakuController.CollectibleEaten -= HandleEaten;

            if (_enemyController != null)
                _enemyController.OnHealthChanged -= HandleHealthChanged;
        }


        private void TryResolve()
        {
            if (_isInitialized) return;

            _enemyController = GetComponentInParent<EnemyController>();
            if (_enemyController == null || _bakuController == null) return;

            _bakuController.CollectibleEaten += HandleEaten;
            _enemyController.OnHealthChanged += HandleHealthChanged;

            _isInitialized = true;
        }


        private void Update()
        {
            if (!_isInitialized)
            {
                TryResolve();
                return;
            }


            // 通常状態以外
            if (_enemyController.CurrentState != EnemyState.Normal)
            {
                SetVfxActive(false);
                return;
            }

            if (_healStock <= 0f)
            {
                SetVfxActive(false);
                return;
            }

            // 吸収直後の待機
            if (_waitTimer > 0f)
            {
                _waitTimer -= Time.deltaTime;
                SetVfxActive(false);
                return;
            }

            // だんだん速くなる
            _rampTimer = Mathf.Min(_rampTimer + Time.deltaTime, _rampUpDuration);
            float t = _rampUpDuration > 0f ? _rampTimer / _rampUpDuration : 1f;
            float rate = _maxHealPerSecond * Mathf.Clamp01(_rampCurve.Evaluate(t));

            float heal = Mathf.Min(rate * Time.deltaTime, _healStock);
            if (heal <= 0f)
                return;

            _healStock -= heal;
            _enemyController.HealBody(heal);

            SetVfxActive(true);
        }


        /// <summary>
        /// 吸収成功時、回復ストックを増やす
        /// </summary>
        /// <param name="collectible"></param>
        private void HandleEaten(CollectibleObject collectible)
        {
            _healStock = Mathf.Min(_healStock + _healPerAbsorb, _maxHealStock);

            // 食べた直後は、1拍おいてから回復させる
            _waitTimer = _regenStartDelay;
        }


        /// <summary>
        /// HP変化を監視し、減っていたら被弾とみなして回復を中断する
        /// </summary>
        /// <param name="current">現在のHP</param>
        /// <param name="max">最大HP</param>
        private void HandleHealthChanged(float current, float max)
        {
            if (_lastKnownHp < 0f)
            {
                _lastKnownHp = current;
                return;
            }

            bool isDamaged = current < _lastKnownHp - 0.0001f;
            _lastKnownHp = current;

            if (!isDamaged || !_resetOnDamage) return;

            _waitTimer = _regenStartDelay;
            _rampTimer = 0f;
            SetVfxActive(false);
        }


        private void SetVfxActive(bool active)
        {
            if (_regenVfxRoot != null)
            {
                if (_regenVfxRoot == null) return;
                if (_regenVfxRoot.activeSelf == active) return;

                _regenVfxRoot.SetActive(active);
            }
        }
    }
}
