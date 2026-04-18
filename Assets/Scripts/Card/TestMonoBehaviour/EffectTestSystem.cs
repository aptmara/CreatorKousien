using UnityEngine;
using CreatorKousien.Effect;
public class EffectTestSystem : MonoBehaviour
{
    [SerializeField]
    EffectDataBase _dataBase;

    EffectSystem _effectSystem;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _effectSystem = new EffectSystem(_dataBase);

        _dataBase.CheckAllData();
        _effectSystem.RegisterData(0);
        _effectSystem.RegisterData(1);
        _effectSystem.ApplyAllEffect();
        Debug.Log("ターンを進めます");
        _effectSystem.DurationUpdate(1);
        _effectSystem.ApplyAllEffect();
        Debug.Log("ターンを進めます");
        _effectSystem.DurationUpdate(1);
        _effectSystem.ApplyAllEffect();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
