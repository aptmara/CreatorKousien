using UnityEngine;

[System.Serializable]
public struct PoolData
{
    [SerializeField]
    int cardID;
    public int CardID => cardID; 

    [SerializeField]
    float rate;
    public float Rate => rate;
}

[CreateAssetMenu(fileName = "SO_CardPool", menuName = "CreatorKousien/Data/CardPool")]
public class CardPool : ScriptableObject
{
    [SerializeField]
    PoolData[] data;


    public PoolData[] Data => data;

}
