using Game.Core.Events;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialUIController : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI text;
    [SerializeField] public GameObject textObject;

    [Header("Typewriter")]
    [SerializeField, Min(0f)] private float _charInterval = 0.03f;

    private Coroutine _typingCoroutine;

    private void OnEnable()
    {
        EventBus.Subscribe<TutorialTextEvent>(OnDrawText);
        EventBus.Subscribe<TutorialTextResetEvent>(OnResetText);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<TutorialTextEvent>(OnDrawText);
        EventBus.Unsubscribe<TutorialTextResetEvent>(OnResetText);
        StopTyping();
    }

    void OnDrawText(TutorialTextEvent ev)
    {
        // ブロッキングせずにタイプ表示を開始します — 確認は既存のシステム（TutorialFlowManager等）が扱います
        if (textObject != null) textObject.SetActive(true);
        StartTyping(ev.Text);
    }

    void OnResetText(TutorialTextResetEvent ev)
    {
        StopTyping();
        if (textObject != null) textObject.SetActive(false);
    }

    private void StartTyping(string content)
    {
        StopTyping();
        if (text == null)
        {
            return;
        }

        if (_charInterval <= 0f)
        {
            text.text = content ?? string.Empty;
            return;
        }

        _typingCoroutine = StartCoroutine(TypeTextRealtime(content));
    }

    private void StopTyping()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
    }

    private System.Collections.IEnumerator TypeTextRealtime(string content)
    {
        text.text = string.Empty;
        if (string.IsNullOrEmpty(content))
        {
            _typingCoroutine = null;
            yield break;
        }

        for (int i = 0; i < content.Length; i++)
        {
            text.text += content[i];
            yield return new WaitForSecondsRealtime(_charInterval);
        }

        _typingCoroutine = null;
    }

    /// <summary>
    /// タイプライター効果で文字を表示し、必要なら確認入力(クリック/決定)を待機します。
    /// waitForConfirm が true の場合、clickAction または submitAction（または両方）を渡してください。
    /// </summary>
    public System.Collections.IEnumerator ShowTextRoutine(string content, bool waitForConfirm = false, InputAction clickAction = null, InputAction submitAction = null)
    {
        if (textObject != null) textObject.SetActive(true);
        StartTyping(content);

        // 確認待ちでない場合は、タイプ表示が完了するまで待って終了します
        if (!waitForConfirm)
        {
            // タイプ表示コルーチンの完了を待つ
            while (_typingCoroutine != null)
            {
                yield return null;
            }
            yield break;
        }

        // 待機中はアクションが有効になっていることを保証する
        bool clickWasEnabled = clickAction != null && clickAction.enabled;
        bool submitWasEnabled = submitAction != null && submitAction.enabled;
        if (clickAction != null && !clickAction.enabled) clickAction.Enable();
        if (submitAction != null && !submitAction.enabled) submitAction.Enable();

        // まずタイプ表示の完了を待つ
        while (_typingCoroutine != null)
        {
            yield return null;
        }

        // 次にいずれかのアクションが発火するのを待つ
        while ((clickAction == null || !clickAction.triggered) && (submitAction == null || !submitAction.triggered))
        {
            yield return null;
        }

        // 変更した場合はアクションの有効/無効状態を復元する
        if (clickAction != null && !clickWasEnabled) clickAction.Disable();
        if (submitAction != null && !submitWasEnabled) submitAction.Disable();
    }
}
