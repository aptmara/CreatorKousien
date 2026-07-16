// ------------------------------------------------------------
// File		: BossThorn.cs
// Summary	: ボスについている棘1本のHP・有効状態・見た目を管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-07-16
//
// Notes	:
// - Bossの棘1本のHP・有効状態・見た目を管理するクラス
// ------------------------------------------------------------
using UnityEngine;
using System;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// ボスに付いている棘1本分の状態を管理するコンポーネント。
    ///
    /// このクラスが担当するもの:
    /// ・棘の最大HPと現在HP
    /// ・戦闘で有効な棘かどうか
    /// ・棘が破壊済みかどうか
    /// ・BoxColliderの有効／無効
    /// ・ブレンドシェイプによる棘の拡大／縮小
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossThorn : MonoBehaviour
    {
        [Header("--- 棘の識別情報 ---")]

        [SerializeField]
        [Min(1)]
        [Tooltip("棘を識別するためのID。Col_Thorn1なら1")]
        private int _thornId = 1;


        [Header("--- 棘の見た目 ---")]

        [SerializeField]
        [Tooltip("棘の見た目を制御するSkinnedMeshRenderer。対応するthorn_1～3を設定")]
        private SkinnedMeshRenderer _thornRenderer;

        [SerializeField]
        [Min(0)]
        [Tooltip("棘の大きさを変更するブレンドシェイプ番号。現在のモデルでは基本的に0を想定。")]
        private int _blendShapeIndex;

        [SerializeField]
        [Tooltip("戦闘で有効な状態のブレンドシェイプ値")]
        private float _activeBlendShapeWeight = 100f;

        [SerializeField]
        [Tooltip("戦闘で無効な状態のブレンドシェイプ値")]
        private float _inactiveBlendShapeWeight;


        [Header("--- 棘の当たり判定 ---")]

        [SerializeField]
        [Tooltip("棘の当たり判定を制御するCollider")]
        private Collider _damageCollider;


        // ランタイム状態
        // ------------------------------------------------------------

        /// <summary>
        /// この棘の最大HP
        /// フェーズ開始時に設定される
        /// </summary>
        private float _maxHp;

        /// <summary>
        /// この棘の現在HP
        /// </summary>
        private float _currentHp;

        /// <summary>
        /// 現在のフェーズ／挑戦で有効な棘かどうか。
        /// falseの場合は、ダメージを受けずColliderも無効になる。
        /// </summary>
        private bool _isCombatActive;

        /// <summary>
        /// HPが0になり、破壊された状態かどうか。
        /// </summary>
        private bool _isBroken;

        /// <summary>
        /// BossControllerから初期設定を受け取ったかどうか。
        /// 初期化前の誤作動を防ぐために使用する。
        /// </summary>
        private bool _isConfigured;


        // 公開プロパティ
        // ------------------------------------------------------------

        /// <summary>
        /// 棘を識別する番号。
        /// </summary>
        public int ThornId => _thornId;

        /// <summary>
        /// 現在HP。
        /// </summary>
        public float CurrentHp => _currentHp;

        /// <summary>
        /// 最大HP。
        /// </summary>
        public float MaxHp => _maxHp;

        /// <summary>
        /// 現在のフェーズ／挑戦で有効な棘かどうか。
        /// </summary>
        public bool IsCombatActive => _isCombatActive;

        /// <summary>
        /// 破壊済みかどうか。
        /// </summary>
        public bool IsBroken => _isBroken;

        /// <summary>
        /// 現在ダメージを受けられる状態かどうか。
        /// </summary>
        public bool IsDamageable => _isCombatActive && !_isBroken && _isConfigured;

        /// <summary>
        /// 現在のHP割合
        /// </summary>
        public float HealthRatio => _maxHp > 0f ? _currentHp / _maxHp : 0f;


        // イベント
        // ------------------------------------------------------------

        /// <summary>
        /// HPが設定または変更されたときに通知する。
        ///
        /// 引数:
        /// 1. HPが変化したBossThorn
        /// 2. 現在HP
        /// 3. 最大HP
        /// </summary>
        public event Action<BossThorn, float, float> HealthChanged;

        /// <summary>
        /// 棘のHPが0になった瞬間に1度だけ通知する。
        /// </summary>
        public event Action<BossThorn> Broken;


        // Unity イベント
        // ------------------------------------------------------------

        /// <summary>
        /// コンポーネントが初めて追加されたときに呼び出される。
        /// </summary>
        private void Reset()
        {
            _damageCollider = GetComponent<Collider>();
        }


        /// <summary>
        /// インスペクターで値が変更されたときに呼び出される。
        /// </summary>
        private void OnValidate()
        {
            _thornId = Mathf.Max(1, _thornId);

            if (_damageCollider == null)
            {
                _damageCollider = GetComponent<Collider>();
            }
        }


        /// <summary>
        /// コンポーネントが有効化されたときに呼び出される。
        /// </summary>
        private void Awake()
        {
            if (_damageCollider == null)
            {
                _damageCollider = GetComponent<Collider>();
            }

            RefreshPresentation();
        }


        // 初期化・状態変更
        // ------------------------------------------------------------

        /// <summary>
        /// フェーズ開始時、または、アングリバイト失敗後の再挑戦時に棘の状態を初期化する
        /// </summary>
        /// <param name="maxHp">この挑戦で使用する棘の最大HP</param>
        /// <param name="isCombatActive">この挑戦で棘が有効かどうか</param>
        public void Configure(float maxHp, bool isCombatActive)
        {
            if (maxHp <= 0f)
            {
                Debug.LogWarning($"BossThorn.Configure: maxHpが0以下の値({maxHp})で呼び出されました。棘ID={_thornId}。棘の最大HPは1に設定されます。");
                maxHp = 1f;
            }

            _maxHp = maxHp;
            _currentHp = maxHp;

            _isCombatActive = isCombatActive;
            _isBroken = false;
            _isConfigured = true;

            RefreshPresentation();

            HealthChanged?.Invoke(this, _currentHp, _maxHp);
        }


        /// <summary>
        /// HPをリセットせずに、戦闘で有効な棘かどうかを切り替える。
        /// </summary>
        /// <param name="isActive"></param>
        public void SetCombatActive(bool isActive)
        {
            if (!_isConfigured)
            {
                Debug.LogWarning($"BossThorn.SetCombatActive: Configureが呼ばれる前にSetCombatActiveが呼ばれました。棘ID={_thornId}。");
            }

            _isCombatActive = isActive;
            RefreshPresentation();
        }


        // ダメージ処理
        // ------------------------------------------------------------

        /// <summary>
        /// 棘にダメージを与える。
        /// </summary>
        /// <param name="damage">与えるダメージ量</param>
        /// <returns>ダメージを適用したかどうか</returns>
        public bool ApplyDamage(float damage)
        {
            if (!IsDamageable)
            {
                return false;
            }

            if (damage <= 0f)
            {
                return false;
            }

            _currentHp = Mathf.Max(0f, _currentHp - damage);

            HealthChanged?.Invoke(this, _currentHp, _maxHp);

            if (_currentHp <= 0f)
            {
                BreakThorn();
            }

            return true;
        }


        /// <summary>
        /// 棘を破壊状態へ移行する
        /// </summary>
        private void BreakThorn()
        {
            if (_isBroken)
            {
                return;
            }

            _isBroken = true;
            _currentHp = 0f;

            RefreshPresentation();

            Broken?.Invoke(this);
        }


        // Colliderとブレンドシェイプの更新
        // ------------------------------------------------------------

        /// <summary>
        /// 現在の論理状態をColliderとブレンドシェイプへ反映する。
        ///
        /// 有効かつ未破壊:
        /// ・Colliderを有効化
        /// ・棘をActive時の大きさにする
        ///
        /// 非Activeまたは破壊済み:
        /// ・Colliderを無効化
        /// ・棘を非Active時の大きさにする
        /// </summary>
        private void RefreshPresentation()
        {
            bool shouldBeActive = _isCombatActive && !_isBroken && _isConfigured;

            if (_damageCollider != null)
            {
                _damageCollider.enabled = shouldBeActive;
            }

            float blendShapeWeight = shouldBeActive ? _activeBlendShapeWeight : _inactiveBlendShapeWeight;

            ApplyBlendShapeWeight(blendShapeWeight);
        }


        /// <summary>
        /// 棘の見た目を現在の状態に合わせて更新する
        /// </summary>
        /// <param name="weight">ブレンドシェイプの値</param>
        private void ApplyBlendShapeWeight(float weight)
        {
            if (_thornRenderer == null)
            {
                return;
            }

            Mesh sharedMesh = _thornRenderer.sharedMesh;
            if (sharedMesh == null)
            {
                Debug.LogWarning($"BossThorn.ApplyBlendShapeWeight: SkinnedMeshRendererのsharedMeshがnullです。棘ID={_thornId}。");
                return;
            }

            if (_blendShapeIndex < 0 || _blendShapeIndex >= sharedMesh.blendShapeCount)
            {
                Debug.LogWarning($"BossThorn.ApplyBlendShapeWeight: ブレンドシェイプインデックス({_blendShapeIndex})が範囲外です。棘ID={_thornId}。");
                return;
            }

            _thornRenderer.SetBlendShapeWeight(_blendShapeIndex, weight);
        }
    }
}

