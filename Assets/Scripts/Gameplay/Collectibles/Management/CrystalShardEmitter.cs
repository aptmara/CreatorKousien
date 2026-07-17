// ================================================================================
// File         : CrystalShardEmitter.cs
// Description  : 大小変更可能なクリスタルを殴った対象に応じて欠片を出す
// author       : 山本郁也
// data         : 2026/05/19
// Updated      : 2026/07/11 (ドロップテーブル拡張) - Iwai Shogo  
// ================================================================================

using System;
using UnityEngine;
using Game.Data.Collectibles;

namespace Game.Gameplay.Collectibles
{
    /// <summary>
    /// ドロップテーブルを使って、クリスタルを殴った時に欠片を発生させるコンポーネント。
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
        /// 生成候補となる欠片データと抽選重みを保持します。
        /// </summary>
        [Serializable]
        public struct WeightedShardData
        {
            /// <summary>
            /// 生成対象の欠片データです。
            /// </summary>
            public CollectibleData Data;

            /// <summary>
            /// 欠片データの抽選重みです。
            /// </summary>
            [Min(0f)] public float Weight;
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

        [Header("Drop Table Configuration")]
        [Tooltip("このクリスタルが使用するドロップテーブルアセット")]
        [SerializeField] private CollectibleTable _collectibleTable;

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

        /// <summary>
        /// 生成する欠片の表示倍率。Pool 返却時に 1 倍へ戻すため、他の Spawner には影響しない。
        /// </summary>
        [SerializeField, Min(0.01f)] private float _shardScale = 1.0f;

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
        /// 欠片を飛ばす中心位置。クリスタル本体からこの位置へ向かう方向を基準にする。
        /// </summary>
        [SerializeField] private ShardEmissionVector _emissionVector;

        /// <summary>
        /// Target 方向から何度まで欠片を散らすか。Gizmo では円錐状の範囲として表示する。
        /// </summary>
        [SerializeField, Min(0f)] private float _spawnRadius = 1.0f;

        /// <summary>
        /// 欠片に与える初速の最小値。
        /// </summary>
        [SerializeField, Min(0f)] private float _minLaunchSpeed = 2.0f;

        /// <summary>
        /// 欠片に与える初速の最大値。
        /// </summary>
        [SerializeField, Min(0f)] private float _maxLaunchSpeed = 5.0f;

        /// <summary>
        /// 欠片に与える角速度の固定範囲。Inspector での調整対象にはしない。
        /// </summary>
        private const float MinAngularSpeed = 2.0f;
        private const float MaxAngularSpeed = 8.0f;

        /// <summary>
        /// Scene 上に表示する Gizmo の固定色。
        /// </summary>
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
            EmitFromHit(transform.position, Vector3.zero, 1f);
        }

        /// <summary>
        ///  外から直接1発分の射出方向を指定して生成する。
        /// </summary>
        /// <param name="direction">欠片の基準移動方向です。</param>
        /// <param name="randomRange">基準方向へ加えるランダム方向の強さです。</param>
        /// <param name="movementAmount">欠片へ与える移動量です。</param>
        /// <param name="shardTable">重み付き欠片データの配列です。</param>
        public void Emitter(Vector3 direction, float randomRange, float movementAmount, WeightedShardData[] shardTable)
        {
            CollectibleData data = GetSelectedCollectibleData();
            if (data == null) return;

            EmitOne(GetBasePosition(transform.position), direction, randomRange, movementAmount, data);
        }

        /// <summary>
        /// プレイヤーの攻撃判定、またはヒット検知側から呼ぶ欠片発生 API。
        /// 互換性のため hitPoint と hitDirection を受け取るが、射出位置と方向は Scene 上の設定を使う。
        /// </summary>
        /// <param name="hitPoint">互換性維持用。現在の射出位置計算では使わない。</param>
        /// <param name="hitDirection">互換性維持用。現在の射出方向計算では使わない。</param>
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
                EmitOne(power);
            }
        }

        /// <summary>
        /// 重み付き生成用の既存オーバーロード
        /// </summary>
        public void EmitFromHit(Vector3 hitPoint, Vector3 direction, float randomRange, float movementAmount, WeightedShardData[] shardTable)
        {
            ResolveReferencesIfNeeded();
            if (!CanUseSpawnApi()) return;

            // 引数に重み付きデータがある場合はそちらを優先（既存の育成クリスタル等の互換用）
            // なければ共通ドロップテーブルから取得
            if (!TrySelectWeightedShardData(shardTable, out CollectibleData data))
            {
                data = GetSelectedCollectibleData();
            }

            if (data == null) return;

            CollectibleObject shard = _pool.Get();
            shard.transform.position = CreateSpawnPosition(hitPoint);
            shard.transform.rotation = UnityEngine.Random.rotation;
            shard.Initialize(data, ReturnToPool, _canBeCollectedByPlayer);
            shard.SetInitialMotion(CreateWeightedVelocity(direction, randomRange, movementAmount), CreateAngularVelocity());

            _registry.Register(shard);
        }

        public void EmitFromWorldPosition(
            Vector3 worldPosition,
            Vector3 direction,
            float randomRange,
            float movementAmount,
            WeightedShardData[] shardTable,
            Vector3 spawnUp)
        {
            ResolveReferencesIfNeeded();
            if (!CanUseSpawnApi()) return;

            if (!TrySelectWeightedShardData(shardTable, out CollectibleData data))
            {
                data = GetSelectedCollectibleData();
            }

            if (data == null) return;

            CollectibleObject shard = _pool.Get();
            shard.transform.position = CreateOrientedSpawnPosition(worldPosition, spawnUp);
            shard.transform.rotation = UnityEngine.Random.rotation;
            shard.Initialize(data, ReturnToPool, _canBeCollectedByPlayer);
            shard.SetInitialMotion(CreateWeightedVelocity(direction, randomRange, movementAmount), CreateAngularVelocity());

            _registry.Register(shard);
        }

        private CollectibleData GetSelectedCollectibleData()
        {
            if (_collectibleTable != null)
            {
                return _collectibleTable.DetermineItem();
            }

            if (_shardData != null && _shardData.Length > 0)
            {
                return _shardData[UnityEngine.Random.Range(0, _shardData.Length)];
            }

            return null;
        }

        /// <summary>
        /// 欠片を 1 個だけ生成し、位置、見た目データ、初速、角速度、Registry 登録を行う。
        /// </summary>
        /// <param name="power">初速に掛ける倍率。</param>
        private void EmitOne(float power)
        {
            CollectibleData data = GetSelectedCollectibleData();
            if (data == null)
            {
                Debug.LogWarning("[CrystalShardEmitter] 抽選されたCollectibleDataがありません。ドロップテーブルの設定を確認してください。");
                return;
            }

            CollectibleObject shard = _pool.Get();
            shard.transform.position = _emissionVector.OriginPosition;
            shard.transform.rotation = UnityEngine.Random.rotation;
            shard.transform.localScale = Vector3.one * Mathf.Max(0.01f, _shardScale);
            shard.Initialize(data, ReturnToPool, _canBeCollectedByPlayer);
            shard.SetInitialMotion(CreateVelocity(power), CreateAngularVelocity());

            _registry.Register(shard);
        }



        /// <summary>
        /// 重み付き欠片テーブルから抽選した欠片を 1 つ生成します。
        /// </summary>
        /// <param name="hitPoint">欠片の発生位置です。</param>
        /// <param name="direction">欠片の基準移動方向です。</param>
        /// <param name="randomRange">基準方向へ加えるランダム方向の強さです。</param>
        /// <param name="movementAmount">欠片へ与える移動量です。</param>
        /// <param name="shardTable">重み付き欠片データの配列です。</param>
        private void EmitOne(Vector3 hitPoint, Vector3 direction, float randomRange, float movementAmount, CollectibleData data)
        {
            CollectibleObject shard = _pool.Get();
            shard.transform.position = CreateSpawnPosition(hitPoint);
            shard.transform.rotation = UnityEngine.Random.rotation;
            shard.Initialize(data, ReturnToPool, _canBeCollectedByPlayer);
            shard.SetInitialMotion(CreateWeightedVelocity(direction, randomRange, movementAmount), CreateAngularVelocity());

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

        private Vector3 CreateOrientedSpawnPosition(Vector3 worldPosition, Vector3 spawnUp)
        {
            Vector3 normalizedUp = spawnUp.sqrMagnitude > 0.0001f
                ? spawnUp.normalized
                : Vector3.up;
            Vector3 offset = UnityEngine.Random.insideUnitSphere * _spawnRadius;
            float upwardAmount = Vector3.Dot(offset, normalizedUp);
            if (upwardAmount < 0f)
            {
                offset -= normalizedUp * (upwardAmount * 2f);
            }

            return worldPosition + offset;
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
        /// <param name="power">速度倍率。</param>
        /// <returns>SetInitialMotion に渡す初速ベクトル。</returns>
        private Vector3 CreateVelocity(float power)
        {
            Vector3 centerDirection = _emissionVector.Direction;
            Vector3 direction = CreateRandomDirectionInCone(centerDirection);

            float minSpeed = Mathf.Min(_minLaunchSpeed, _maxLaunchSpeed);
            float maxSpeed = Mathf.Max(_minLaunchSpeed, _maxLaunchSpeed);
            float speed = UnityEngine.Random.Range(minSpeed, maxSpeed);
            return direction.normalized * speed * Mathf.Max(0.1f, power);
        }

        /// <summary>
        /// クリスタル本体から Target へ向かう中心方向を取得する。
        /// </summary>
        /// <returns>正規化済みの中心方向。</returns>
        /// <summary>
        /// Scene 上で指定された中心方向を基準に、円錐範囲内のランダムな方向を作る。
        /// </summary>
        /// <param name="centerDirection">円錐の中心方向。</param>
        /// <returns>円錐範囲内に収まる正規化済み方向。</returns>
        private Vector3 CreateRandomDirectionInCone(Vector3 centerDirection)
        {
            float angle = UnityEngine.Random.Range(0f, _emissionVector.EmissionAngle);
            float rotation = UnityEngine.Random.Range(0f, 360f);
            Vector3 perpendicular = GetPerpendicular(centerDirection);
            Vector3 tiltedDirection = Quaternion.AngleAxis(angle, perpendicular) * centerDirection;
            return (Quaternion.AngleAxis(rotation, centerDirection) * tiltedDirection).normalized;
        }

        /// <summary>
        /// 指定方向と直交するベクトルを作る。
        /// </summary>
        /// <param name="direction">基準方向。</param>
        /// <returns>基準方向と直交する正規化済みベクトル。</returns>
        private static Vector3 GetPerpendicular(Vector3 direction)
        {
            Vector3 axis = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) < 0.99f
                ? Vector3.up
                : Vector3.right;
            return Vector3.Cross(direction, axis).normalized;
        }

        /// <summary>
        /// 重み付き生成 API 用の移動ベクトルを作成します。
        /// </summary>
        /// <param name="direction">欠片の基準移動方向です。</param>
        /// <param name="randomRange">基準方向へ加えるランダム方向の強さです。</param>
        /// <param name="movementAmount">欠片へ与える移動量です。</param>
        /// <returns>SetInitialMotion に渡す移動ベクトルです。</returns>
        private Vector3 CreateWeightedVelocity(Vector3 direction, float randomRange, float movementAmount)
        {
            Vector3 baseDirection = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : transform.up;

            // ランダム方向のばらつき角度を計算する
            Vector3 finalDirection = RandomInCone(baseDirection, randomRange);
            return finalDirection * Mathf.Max(0f, movementAmount);
        }


        /// <summary>
        /// 指定した方向を中心に、指定角度の円錐内でランダムな方向ベクトルを作る。
        /// 6.23 浅野：円錐内ランダム方向を作るように修正しました。
        /// </summary>
        /// <param name="baseDir">中心</param>
        /// <param name="maxAngleDeg">最大開き角</param>
        /// <returns>ランダムな方向ベクトル</returns>
        private static Vector3 RandomInCone(Vector3 baseDir, float maxAngleDeg)
        {
            baseDir = baseDir.normalized;

            float angle = UnityEngine.Random.Range(0f, Mathf.Max(0f, maxAngleDeg)); // 中心からの開き角
            float roll = UnityEngine.Random.Range(0f, 360f);                        // 中心軸まわりの回転

            // BaseDir に直行する軸を作る
            Vector3 perp = Vector3.Cross(baseDir, Mathf.Abs(baseDir.y) < 0.99f ? Vector3.up : Vector3.right).normalized;

            Vector3 tilted = Quaternion.AngleAxis(angle, perp) * baseDir;           // angle だけ傾けた方向
            return Quaternion.AngleAxis(roll, baseDir) * tilted;                    // baseDir まわりに roll 回転させる
        }



        /// <summary>
        /// 欠片に与えるランダムな角速度を作る。
        /// </summary>
        /// <returns>SetInitialMotion に渡す角速度ベクトル。</returns>
        private Vector3 CreateAngularVelocity()
        {
            return UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(MinAngularSpeed, MaxAngularSpeed);
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

            if (_collectibleTable == null && (_shardData == null || _shardData.Length == 0))
            {
                Debug.LogError("[CrystalShardEmitter] Both CollectibleTable and ShardData fallback are missing.", this);
                return false;
            }

            if (_shardData == null || _shardData.Length == 0)
            {
                Debug.LogError("[CrystalShardEmitter] Shard data is missing.", this);
                return false;
            }

            if (_emissionVector == null || !_emissionVector.HasTarget)
            {
                Debug.LogError("[CrystalShardEmitter] ShardEmissionVector or its Target is missing.", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 新しい生成 API で必要な参照が揃っているかを確認します。
        /// </summary>
        /// <returns>生成 API を利用できる場合は true を返します。</returns>
        private bool CanUseSpawnApi()
        {
            if (_pool == null || _registry == null)
            {
                Debug.LogError("[CrystalShardEmitter] CollectiblePool or CollectibleRegistry is missing.", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 重み付き欠片テーブルから欠片データを抽選します。
        /// </summary>
        /// <param name="shardTable">重み付き欠片データの配列です。</param>
        /// <param name="data">抽選結果の欠片データです。</param>
        /// <returns>抽選に成功した場合は true を返します。</returns>
        private bool TrySelectWeightedShardData(WeightedShardData[] shardTable, out CollectibleData data)
        {
            data = null;

            if (shardTable == null || shardTable.Length == 0)
            {
                // Debug.LogError("[CrystalShardEmitter] Weighted shard data is missing.", this);
                return false;
            }

            float totalWeight = 0f;
            int validCount = 0;
            for (int i = 0; i < shardTable.Length; i++)
            {
                WeightedShardData candidate = shardTable[i];
                if (candidate.Data == null || candidate.Weight <= 0f || float.IsNaN(candidate.Weight) || float.IsInfinity(candidate.Weight))
                {
                    continue;
                }

                totalWeight += candidate.Weight;
                validCount++;
            }

            if (validCount == 0)
            {
                Debug.LogError("[CrystalShardEmitter] Weighted shard data is missing.", this);
                return false;
            }

            if (totalWeight <= 0f || float.IsNaN(totalWeight) || float.IsInfinity(totalWeight))
            {
                Debug.LogError("[CrystalShardEmitter] Weighted shard weight is invalid.", this);
                return false;
            }

            float randomWeight = UnityEngine.Random.value * totalWeight;
            float cumulativeWeight = 0f;

            for (int i = 0; i < shardTable.Length; i++)
            {
                WeightedShardData candidate = shardTable[i];
                if (candidate.Data == null || candidate.Weight <= 0f || float.IsNaN(candidate.Weight) || float.IsInfinity(candidate.Weight))
                {
                    continue;
                }

                cumulativeWeight += candidate.Weight;
                if (randomWeight <= cumulativeWeight)
                {
                    data = candidate.Data;
                    return true;
                }
            }

            for (int i = shardTable.Length - 1; i >= 0; i--)
            {
                WeightedShardData candidate = shardTable[i];
                if (candidate.Data != null && candidate.Weight > 0f && !float.IsNaN(candidate.Weight) && !float.IsInfinity(candidate.Weight))
                {
                    data = candidate.Data;
                    return true;
                }
            }

            Debug.LogError("[CrystalShardEmitter] Weighted shard data is missing.", this);
            return false;
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

        /// <summary>
        /// Scene 上で選択した時に、欠片の中心方向と発射可能範囲を円錐 Gizmo として表示する。
        /// オレンジ色の矢印が欠片の主方向、水色の円錐が散らばる範囲を表す。
        /// 表示方向と実際の発射方向は、どちらもクリスタル本体から Target へ向かう方向になる。
        /// </summary>
        /// <summary>
        /// Scene 上で欠片の主方向を確認できる矢印 Gizmo を描画する。
        /// </summary>
        /// <param name="origin">矢印の始点。</param>
        /// <param name="direction">矢印が向く方向。</param>
        /// <param name="length">矢印全体の長さ。</param>
    }
}
