// ================================================================================
// File         : CrystalShardEmitter.cs
// Description  : 大小変更可能なクリスタルを殴った対象に応じて欠片を出す
// author       : 山本郁也
// data         : 2026/05/19
// ================================================================================

using System;
using UnityEngine;
using Game.Data.Collectibles;

namespace Game.Gameplay.Collectibles
{
    /// <summary>
    /// Emits collectible shards when a crystal is hit.
    /// The crystal itself is never resized, damaged, destroyed, or despawned.
    /// </summary>
    public sealed class CrystalShardEmitter : MonoBehaviour
    {
        /// <summary>
        /// 欠片の発生数をどの方式で決めるかを表す列挙型。
        /// </summary>
        public enum ShardAmountMode
        {
            /// <summary>
            /// クリスタルの大小を見ず、毎回固定数の欠片を出す。
            /// </summary>
            Fixed,

            /// <summary>
            /// クリスタルサイズごとに設定された範囲から欠片数を決める。
            /// </summary>
            SizeBased
        }

        /// <summary>
        /// サイズ依存モードで使用するクリスタルの大きさ。
        /// </summary>
        public enum CrystalSize
        {
            /// <summary>
            /// 小さいクリスタル。
            /// </summary>
            Small,

            /// <summary>
            /// 中くらいのクリスタル。
            /// </summary>
            Medium,

            /// <summary>
            /// 大きいクリスタル。
            /// </summary>
            Large
        }

        /// <summary>
        /// 欠片発生数の最小値と最大値を保持する設定。
        /// </summary>
        [Serializable]
        private struct ShardCountRange
        {
            /// <summary>
            /// 1 回のヒットで出す欠片数の最小値。必ず 1 以上として扱う。
            /// </summary>
            [Min(1)] public int Min;

            /// <summary>
            /// 1 回のヒットで出す欠片数の最大値。Min より小さい場合は Min と同じ値として扱う。
            /// </summary>
            [Min(1)] public int Max;

            /// <summary>
            /// Min と Max の範囲内から、実際に出す欠片数をランダムに決める。
            /// </summary>
            /// <returns>1 以上の欠片発生数。</returns>
            public int GetRandomCount()
            {
                int min = Mathf.Max(1, Min);
                int max = Mathf.Max(min, Max);
                return UnityEngine.Random.Range(min, max + 1);
            }
        }

        [Header("Spawn API References")]
        /// <summary>
        /// 欠片本体の取得に使う既存 Pool API。未設定ならシーン内から自動検索する。
        /// </summary>
        [SerializeField] private CollectiblePool _pool;

        /// <summary>
        /// 生成した欠片をアクティブ管理へ登録する既存 Registry API。未設定ならシーン内から自動検索する。
        /// </summary>
        [SerializeField] private CollectibleRegistry _registry;

        [Header("Shard Data")]
        /// <summary>
        /// 生成する欠片の種類。複数設定した場合は発生ごとにランダムで 1 つ選ぶ。
        /// </summary>
        [SerializeField] private CollectibleData[] _shardData;

        /// <summary>
        /// true の場合、生成した欠片をプレイヤーが回収できる状態にする。
        /// </summary>
        [SerializeField] private bool _canBeCollectedByPlayer = true;

        [Header("Amount")]
        /// <summary>
        /// 欠片数を固定値にするか、クリスタルサイズに応じて変えるかを指定する。
        /// </summary>
        [SerializeField] private ShardAmountMode _amountMode = ShardAmountMode.Fixed;

        /// <summary>
        /// Fixed モード時に 1 回のヒットで出す欠片数。
        /// </summary>
        [SerializeField, Min(1)] private int _fixedShardCount = 3;

        /// <summary>
        /// SizeBased モード時に参照するクリスタルサイズ。
        /// </summary>
        [SerializeField] private CrystalSize _crystalSize = CrystalSize.Medium;

        /// <summary>
        /// Small サイズ時に出す欠片数の範囲。
        /// </summary>
        [SerializeField] private ShardCountRange _smallCount = new ShardCountRange { Min = 1, Max = 3 };

        /// <summary>
        /// Medium サイズ時に出す欠片数の範囲。
        /// </summary>
        [SerializeField] private ShardCountRange _mediumCount = new ShardCountRange { Min = 3, Max = 6 };

        /// <summary>
        /// Large サイズ時に出す欠片数の範囲。
        /// </summary>
        [SerializeField] private ShardCountRange _largeCount = new ShardCountRange { Min = 6, Max = 10 };

        [Header("Emission")]
        /// <summary>
        /// 欠片の発生基準位置。未設定の場合はヒット位置を使う。
        /// </summary>
        [SerializeField] private Transform _emitOrigin;

        /// <summary>
        /// 発生位置を基準点からどれくらいランダムに散らすか。
        /// </summary>
        [SerializeField, Min(0f)] private float _spawnRadius = 0.25f;

        /// <summary>
        /// 欠片に与える初速の最小値。
        /// </summary>
        [SerializeField, Min(0f)] private float _minLaunchSpeed = 2.0f;

        /// <summary>
        /// 欠片に与える初速の最大値。
        /// </summary>
        [SerializeField, Min(0f)] private float _maxLaunchSpeed = 5.0f;

        /// <summary>
        /// 欠片を上方向へ持ち上げる補正量。
        /// </summary>
        [SerializeField, Min(0f)] private float _upwardBias = 0.75f;

        /// <summary>
        /// 欠片の飛ぶ方向に加えるランダムなばらつき。
        /// </summary>
        [SerializeField, Min(0f)] private float _randomSpread = 0.45f;

        /// <summary>
        /// 欠片に与える角速度の最小値。
        /// </summary>
        [SerializeField, Min(0f)] private float _minAngularSpeed = 2.0f;

        /// <summary>
        /// 欠片に与える角速度の最大値。
        /// </summary>
        [SerializeField, Min(0f)] private float _maxAngularSpeed = 8.0f;

        /// <summary>
        /// 起動時に Pool と Registry の参照を補完する。
        /// </summary>
        private void Awake()
        {
            ResolveReferencesIfNeeded();
        }

        /// <summary>
        /// Inspector の Context Menu から欠片発生を確認するためのデバッグ API。
        /// </summary>
        [ContextMenu("DEBUG: Emit Shards")]
        public void Emit()
        {
            EmitFromHit(GetBasePosition(transform.position), transform.up, 1f);
        }

        /// <summary>
        /// プレイヤーの攻撃判定、またはヒット検知側から呼ぶ欠片発生 API。
        /// </summary>
        /// <param name="hitPoint">クリスタルが殴られたワールド座標。</param>
        /// <param name="hitDirection">欠片を主に飛ばしたいワールド方向。</param>
        /// <param name="power">欠片の飛ぶ速度に掛ける倍率。0.1 未満は 0.1 として扱う。</param>
        public void EmitFromHit(Vector3 hitPoint, Vector3 hitDirection, float power = 1f)
        {
            ResolveReferencesIfNeeded();

            if (!CanEmit())
            {
                return;
            }

            int count = GetShardCount();
            for (int i = 0; i < count; i++)
            {
                EmitOne(hitPoint, hitDirection, power);
            }
        }

        /// <summary>
        /// 欠片を 1 個だけ生成し、位置、見た目データ、初速、角速度、Registry 登録を行う。
        /// </summary>
        /// <param name="hitPoint">発生位置計算に使うヒット座標。</param>
        /// <param name="hitDirection">初速方向計算に使うヒット方向。</param>
        /// <param name="power">初速に掛ける倍率。</param>
        private void EmitOne(Vector3 hitPoint, Vector3 hitDirection, float power)
        {
            CollectibleObject shard = _pool.Get();
            CollectibleData data = _shardData[UnityEngine.Random.Range(0, _shardData.Length)];

            shard.transform.position = CreateSpawnPosition(hitPoint);
            shard.transform.rotation = UnityEngine.Random.rotation;
            shard.Initialize(data, ReturnToPool, _canBeCollectedByPlayer);
            shard.SetInitialMotion(CreateVelocity(hitDirection, power), CreateAngularVelocity());

            _registry.Register(shard);
        }

        /// <summary>
        /// 現在の発生数モードに応じて、今回のヒットで出す欠片数を決める。
        /// </summary>
        /// <returns>1 以上の欠片発生数。</returns>
        private int GetShardCount()
        {
            if (_amountMode == ShardAmountMode.Fixed)
            {
                return Mathf.Max(1, _fixedShardCount);
            }

            switch (_crystalSize)
            {
                case CrystalSize.Small:
                    return _smallCount.GetRandomCount();
                case CrystalSize.Medium:
                    return _mediumCount.GetRandomCount();
                case CrystalSize.Large:
                    return _largeCount.GetRandomCount();
                default:
                    return 1;
            }
        }

        /// <summary>
        /// 欠片を実際に配置するワールド座標を作る。
        /// </summary>
        /// <param name="hitPoint">発生基準位置として使うヒット座標。</param>
        /// <returns>ランダムな散らばりを加えたワールド座標。</returns>
        private Vector3 CreateSpawnPosition(Vector3 hitPoint)
        {
            Vector3 basePosition = GetBasePosition(hitPoint);
            Vector3 offset = UnityEngine.Random.insideUnitSphere * _spawnRadius;
            offset.y = Mathf.Abs(offset.y);
            return basePosition + offset;
        }

        /// <summary>
        /// 欠片発生の基準位置を返す。
        /// </summary>
        /// <param name="fallbackPosition">EmitOrigin が未設定の場合に使う代替位置。</param>
        /// <returns>EmitOrigin があればその位置、なければ fallbackPosition。</returns>
        private Vector3 GetBasePosition(Vector3 fallbackPosition)
        {
            return _emitOrigin != null ? _emitOrigin.position : fallbackPosition;
        }

        /// <summary>
        /// 欠片に与える初速ベクトルを作る。
        /// </summary>
        /// <param name="hitDirection">欠片を主に飛ばす方向。</param>
        /// <param name="power">速度倍率。</param>
        /// <returns>SetInitialMotion に渡す初速ベクトル。</returns>
        private Vector3 CreateVelocity(Vector3 hitDirection, float power)
        {
            Vector3 baseDirection = hitDirection.sqrMagnitude > 0.01f
                ? hitDirection.normalized
                : transform.up;

            Vector3 direction = baseDirection
                + Vector3.up * _upwardBias
                + UnityEngine.Random.insideUnitSphere * _randomSpread;

            if (direction.sqrMagnitude < 0.01f)
            {
                direction = Vector3.up;
            }

            float minSpeed = Mathf.Min(_minLaunchSpeed, _maxLaunchSpeed);
            float maxSpeed = Mathf.Max(_minLaunchSpeed, _maxLaunchSpeed);
            float speed = UnityEngine.Random.Range(minSpeed, maxSpeed);
            return direction.normalized * speed * Mathf.Max(0.1f, power);
        }

        /// <summary>
        /// 欠片に与えるランダムな角速度を作る。
        /// </summary>
        /// <returns>SetInitialMotion に渡す角速度ベクトル。</returns>
        private Vector3 CreateAngularVelocity()
        {
            float minSpeed = Mathf.Min(_minAngularSpeed, _maxAngularSpeed);
            float maxSpeed = Mathf.Max(_minAngularSpeed, _maxAngularSpeed);
            return UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(minSpeed, maxSpeed);
        }

        /// <summary>
        /// 欠片発生に必要な参照とデータが揃っているか確認する。
        /// </summary>
        /// <returns>欠片発生を実行できる状態なら true。</returns>
        private bool CanEmit()
        {
            if (_pool == null || _registry == null)
            {
                Debug.LogError("[CrystalShardEmitter] CollectiblePool or CollectibleRegistry is missing.", this);
                return false;
            }

            if (_shardData == null || _shardData.Length == 0)
            {
                Debug.LogError("[CrystalShardEmitter] Shard data is missing.", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Inspector で未設定の Pool と Registry をシーン内検索で補完する。
        /// </summary>
        private void ResolveReferencesIfNeeded()
        {
            if (_pool == null)
            {
                _pool = FindFirstObjectByType<CollectiblePool>();
            }

            if (_registry == null)
            {
                _registry = FindFirstObjectByType<CollectibleRegistry>();
            }
        }

        /// <summary>
        /// 欠片が回収または Despawn された時に呼ばれる Pool 返却処理。
        /// Registry から外してから Pool に戻す。
        /// </summary>
        /// <param name="shard">返却する欠片オブジェクト。</param>
        private void ReturnToPool(CollectibleObject shard)
        {
            _registry.Unregister(shard);
            _pool.Return(shard);
        }
    }
}
