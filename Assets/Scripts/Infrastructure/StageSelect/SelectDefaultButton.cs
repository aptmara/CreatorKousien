using Game.Core.Events;
using Game.WaveSystem;
using UnityEngine.UI;
using UnityEngine;

public class SelectDefaultButton : SelectSceneButtonBase
{
    private Button hasButton;


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
        hasButton?.Select();
    }
}
