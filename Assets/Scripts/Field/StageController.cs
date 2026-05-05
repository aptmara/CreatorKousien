using TMPro.EditorUtilities;
using UnityEngine;

public class StageController
{
    [Header("生成するオブジェクト")]

    [SerializeField]
    StageData _stageData;

    public StageData StageData => _stageData;

    public void Initialize(StageData stageData)
    {
        _stageData = stageData;

        
    }
}
