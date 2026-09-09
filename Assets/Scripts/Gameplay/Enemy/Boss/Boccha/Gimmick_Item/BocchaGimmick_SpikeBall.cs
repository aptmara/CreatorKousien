// ------------------------------------------------------------
// File		: BocchaGimmick_SpikeBall.cs
// Summary	: トゲ玉を落し物として山なりに投げるギミック
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - トゲ玉はプレイヤーが持ち運べるので、キャンディと同じ落し物として生成する!
// - 調整値（サイズ・数）はこのSOのScale Multiplier / Spawn Countで行う!
// - バリアダメージ量もこのSOで設定し、生成時にBocchaSpikeBallへ渡す。
// - ボス自身が危険圏へ投げ込まないよう、危険圏内の散布点は除外する。
// ------------------------------------------------------------
using System.Collections.Generic;
using Game.Data.Collectibles;
using Game.Gameplay.Collectibles;
using Game.Gameplay.Stage;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// トゲ玉を生成するギミック
    /// </summary>
    [CreateAssetMenu(fileName = "Gimmick_BocchaSpikeBall", menuName = "Boss/Gimmicks/Boccha/SpikeBall")]
    public sealed class BocchaGimmick_SpikeBall : BossGimmickSO, IBocchaTunable
    {
        [Header("--- トゲ玉 ---")]

        [SerializeField]
        [Tooltip("トゲ玉の落し物データ")]
        private CollectibleData _spikeBallData;

        [SerializeField]
        [Min(1)]
        [Tooltip("1回あたりの生成数")]
        private int _spawnCount = 2;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("大きさの倍率")]
        private float _scaleMultiplier = 3.0f;

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


        [Header("--- バリアダメージ ---")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("防衛バリアに当たった時に与えるダメージ")]
        private float _barrierDamage = 20.0f;

        [SerializeField]
        [Tooltip("バリアへダメージが入った時のVFX")]
        private GameObject _barrierHitVfxPrefab;

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

        [SerializeField]
        [Tooltip("ボス自身が危険圏へ投げ込まないよう、危険圏内の散布点を除外する")]
        private bool _avoidDangerZone = true;


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

            if (_avoidDangerZone)
            {
                RemoveDangerZonePoints();
            }

            if (_points.Count == 0 || _spikeBallData == null)
            {
                Debug.LogWarning("[SpikeBall] データ未設定、または散布点が0のため生成できません。");

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


        // 内部処理
        // ------------------------------------------------------------

        /// <summary>
        /// トゲ玉を1個投げる
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

            // Toge型は既定でロックされているため、ボスギミックではアンロック判定を無視する
            CollectibleObject ball = spawner.SpawnWithVelocity(_spikeBallData, from, Vector3.zero, _scaleMultiplier, true);

            if (ball == null)
            {
                return;
            }

            // 初速は与えず、放物線上を直接動かして軌道を保証する
            if (!ball.TryGetComponent(out BocchaThrownItemMover mover))
            {
                mover = ball.gameObject.AddComponent<BocchaThrownItemMover>();
            }

            mover.Begin(from, velocity, gravity, flightTime);

            mover.SetLandingVfx(_landingVfxPrefab, _vfxLifeTime);

            // 落し物はプールの使い回しなので、判定コンポーネントも実行時に付ける
            if (!ball.TryGetComponent(out BocchaSpikeBall spikeBall))
            {
                spikeBall = ball.gameObject.AddComponent<BocchaSpikeBall>();
            }

            spikeBall.Initialize(_barrierDamage, _barrierHitVfxPrefab, _vfxLifeTime);

            if (_drawSpawnPoints)
            {
                BocchaBallistics.DrawTrajectory(from, velocity, gravity, flightTime, 5.0f, Color.red);
            }
        }


        /// <summary>
        /// 危険圏に入っている散布点を取り除く
        /// </summary>
        private void RemoveDangerZonePoints()
        {
            if (!BocchaBarrierDangerZone.HasAnyZone)
            {
                return;
            }

            for (int i = _points.Count - 1; i >= 0; --i)
            {
                if (!BocchaBarrierDangerZone.TryGetZone(_points[i], out _))
                {
                    continue;
                }

                _points.RemoveAt(i);
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
                Debug.LogWarning("[SpikeBall] CollectibleSpawnerがシーンに見つかりません。");
            }

            return _spawner;
        }


        public void ApplyTuning(BocchaRoundTuning tuning)
        {
            if (tuning == null) return;

            _spawnCount = Mathf.Max(0, tuning.SpikeBallCount);
        }
    }
}
