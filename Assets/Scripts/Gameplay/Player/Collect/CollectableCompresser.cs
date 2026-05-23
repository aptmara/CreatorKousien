using UnityEngine;
using System.Collections.Generic;
using Game.Gameplay.Collectibles;


namespace Game.Gameplay.Player {

public class CollectableCompresser : MonoBehaviour
{
    public GameObject compressedPrefab;

        [SerializeField, Tooltip("一回の圧縮に使用する個数")]
        private int _compressAmount = 10;

    private List<CollectibleObject> _nearbyCollectable = new();


        private void OnTriggerEnter(Collider other)
        {
            // 収集範囲ないに入ったら圧縮対象リストに追加
            if (other.TryGetComponent(out CollectibleObject obj))
            {
                _nearbyCollectable.Add(obj);
                // 圧縮できるか確認
                CheckCompress();
            }
        }


        private void OnTriggerExit(Collider other)
        {
            // 収集範囲外に出たら圧縮対象リストから削除
            if (other.TryGetComponent(out CollectibleObject obj))
            {
                _nearbyCollectable.Remove(obj);
            }
        }


        void CheckCompress()
        {
            // 圧縮個数に達していたら
            if (_nearbyCollectable.Count < _compressAmount) return;
            // 圧縮
            Compress();
        }


        void Compress()
        {
            Vector3 center = Vector3.zero;

            for (int i = 0; i < _compressAmount; ++i)
            {
                center += _nearbyCollectable[i].transform.position;
            }
            
            center /= _compressAmount;
            // 圧縮後のオブジェクトを生成
            var obj = Instantiate(
                compressedPrefab,
                center,
                Quaternion.identity
                );
            // 圧縮個数から解放時の個数を設定
            var comp = obj.GetComponent<CompressCollectable>();
            comp.amount = _compressAmount;

            // 圧縮対象のオブジェクトを削除と同時に種類を保存
            for (int i = 0; i < _compressAmount; ++i)
            {
                comp.expandDataList.Enqueue(_nearbyCollectable[i].GetCollectableData());
                Destroy(_nearbyCollectable[i].gameObject);
            }

            _nearbyCollectable.RemoveRange(0, _compressAmount);
        }
    }

}
