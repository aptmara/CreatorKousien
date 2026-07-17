using Game.Core.Events;
using UnityEngine;

namespace Game.Gameplay.Collectibles
{
    /// <summary>
    /// EventBus上の撃破・コンボ終了イベントを監視し、既存のCollectibleSpawnerへ生成要求だけを中継する。
    /// </summary>
    public sealed class CollectibleEventDropEmitter : MonoBehaviour
    {
        [Header("参照設定")]
        [Tooltip("Collectible生成を委譲する既存Spawner。未設定時はAwakeで同一GameObjectから取得する。")]
        [SerializeField] private CollectibleSpawner _spawner;

        [Tooltip("敵撃破時のCollectible生成を委譲するCrystalWalk。実行時生成のため、未設定時はイベント受信時に検索します。")]
        [SerializeField] private CrystalWalk _crystalWalk;

        [Header("敵撃破ドロップ設定")]
        [Tooltip("敵撃破時に生成するCollectible数の基礎値です。")]
        [SerializeField, Min(0)] private int _enemyDefeatBaseSpawnCount = 3;

        [Tooltip("敵撃破時の基礎値へ掛ける倍率です。")]
        [SerializeField, Min(0f)] private float _enemyDefeatSpawnMultiplier = 1f;

        [Tooltip("敵撃破時に実際に生成される個数です。基礎値と倍率から自動計算されます。")]
        [SerializeField, Min(0)] private int _enemyDefeatActualSpawnCount = 3;

        [Tooltip("敵撃破時の生成位置を、CrystalWalkからプレイヤー方向へ内側に寄せる距離です。")]
        [SerializeField, Min(0f)] private float _enemyDefeatSpawnInwardOffset = 2f;

        [Header("コンボ閾値ドロップ設定")]
        [Tooltip("このコンボ数へ到達するたびにCollectibleを生成します。")]
        [SerializeField, Min(1)] private int _requiredComboCount = 100;

        [Tooltip("必要コンボ数へ掛ける生成数の基礎値です。")]
        [SerializeField, Min(0f)] private float _comboSpawnBaseValue = 0.3f;

        [Tooltip("必要コンボ数と基礎値から求めた生成数へ掛ける外部倍率です。")]
        [SerializeField, Min(0f)] private float _comboEndSpawnMultiplier = 1f;

        [Tooltip("必要コンボ数へ到達するたびに実際に生成される個数です。自動計算されます。")]
        [SerializeField, Min(0)] private int _comboThresholdActualSpawnCount = 30;

        private int _completedComboThresholdCount;

        /// <summary>
        /// 敵撃破時の生成数倍率を外部から設定します。
        /// </summary>
        /// <param name="multiplier">敵撃破時の基礎値へ掛ける倍率です。</param>
        public void SetEnemyDefeatSpawnMultiplier(float multiplier)
        {
            _enemyDefeatSpawnMultiplier = Mathf.Max(0f, multiplier);
            RefreshActualSpawnCounts();
        }

        /// <summary>
        /// コンボ終了時の生成数倍率を外部から設定します。
        /// </summary>
        /// <param name="multiplier">コンボ終了時の基礎値へ掛ける倍率です。</param>
        public void SetComboEndSpawnMultiplier(float multiplier)
        {
            _comboEndSpawnMultiplier = Mathf.Max(0f, multiplier);
            RefreshActualSpawnCounts();
        }

        public void SetRequiredComboCount(int requiredComboCount)
        {
            _requiredComboCount = Mathf.Max(1, requiredComboCount);
            RefreshActualSpawnCounts();
        }

        /// <summary>
        /// Inspector上の設定変更を実生成数へ反映します。
        /// </summary>
        private void OnValidate()
        {
            RefreshActualSpawnCounts();
        }

        /// <summary>
        /// 参照未設定時に同一GameObject上のCollectibleSpawnerを自動補完する。
        /// </summary>
        private void Awake()
        {
            RefreshActualSpawnCounts();

            if (_spawner == null)
            {
                _spawner = GetComponent<CollectibleSpawner>();
            }
        }

        /// <summary>
        /// 必要なイベント購読を開始する。
        /// </summary>
        private void OnEnable()
        {
            EventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            EventBus.Subscribe<ComboChangedEvent>(OnComboChanged);
        }

        /// <summary>
        /// 有効状態解除時に必ずイベント購読を解除する。
        /// </summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            EventBus.Unsubscribe<ComboChangedEvent>(OnComboChanged);
        }

        /// <summary>
        /// 敵撃破イベントを受け取り、設定数に応じてCollectible生成を中継する。
        /// </summary>
        /// <param name="ev">敵撃破イベント。</param>
        private void OnEnemyDefeated(EnemyDefeatedEvent _)
        {
            if (_enemyDefeatActualSpawnCount <= 0)
            {
                return;
            }

            ResolveCrystalWalkIfNeeded();
            if (_crystalWalk == null)
            {
                Debug.LogWarning("[CollectibleEventDropEmitter] EnemyDefeatedEvent を受信しましたが、CrystalWalk が見つからないため Collectible を生成できません。", this);
                return;
            }

            _crystalWalk.EmitInwardHitStyleCollectibles(
                _enemyDefeatActualSpawnCount,
                _enemyDefeatSpawnInwardOffset);
        }

        /// <summary>
        /// 実行時に生成されるCrystalWalkの参照を必要なときだけ取得します。
        /// </summary>
        private void ResolveCrystalWalkIfNeeded()
        {
            if (_crystalWalk == null)
            {
                _crystalWalk = FindFirstObjectByType<CrystalWalk>();
            }
        }

        /// <summary>
        /// コンボ数の更新を受け取り、新しく到達した閾値回数分のCollectible生成を中継する。
        /// </summary>
        /// <param name="ev">コンボ更新イベント。</param>
        private void OnComboChanged(ComboChangedEvent ev)
        {
            if (ev.CurrentCombo <= 0)
            {
                _completedComboThresholdCount = 0;
                return;
            }

            int reachedThresholdCount = ev.CurrentCombo / Mathf.Max(1, _requiredComboCount);
            int newlyReachedThresholdCount = reachedThresholdCount - _completedComboThresholdCount;
            if (newlyReachedThresholdCount <= 0 || _comboThresholdActualSpawnCount <= 0)
            {
                return;
            }

            ResolveCrystalWalkIfNeeded();
            if (_crystalWalk == null)
            {
                Debug.LogWarning("[CollectibleEventDropEmitter] コンボ閾値へ到達しましたが、CrystalWalk が見つからないため Collectible を生成できません。", this);
                return;
            }

            _completedComboThresholdCount = reachedThresholdCount;
            _crystalWalk.EmitHitStyleCollectibles(
                _comboThresholdActualSpawnCount * newlyReachedThresholdCount);
        }

        /// <summary>
        /// 基礎値と倍率から各イベントの実生成数を更新します。
        /// </summary>
        private void RefreshActualSpawnCounts()
        {
            _enemyDefeatActualSpawnCount = CalculateActualSpawnCount(
                _enemyDefeatBaseSpawnCount,
                _enemyDefeatSpawnMultiplier);
            _comboThresholdActualSpawnCount = Mathf.FloorToInt(
                Mathf.Max(1, _requiredComboCount)
                * Mathf.Max(0f, _comboSpawnBaseValue)
                * Mathf.Max(0f, _comboEndSpawnMultiplier));
        }

        /// <summary>
        /// 基礎値と倍率から実生成数を計算します。
        /// </summary>
        /// <param name="baseCount">生成数の基礎値です。</param>
        /// <param name="multiplier">基礎値へ掛ける倍率です。</param>
        /// <returns>小数部分を切り捨てた0以上の実生成数です。</returns>
        private static int CalculateActualSpawnCount(int baseCount, float multiplier)
        {
            return Mathf.FloorToInt(Mathf.Max(0, baseCount) * Mathf.Max(0f, multiplier));
        }

        /// <summary>
        /// 生成中継の事前条件を検証する。
        /// </summary>
        /// <param name="eventName">発火元イベント名。</param>
        /// <param name="spawnCount">生成予定数。</param>
        /// <returns>生成可能な場合はtrue。</returns>
        private bool CanEmitDrop(string eventName, int spawnCount)
        {
            if (_spawner == null)
            {
                Debug.LogWarning($"[CollectibleEventDropEmitter] {eventName} を受信しましたが、CollectibleSpawner が未設定のため Collectible を生成できません。", this);
                return false;
            }

            if (spawnCount <= 0)
            {
                return false;
            }

            return true;
        }
    }
}
