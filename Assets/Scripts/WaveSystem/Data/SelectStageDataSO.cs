using UnityEngine;
using UnityEngine.UI;
using Game.WaveSystem;
using System;

[CreateAssetMenu(fileName = "SelectStageDataSO", menuName = "Game/StageSelect/SelectStageDataSO")]
[Serializable]
public class SelectStageDataSO : ScriptableObject
{
    [Header("ステージ名")]
    [SerializeField] string _stageName;
    public string StageName => _stageName;
    [Header("ステージ説明文")]
    [SerializeField] string _stageInfo;
    public string StageInfo => _stageInfo;

    [Header("アイコン")]
    [SerializeField] Sprite _stageIcon;
    public Sprite StageIcon => _stageIcon;
    [Header("移動先ステージ")]
    [SerializeField] StageDataSO _stageData;
    public StageDataSO StageData => _stageData;

}
