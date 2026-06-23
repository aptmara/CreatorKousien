using UnityEngine;
using Game.Data.Collectibles;
using System;
using TMPro;
using Game.Gameplay.Collectibles;

namespace Game.Data.Collectibles
{
    public class CrystalOneBreakforScale : MonoBehaviour
    {
        // サイズによる欠片生成量
        [Serializable]
        public struct CollectiblesCount
        {
            [Min(1)] public int _min;
            [Min(1)] public int _max;
        }

        [SerializeField] private DrawVectorforGizmo _gizmo;
        [SerializeField] private Transform _mainCrystal;
        [SerializeField] private CrystalShardEmitter _shardEmitter;

        [SerializeField] public CrystalShardEmitter.WeightedShardData[] _shard;

        //--- サイズによる欠片生成量の上下限
        [SerializeField, Min(1)] public int _min = 1;
        [SerializeField, Min(5)] public int _max = 1;

        // 初期サイズ倍率
        [SerializeField] private float _crystalInitScale = 1.5F;

        // サイズ上限 この上限を基準に欠片の生成量が決まる
        [SerializeField] public Vector3 CrystalMaxScale = new Vector3(1.5f,1.5f,1.5f);

        // 射出力の倍率
        [SerializeField] private float EmissionPower = 1.0F;

        // 最大になるまでの秒数(float)
        [SerializeField] public float MaxScaleCount = 10.0F;

        [SerializeField]private float _currentScaleCount;

        private Vector3 _emitVector;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            gameObject.transform.localScale = CrystalMaxScale * _crystalInitScale;
            _currentScaleCount = _crystalInitScale;
            if(_gizmo != null)
            {
                _emitVector = (_gizmo._origin.transform.position - _gizmo._target.transform.position).normalized;
            }
        }
    
        // Update is called once per frame
        void FixedUpdate()
        {
            // 最大時間を超過している場合秒数を更新しない
            if(_currentScaleCount >= MaxScaleCount)
            {
                _currentScaleCount = MaxScaleCount;
            }
            // そうでない場合更新
            else
            {
                _currentScaleCount += Time.deltaTime;
            }

            // サイズに最大から現在の経過時間でサイズを更新
            gameObject.transform.localScale = CrystalMaxScale * (_currentScaleCount / MaxScaleCount);
        }

        [ContextMenu("DEBUG: Emit Shards")]
        public void Emits()
        {
            if(_shardEmitter != null)
            {
                int max = UnityEngine.Random.Range(_min, _max);
                max = (int)((float)(_max) * (_currentScaleCount / MaxScaleCount));
                for(int i = 0; i < max;i++)
                _shardEmitter.Emitter(_emitVector,_gizmo._emissionAngle,EmissionPower,_shard);
            }
            _currentScaleCount = 0.00001f;
        }
    }

}
