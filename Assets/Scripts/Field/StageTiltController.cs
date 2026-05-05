using UnityEngine;
using System.Collections.Generic;

public class StageTiltController : MonoBehaviour
{
    [Tooltip("生成オブジェクト")]
    [SerializeField]
    private StageData _stageData;

    
    private GameObject _tiltField;

    private List<GameObject> _Collectionspawners;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _tiltField = Instantiate(_stageData.FieldGroundPrefab);
        _tiltField.transform.localScale = new Vector3(_stageData.width,1.0f,_stageData.height);

        foreach(var obj in _stageData.spawns)
        {
            _Collectionspawners.Add(Instantiate(obj.SpawnerPrefab,obj.position,new Quaternion(0,0,0,0)));
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
