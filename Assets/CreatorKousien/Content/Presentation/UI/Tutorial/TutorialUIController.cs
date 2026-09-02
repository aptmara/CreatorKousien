using Game.Core.Events;
using TMPro;
using UnityEngine;

public class TutorialUIController : MonoBehaviour
{

    [SerializeField] TextMeshProUGUI text;
    [SerializeField] GameObject textObject;
    private void OnEnable()
    {
        EventBus.Subscribe<TutorialTextEvent>(OnDrawText);
        EventBus.Subscribe<TutorialTextResetEvent>(OnResetText);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<TutorialTextEvent>(OnDrawText);
        EventBus.Unsubscribe<TutorialTextResetEvent>(OnResetText);
    }



    void OnDrawText(TutorialTextEvent ev)
    {
        textObject.SetActive(true);
        text.text = ev.Text;
    }

    void OnResetText(TutorialTextResetEvent ev)
    {
        textObject.SetActive(false);
    }
}
