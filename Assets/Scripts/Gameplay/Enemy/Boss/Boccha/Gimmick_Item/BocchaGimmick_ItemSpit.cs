// ------------------------------------------------------------
// File		: BocchaGimmick_ItemSpit.cs
// Summary	: 「何が出るかな？」オカシの散布とお邪魔アイテムの抽選を行う！
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - 一度に全部出すと負荷が跳ねるため、Tickで1個ずつ吐き出したほうがいいってAIがいってるのでまずはそれでやってみます！
// - 散布座標はBocchaScatterSettingsが担当し、方向の偏りが出ないようにしてみる！また甲斐に見てもらってかわるかもだけど、勝手にやるね～
// - 抽選したハザードはNextOverrideGimmickでフロー側へ渡し、割り込み実行のやつをふぁるの作ってくれたのうまいこと使って実行する感じにする！
// ------------------------------------------------------------
using System.Collections.Generic;
using Game.Core.Events;
using Game.Data.Collectibles;
using Game.Gameplay.Collectibles;
using Game.Gameplay.Stage;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 「何が出るかな？」のギミッククラス
    /// </summary>
    [CreateAssetMenu(fileName = "Gimmick_BocchaItemSpit", menuName = "Boss/Gimmicks/Boccha/ItemSpit")]
    public class BocchaGimmick_ItemSpit : BossGimmickSO
    {
        [Header("--- オカシ散布 ---")]

        [SerializeField]
        [Tooltip("散布するオカシのデータ")]
        private CollectibleData _candyData;

        [SerializeField]
        [Min(1)]
        [Tooltip("1回あたりの散布数")]
        private int _candyCount = 8;

        [SerializeField]
        [Min(0f)]
        [Tooltip("1個ずつ吐き出す間隔")]
        private float _spitInterval = 0.12f;

        [SerializeField]
        [Min(1f)]
        [Tooltip("落下を開始する高さ")]
        private float _dropHeight = 12.0f;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("オカシの大きさ倍率")]
        private float _candyScale = 1.0f;


        [Header("--- 散布パターン ---")]

        [SerializeField]
        [Tooltip("散布座標の計算設定")]
        private BocchaScatterSettings _scatter = new BocchaScatterSettings();

        [SerializeField]
        [Tooltip("ONならフィールド中心、OFFならボス位置を散布の中心にする")]
        private bool _scatterFromFieldCenter = true;


        [Header("--- 投げ方 ---")]

        [SerializeField]
        [Tooltip("ONならボスから山なりに投げる。OFFなら真上から落とす")]
        private bool _useArcThrow = true;

        [SerializeField]
        [Tooltip("投げ出す位置のソケット")]
        private BossSocket _throwSocket = BossSocket.Muzzle;

        [SerializeField]
        [Min(0.5f)]
        [Tooltip("軌道の頂点の高さ。大きいほどふわっと山なりになる")]
        private float _arcApexHeight = 9.0f;

        [SerializeField]
        [Range(0f, 0.5f)]
        [Tooltip("頂点の高さのばらつき。1個ずつ軌道を変えて単調さを消す")]
        private float _apexRandomness = 0.2f;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("軌道計算に使う重力加速度。CollectableGravityの値と揃える")]
        private float _throwGravity = 9.8f;


        [Header("--- ハザード抽選 ---")]

        [SerializeField]
        [Tooltip("石化キャンディのギミックデータ")]
        private BossGimmickData _stoneCandyData;

        [SerializeField]
        [Tooltip("トゲ玉のギミックデータ")]
        private BossGimmickData _spikeBallData;

        [SerializeField]
        [Tooltip("ドクロフィーバーのギミックデータ")]
        private BossGimmickData _skullFeverData;

        [SerializeField]
        [Min(0f)]
        [Tooltip("石化キャンディの抽選重み")]
        private float _stoneWeight = 1.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("トゲ玉の抽選重み")]
        private float _spikeWeight = 1.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("ドクロフィーバーの抽選重み")]
        private float _skullWeight = 1.0f;


        [Header("--- デバッグ ---")]

        [SerializeField]
        [Tooltip("散布点をSceneビューにレイで表示するかどうか")]
        private bool _drawSpawnPoints = true;

        [SerializeField]
        [Tooltip("散布のたびにログを出すかどうか")]
        private bool _logSpit = true;


        // ランタイム状態
        // ------------------------------------------------------------

        private readonly List<Vector3> _pendingPoints = new List<Vector3>();

        private CollectibleSpawner _spawner;
        private BocchaCapacityGauge _capacityGauge;
        private BossGimmickData _nextOverride;

        private float _timer;
        private int _spawnedCount;
        private bool _isComplete;


        public override bool IsComplete => _isComplete;

        public override bool IsTick => true;

        public override BossGimmickData NextOverrideGimmick => _nextOverride;


        /// <summary>
        /// ギミックの初期化
        /// </summary>
        /// <param name="context"></param>
        public override void Initialize(BossContext context)
        {
            base.Initialize(context);

            _capacityGauge = context.Transform.GetComponentInChildren<BocchaCapacityGauge>();

            if (_capacityGauge == null)
            {
                Debug.LogWarning($"[{nameof(BocchaGimmick_ItemSpit)}] BocchaCapacityGaugeが見つかりませんでした。");
            }
        }


        /// <summary>
        /// ギミックの実行開始
        /// </summary>
        public override void Execute()
        {
            _isComplete = false;
            _timer = 0.0f;
            _spawnedCount = 0;
            _nextOverride = null;

            _scatter.BuildPoints(ResolveScatterCenter(), _candyCount, _pendingPoints);

            EventBus.Publish(new BocchaSpitStartedEvent(Context.GetSocket(BossSocket.Muzzle).position, _pendingPoints.Count));

            _capacityGauge?.AddFromSpit();

            _nextOverride = PickHazard();

            if (_logSpit)
            {
                string hazardName = _nextOverride != null ? _nextOverride.name : "なし(つぎやるよてー)";

                Debug.Log($"[ItemSpit] 散布点 {_pendingPoints.Count} 個 / 次のハザード: {hazardName}");
            }

            if (_pendingPoints.Count == 0) _isComplete = true;
        }


        /// <summary>
        /// ギミックの実行中に毎フレーム呼ばれる
        /// </summary>
        /// <param name="dt"></param>
        public override void Tick(float dt)
        {
            if (_isComplete)
            {
                return;
            }

            _timer -= dt;

            if (_timer > 0.0f)
            {
                return;
            }

            _timer = _spitInterval;

            // 散布点が残っていれば、1個ずつ吐き出す
            SpawnNext();

            // すべて吐き出し終わったら、次のギミックを割り込み実行する
            if (_spawnedCount < _pendingPoints.Count)
            {
                return;
            }

            _isComplete = true;
        }


        /// <summary>
        /// ギミックの実行を中断する
        /// </summary>
        public override void Cancel()
        {
            _pendingPoints.Clear();

            // 中断された場合は、次のギミックを割り込み実行しない
            _nextOverride = null;
            _isComplete = true;
        }


        // 内部処理
        // ------------------------------------------------------------

        /// <summary>
        /// ギミックを一個ずつ吐き出す
        /// </summary>
        private void SpawnNext()
        {
            CollectibleSpawner spawner = ResolveSpawner();

            if (spawner == null)
            {
                _isComplete = true;
                return;
            }

            Vector3 point = _pendingPoints[_spawnedCount];

            _spawnedCount++;

            if (_useArcThrow)
            {
                ThrowCandy(spawner, point);
            }
            else
            {
                spawner.SpawnCollectiblesFromAboveAt(point, 1, _candyData, _dropHeight, 0.0f, _candyScale);
            }

            if (_drawSpawnPoints)
            {
                Debug.DrawRay(point, Vector3.up * 2.0f, Color.cyan, 5.0f);
            }
        }


        /// <summary>
        /// オカシを山なりに投げる
        /// </summary>
        /// <param name="spawner">アイテムのスポナー</param>
        /// <param name="targetPoint">目標地点</param>
        private void ThrowCandy(CollectibleSpawner spawner, Vector3 targetPoint)
        {
            Vector3 from = Context.GetSocket(_throwSocket).position;

            // 頂点の高さを一個ずつ揺らして、同じ軌道が並ばないようにする
            float apex = _arcApexHeight * (1.0f + UnityEngine.Random.Range(-_apexRandomness, _apexRandomness));

            // CollectableGravityが独自重力を使うため、フィールドの上方向から重力を作り直す
            Vector3 up = FieldContext.IsReady ? FieldContext.Up : Vector3.up;
            Vector3 gravity = -up * _throwGravity;

            Vector3 velocity = BocchaBallistics.SolveVelocityByApex(from, targetPoint, apex, gravity, out float flightTime);

            // 初速は与えず、放物線上を直接動かして軌道を保証する
            CollectibleObject candy = spawner.SpawnWithVelocity(_candyData, from, Vector3.zero, _candyScale);

            if (candy == null)
            {
                return;
            }

            if (!candy.TryGetComponent(out BocchaThrownItemMover mover))
            {
                mover = candy.gameObject.AddComponent<BocchaThrownItemMover>();
            }

            mover.Begin(from, velocity, gravity, flightTime);

            if (_drawSpawnPoints)
            {
                BocchaBallistics.DrawTrajectory(from, velocity, gravity, flightTime, 5.0f, Color.yellow);
            }
        }


        private Vector3 ResolveScatterCenter()
        {
            if (_scatterFromFieldCenter && FieldContext.IsReady)
            {
                return FieldContext.Center;
            }

            return Context.Transform.position;
        }


        /// <summary>
        /// シーンの加算ロード順に影響されないよう、実行時に遅延取得
        /// </summary>
        /// <returns></returns>
        private CollectibleSpawner ResolveSpawner()
        {
            if (_spawner != null)
            {
                return _spawner;
            }

            _spawner = UnityEngine.Object.FindFirstObjectByType<CollectibleSpawner>();

            if (_spawner == null)
            {
                Debug.LogError($"[{nameof(BocchaGimmick_ItemSpit)}] CollectibleSpawnerが見つかりませんでした。");
            }

            return _spawner;
        }


        /// <summary>
        /// ハザード抽選を行う
        /// </summary>
        /// <returns>どのハザードが選ばれたか</returns>
        private BossGimmickData PickHazard()
        {
            float total = 0.0f;

            if (_stoneCandyData != null) total += _stoneWeight;
            if (_spikeBallData != null) total += _spikeWeight;
            if (_skullFeverData != null) total += _skullWeight;

            if (total <= 0.0f)
            {
                return null;
            }

            // 重み付きランダム抽選
            float roll = UnityEngine.Random.Range(0.0f, total);

            // 石化キャンディの抽選
            if (_stoneCandyData != null && (roll -= _stoneWeight) < 0.0f)
            {
                return _stoneCandyData;
            }

            // トゲ玉の抽選
            if (_spikeBallData != null && (roll -= _spikeWeight) < 0.0f)
            {
                return _spikeBallData;
            }

            return _skullFeverData;
        }
    }
}
