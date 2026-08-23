using UnityEngine;
using UnityEngine.UI;
using Game.WaveSystem;
using Game.Core.Events;

public class StageSelectButton : SelectSceneButtonBase
{
    private Button hasButton;
    [SerializeField] private SelectStageDataSO hasData;


    void Start()
    {
        hasButton = GetComponent<Button>();
    }

    public override void OnClick() 
    {
        hasButton?.onClick.Invoke();
    }

    public override void OnSelectCursor()
    {
        Debug.Log(hasData.name + "を選択");
        hasButton?.Select();
        EventBus.Publish(new StageCursorSelectEvent(hasData.StageName ,hasData.StageInfo, hasData.StageIcon));
    }
}
