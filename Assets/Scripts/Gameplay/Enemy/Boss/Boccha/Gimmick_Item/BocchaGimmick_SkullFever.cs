// ------------------------------------------------------------
// File		: BocchaGimmick_SkullFever.cs
// Summary	: ドクロを落し物として山なりに投げるギミック
//
// Author	: [浅野 勇生]
// Created	: 2026-09-08
//
// Notes	:
// - ドクロはプレイヤーが持ち運べるので、キャンディと同じ落し物として生成する!
// - カウントダウンは生成した瞬間から回るので、持ったままだと手元で爆発する！
// - 調整値（数）はこのSOのSpawn Countで行う!
// - 爆発の設定もこのSOで持ち、生成時にBocchaSkullへ渡す。
// - バリアを削らないので、トゲ玉と違い危険圏を避ける必要はない。
// ------------------------------------------------------------
using System.Collections.Generic;
using Game.Data.Collectibles;
using Game.Gameplay.Collectibles;
using Game.Gameplay.Stage;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// ドクロを生成するギミック
    /// </summary>
    [CreateAssetMenu(fileName = "Gimmick_BocchaSkullFever", menuName = "Boss/Gimmicks/Boccha/SkullFever")]
    public sealed class BocchaGimmick_SkullFever : BossGimmickSO, IBocchaTunable
    {
        [Header("--- ドクロ ---")]

        [SerializeField]
        [Tooltip("ドクロの落し物データ")]
        private CollectibleData _skullData;

        [SerializeField]
        [Min(1)]
        [Tooltip("1回あたりの生成数")]
        private int _spawnCount = 3;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("大きさの倍率")]
        private float _scaleMultiplier = 1.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("1個ずつ投げる間隔")]
        private float _spawnInterval = 0.2f;

        [SerializeField]
        [Tooltip("着地した瞬間のVFX")]
        private GameObject _landingVfxPrefab;


        [Header("--- 投げ方 ---")]

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


        [Header("--- 爆発 ---")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("生成してから爆発するまでの時間")]
        private float _countdownDuration = 4.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("爆発が届く範囲")]
        private float _explosionRadius = 6.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("オカシを吹き飛ばす初速(m/s)。中心にいるものがこの速度で飛ぶ")]
        private float _explosionSpeed = 8.0f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("吹き飛ばしを上方向へ寄せる割合。0で真横、1で真上寄り")]
        private float _upwardRatio = 0.4f;

        [SerializeField]
        [Tooltip("爆発時のVFX")]
        private GameObject _explosionVfxPrefab;

        [SerializeField]
        [Min(0f)]
        [Tooltip("VFXを破棄するまでの時間")]
        private float _vfxLifeTime = 3.0f;


        [Header("--- 散布パターン ---")]

        [SerializeField]
        [Tooltip("散布座標の計算設定")]
        private BocchaScatterSettings _scatter = new BocchaScatterSettings();

        [SerializeField]
        [Tooltip("ONならフィールド中心、OFFならボス位置を散布の中心にする")]
        private bool _scatterFromFieldCenter = true;


        [Header("--- デバッグ ---")]

        [SerializeField]
        [Tooltip("散布点と軌道をSceneビューに表示するかどうか")]
        private bool _drawSpawnPoints = true;


        // ランタイム状態
        // ------------------------------------------------------------

        private readonly List<Vector3> _points = new List<Vector3>();

        private CollectibleSpawner _spawner;
        private float _timer;
        private int _spawnedCount;
        private bool _isComplete;


        public override bool IsComplete => _isComplete;

        public override bool IsTick => true;


        /// <summary>
        /// 散布点を計算して投げる準備をする
        /// </summary>
        public override void Execute()
        {
            _isComplete = false;
            _timer = 0.0f;
            _spawnedCount = 0;

            _scatter.BuildPoints(ResolveScatterCenter(), _spawnCount, _points);

            if (_points.Count == 0 || _skullData == null)
            {
                Debug.LogWarning("[SkullFever] データ未設定、または散布点が0のため生成できません。");

                _isComplete = true;
            }
        }


        /// <summary>
        /// 一定間隔で1個ずつ投げる
        /// </summary>
        /// <param name="dt">経過時間</param>
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

            _timer = _spawnInterval;

            ThrowOne();

            if (_spawnedCount < _points.Count)
            {
                return;
            }

            _isComplete = true;
        }


        /// <summary>
        /// ギミックの中断
        /// </summary>
        public override void Cancel()
        {
            _points.Clear();

            _isComplete = true;
        }


        public void ApplyTuning(BocchaRoundTuning tuning)
        {
            if (tuning == null) return;

            _spawnCount = Mathf.Max(0, tuning.SkullCount);
        }


        // 内部処理
        // ------------------------------------------------------------

        /// <summary>
        /// ドクロを1個投げる
        /// </summary>
        private void ThrowOne()
        {
            CollectibleSpawner spawner = ResolveSpawner();

            if (spawner == null)
            {
                _isComplete = true;

                return;
            }

            Vector3 targetPoint = _points[_spawnedCount];

            _spawnedCount++;

            Vector3 from = Context.GetSocket(_throwSocket).position;

            // CollectableGravityが独自重力を使うため、フィールドの上方向から重力を作り直す
            Vector3 up = FieldContext.IsReady ? FieldContext.Up : Vector3.up;
            Vector3 gravity = -up * _throwGravity;

            // 頂点の高さを一個ずつ揺らして、同じ軌道が並ばないようにする
            float apex = _arcApexHeight * (1.0f + UnityEngine.Random.Range(-_apexRandomness, _apexRandomness));

            Vector3 velocity = BocchaBallistics.SolveVelocityByApex(from, targetPoint, apex, gravity, out float flightTime);

            // ロックされた種類でも出せるよう、ボスギミックではアンロック判定を無視する
            CollectibleObject skullObject = spawner.SpawnWithVelocity(_skullData, from, Vector3.zero, _scaleMultiplier, true);

            if (skullObject == null)
            {
                return;
            }

            // 初速は与えず、放物線上を直接動かして軌道を保証する
            if (!skullObject.TryGetComponent(out BocchaThrownItemMover mover))
            {
                mover = skullObject.gameObject.AddComponent<BocchaThrownItemMover>();
            }

            mover.Begin(from, velocity, gravity, flightTime);

            mover.SetLandingVfx(_landingVfxPrefab, _vfxLifeTime);

            // 落し物はプールの使い回しなので、判定コンポーネントも実行時に付ける
            if (!skullObject.TryGetComponent(out BocchaSkull skull))
            {
                skull = skullObject.gameObject.AddComponent<BocchaSkull>();
            }

            skull.Initialize(
                _countdownDuration,
                _explosionRadius,
                _explosionSpeed,
                _upwardRatio,
                _explosionVfxPrefab,
                _vfxLifeTime);

            if (_drawSpawnPoints)
            {
                BocchaBallistics.DrawTrajectory(from, velocity, gravity, flightTime, 5.0f, Color.magenta);
            }
        }


        /// <summary>
        /// 散布の中心を決める
        /// </summary>
        private Vector3 ResolveScatterCenter()
        {
            if (_scatterFromFieldCenter && FieldContext.IsReady)
            {
                return FieldContext.Center;
            }

            return Context.Transform.position;
        }


        /// <summary>
        /// シーンの加算ロード順に影響されないよう、実行時に遅延取得する
        /// </summary>
        private CollectibleSpawner ResolveSpawner()
        {
            if (_spawner != null)
            {
                return _spawner;
            }

            _spawner = UnityEngine.Object.FindFirstObjectByType<CollectibleSpawner>();

            if (_spawner == null)
            {
                Debug.LogWarning("[SkullFever] CollectibleSpawnerがシーンに見つかりません。");
            }

            return _spawner;
        }
    }
}
