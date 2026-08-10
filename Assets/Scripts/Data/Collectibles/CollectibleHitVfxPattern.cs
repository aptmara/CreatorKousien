using UnityEngine;



[CreateAssetMenu(fileName = "CollectibleHitVfxPattern", menuName = "Game/Collectible/CollectibleHitVfxPattern")]
public class CollectibleHitVfxPattern : ScriptableObject
{
    [Header("認識用ID")]
    public string ID;
    [Header("命中時のVfxパターン")]
    [SerializeField]
    public GameObject[] HitPattern;

}
