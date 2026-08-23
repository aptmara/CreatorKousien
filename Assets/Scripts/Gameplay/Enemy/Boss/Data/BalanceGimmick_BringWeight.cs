using Game.Data.Collectibles;
using Game.Gameplay.Collectibles;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "BalanceGimmick_BringWeight", menuName = "Boss/Gimmick/Balance_BringWeight")]
public class BalanceGimmick_BringWeight : BossGimmickSO
{
    private enum DeliveryType { WeaknessObject,Collectible};
    [Header("==== 弱点のオブジェクト候補 ====")]
    [SerializeField] List<GameObject> _weaknessPrefabs = new List<GameObject>();

    [Header("==== Collectible候補 ====")]
    [SerializeField] List<CollectibleData> _spawnCollectibleList = new List<CollectibleData>();

    [Header("==== 抽選比率 =====")]
    [SerializeField, Tooltip("弱点オブジェクトが選ばれる確率")]
    private float _weaknessObjectChance = 0.3f;

    [SerializeField,Tooltip("物を運んでくるキャラ")] GameObject _SummonMonster;

    private BossBalanceBeamController _beamController;
    private CollectibleSpawner _collectibleSpawner;
    private GameObject _SpawnedMonster;
    private TraySide _targetSide;

    private bool _isComplete = false;
    public override bool IsComplete=>_isComplete;
    public override bool IsTick => true;


    public override void Initialize(BossContext context)
    {
        base.Initialize(context);
        _beamController = context.Transform.GetComponentInChildren<BossBalanceBeamController>();
        _collectibleSpawner = UnityEngine.Object.FindFirstObjectByType<CollectibleSpawner>();
    }

    public override void Execute()
    {
        _isComplete = false;
        
        if(!_SummonMonster.TryGetComponent<Balance_BringMonster>(out _))
        {
            Debug.Log("[BringWeight] Balance_BringMonsterを持っていません");
            _isComplete = true;
            return;
        }

        DeliveryType deliveryType = ChooseDeliveryType();
        if(deliveryType == DeliveryType.WeaknessObject && _weaknessPrefabs.Count == 0)
        {
            deliveryType = DeliveryType.Collectible;
        }
        if(deliveryType == DeliveryType.Collectible && _spawnCollectibleList.Count == 0)
        {
            _isComplete = true;
            return;
        }
        
        _targetSide = UnityEngine.Random.value < 0.5f ? TraySide.Left : TraySide.Right;
        Transform targetTrans = _beamController.GetTraySocket(_targetSide);

        Vector3 spawnPos = targetTrans.position + new Vector3(70.0f * (_targetSide == TraySide.Left ? -1.0f : 1.0f),5.0f,0.0f);

        _SpawnedMonster = Instantiate(_SummonMonster, spawnPos, Quaternion.identity);
        var monster = _SpawnedMonster.GetComponent<Balance_BringMonster>();

        System.Action<Vector3, Quaternion> onArrived = deliveryType == DeliveryType.WeaknessObject ?
            (pos, rot) => SpawnWeaknessObject(pos, rot) :
            (pos, rot) => SpawnCollectible(pos);

        monster.Initialize(spawnPos, targetTrans.position, onArrived);
        
    }

    public override void Tick(float dt)
    {
        if(_SpawnedMonster == null) { _isComplete = true; return; }

        var comp = _SpawnedMonster.GetComponent<Balance_BringMonster>();
        if(comp.IsSpawned)
        {
            _isComplete = true;
        }
    }

    public override void Cancel()
    {
        _isComplete = true;
    }


    private DeliveryType ChooseDeliveryType()
    {
        return UnityEngine.Random.value < _weaknessObjectChance ?
            DeliveryType.WeaknessObject :
            DeliveryType.Collectible;
    }

    private void SpawnWeaknessObject(Vector3 position, Quaternion rotation)
    {
        var prefab = _weaknessPrefabs[UnityEngine.Random.Range(0, _weaknessPrefabs.Count)];
        var instance = Instantiate(prefab, position, rotation);

        if (instance.TryGetComponent(out IBossTrayItem item))
        {
            _beamController.RegisterItem(_targetSide, item);
        }
    }

    private void SpawnCollectible(Vector3 position)
    {
        if (_collectibleSpawner == null) return;

        var data = _spawnCollectibleList[UnityEngine.Random.Range(0, _spawnCollectibleList.Count)];
        _collectibleSpawner.SpawnSpecificAt(data, position);
    }

}
