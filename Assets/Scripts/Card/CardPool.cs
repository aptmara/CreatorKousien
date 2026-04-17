using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct PoolData
{
    //! ID
    [SerializeField]
    int cardID;
    public int CardID => cardID; 
    //! 出現率
    [SerializeField]
    float rate;
    public float Rate => rate;
}

[CreateAssetMenu(fileName = "SO_CardPool", menuName = "CreatorKousien/Data/CardPool")]
public class CardPool : ScriptableObject
{
    [SerializeField]
    private int _poolID;
    public int PoolID => _poolID;

    //! カードプール、Listとして受け取りたい場面が多そうなのでこう保持
    [SerializeField]
    private List<PoolData> _dataList;
    public List<PoolData> DataList => _dataList;

}
