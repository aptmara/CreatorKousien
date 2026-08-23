using Game.Core.Events;
using UnityEngine;
using UnityEngine.UI;

public class StageSelectUIController : MonoBehaviour
{
    [SerializeField] TMPro.TextMeshProUGUI stageName;
    [SerializeField] TMPro.TextMeshProUGUI stageInfo;
    [SerializeField] Image stageIcon;

    [SerializeField] Image back;
    private void OnEnable()
    {
        EventBus.Subscribe<StageCursorSelectEvent>(OnSelectCursor);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<StageCursorSelectEvent>(OnSelectCursor);
    }

    private void Update()
    {
        
    }

    void OnSelectCursor(StageCursorSelectEvent ev)
    {
        stageName.text = ev.StageName;
        stageInfo.text = ev.StageInfo;
        stageIcon.sprite = ev.StageIcon;
    }
}
