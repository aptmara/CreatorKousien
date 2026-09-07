// ------------------------------------------------------------
// File		: BocchaHazardGimmickBase.cs
// Summary	: お邪魔アイテム生成ギミックの基底クラス
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - 散布座標はBocchaScatterSettingsが担当し、方向の偏りが出ないようにする。
// - 生成間隔を空けることで、一度に大量生成して負荷が跳ねるのを防ぐ。
// ------------------------------------------------------------
using System.Collections.Generic;
using Game.Core.Events;
using Game.Gameplay.Stage;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// お邪魔アイテム生成ギミックの基底クラス。
    /// </summary>
    public abstract class BocchaHazardGimmickBase : BossGimmickSO
    {
        [Header("--- 生成設定 ---")]

        [SerializeField]
        [Tooltip("生成するお邪魔アイテムのPrefab")]
        private GameObject _hazardPrefab;

        [SerializeField]
        [Min(1)]
        [Tooltip("1回あたりの生成数")]
        private int _spawnCount = 2;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("大きさの倍率")]
        private float _scaleMultiplier = 1.0f;

        [SerializeField]
        [Tooltip("投げ出す位置のソケット。Prefab側の落下方式がArcFromSourceのときに使用する")]
        private BossSocket _throwSocket = BossSocket.Muzzle;

        [SerializeField]
        [Min(0f)]
        [Tooltip("1個ずつ生成する間隔")]
        private float _spawnInterval = 0.15f;


        [Header("--- 散布パターン ---")]

        [SerializeField]
        [Tooltip("散布座標の計算設定")]
        private BocchaScatterSettings _scatter = new BocchaScatterSettings();

        [SerializeField]
        [Tooltip("ONならフィールド中心、OFFならボス位置を散布の中心にする")]
        private bool _scatterFromFieldCenter = true;


        private readonly List<Vector3> _points = new List<Vector3>();

        private float _timer;
        private int _spawnedCount;
        private bool _isComplete;


        public override bool IsComplete => _isComplete;

        public override bool IsTick => true;

        /// <summary>生成するお邪魔アイテムの種類。</summary>
        protected abstract BocchaHazardType HazardType { get; }


        /// <summary>
        /// ギミック開始時の初期化
        /// </summary>
        public override void Execute()
        {
            _isComplete = false;
            _timer = 0.0f;
            _spawnedCount = 0;

            _scatter.BuildPoints(ResolveScatterCenter(), _spawnCount, _points);

            if (_points.Count == 0 || _hazardPrefab == null)
            {
                Debug.LogWarning($"[{HazardType}] Prefab未設定、または散布点が0のため生成できません。");

                _isComplete = true;
            }
        }


        /// <summary>
        /// ギミック開始時の初期化
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

            _timer = _spawnInterval;

            // タイミングが来たら1個生成する
            SpawnOne();

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


        /// <summary>
        /// 生成直後の初期化を行いたいクラス向けの派生イベント
        /// </summary>
        /// <param name="hazard"></param>
        protected virtual void OnHazardSpawned(BocchaHazardBase hazard) { }


        /// <summary>
        /// 1個生成する
        /// </summary>
        private void SpawnOne()
        {
            Vector3 point = _points[_spawnedCount];


            _spawnedCount++;

            GameObject instance = Instantiate(_hazardPrefab, point, Quaternion.identity);

            if (!instance.TryGetComponent(out BocchaHazardBase hazard))
            {
                Debug.LogError($"[{HazardType}] Prefabに{nameof(BocchaHazardBase)}がありません。", instance);
                Destroy(instance);

                return;
            }

            // 落下方式がArcFromSourceの場合、この位置から山なりに投げられる
            hazard.Launch(Context.GetSocket(_throwSocket).position, point, _scaleMultiplier);

            // 生成時のイベントを発行
            OnHazardSpawned(hazard);
        }


        private Vector3 ResolveScatterCenter()
        {
            if (_scatterFromFieldCenter && FieldContext.IsReady)
            {
                return FieldContext.Center;
            }

            return Context.Transform.position;
        }

    }
}
