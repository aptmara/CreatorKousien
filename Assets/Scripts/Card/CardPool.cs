using UnityEngine;

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
    //! カードプール
    [SerializeField]
    PoolData[] data;
    public PoolData[] Data => data;

}
