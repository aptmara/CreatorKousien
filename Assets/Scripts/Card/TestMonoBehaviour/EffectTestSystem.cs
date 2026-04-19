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
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
