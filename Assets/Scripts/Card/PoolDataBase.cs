using UnityEngine;

[CreateAssetMenu(fileName = "SO_PoolDataBase", menuName = "CreatorKousien/Data/PoolDataBase")]
public class PoolDataBase : ScriptableObject
{
    [SerializeField]
    CardPool[] _poolList;

    [SerializeField]
    CardPool _fallBackPool;
    public CardPool FallBackPool => _fallBackPool;

    public CardPool GetPool(int a_poolID)
    {
        // 見つからなかった時用のデータを挿入
        CardPool ret = _fallBackPool;
        // IDで探索
        foreach (CardPool data in _poolList)
        {
            // IDが違ったら次へ
            if (data.PoolID != a_poolID) continue;
            // IDが等しかったら上書きして抜ける
            ret = data;
            break;
        }
        return ret;
    }
}
