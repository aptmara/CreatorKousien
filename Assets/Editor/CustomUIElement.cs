//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file : CustomUIElement.cs
// brief : UIオブジェクトに対して、現在どのような入力状態なのかを表す列挙型と、入力判定APIを追加するためのやつです。
// author : 山本郁也
// data : 2026/05/13
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine; // MonoBehaviourやSerializeFieldなど、Unity基本機能を使うために必要です。
using UnityEngine.EventSystems; // UIのポインター入力イベントを受け取るために必要です。

/// <summary>
/// UIオブジェクトに対して、現在どのような入力状態なのかを表す列挙型です。
/// </summary>
public enum UIActionState
{
    /// <summary>
    /// 何も反応していない状態です。
    /// </summary>
    NoAction,

    /// <summary>
    /// マウスカーソル、またはポインターがUIの上に乗っている状態です。
    /// </summary>
    Hover,

    /// <summary>
    /// UIが押された瞬間の状態です。
    /// </summary>
    Trigger,

    /// <summary>
    /// UIから押下が離された瞬間の状態です。
    /// </summary>
    Released
}

/// <summary>
/// 専用UIオブジェクトに入力判定APIを追加するためのコンポーネントです。
/// </summary>
public class CustomUIElement : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    /// <summary>
    /// このUIオブジェクトを識別するためのIDです。
    /// </summary>
    [SerializeField]
    private int id = -1;

    /// <summary>
    /// 現在ポインターがこのUI上に乗っているかどうかです。
    /// </summary>
    private bool isHover = false;

    /// <summary>
    /// 現在このUIが押されている最中かどうかです。
    /// </summary>
    private bool isPressed = false;

    /// <summary>
    /// 現在の入力状態です。
    /// </summary>
    private UIActionState currentState = UIActionState.NoAction;

    /// <summary>
    /// 1フレーム前の入力状態です。
    /// </summary>
    private UIActionState previousState = UIActionState.NoAction;

    /// <summary>
    /// このUIオブジェクトのIDを取得します。
    /// </summary>
    public int Id => id;

    /// <summary>
    /// 現在の入力状態を取得します。
    /// </summary>
    public UIActionState CurrentState => currentState;

    /// <summary>
    /// 1フレーム前の入力状態を取得します。
    /// </summary>
    public UIActionState PreviousState => previousState;

    /// <summary>
    /// UnityのLateUpdateです。
    /// TriggerやReleasedのような一瞬だけ欲しい状態を、次の状態へ戻すために使います。
    /// </summary>
    private void LateUpdate()
    {
        // 現在の状態を、前回状態として保存します。
        previousState = currentState;

        // Triggerは押した瞬間だけの状態なので、次の状態へ戻す対象です。
        bool isOneFrameTrigger = currentState == UIActionState.Trigger;

        // Releasedは離した瞬間だけの状態なので、次の状態へ戻す対象です。
        bool isOneFrameReleased = currentState == UIActionState.Released;

        // 現在の状態が、1フレームだけ扱いたい状態かどうかを判定します。
        if (isOneFrameTrigger || isOneFrameReleased)
        {
            // ポインターがUI上に残っているならHoverに戻し、外れているならNoActionに戻します。
            currentState = isHover ? UIActionState.Hover : UIActionState.NoAction;
        }
    }

    /// <summary>
    /// このUIオブジェクトのIDを設定します。
    /// </summary>
    /// <param name="value">設定したいIDです。</param>
    public void SetId(int value)
    {
        // 引数で受け取った値をIDとして保存します。
        id = value;
    }

    /// <summary>
    /// 現在の入力状態を取得します。
    /// </summary>
    /// <returns>現在のUIActionStateを返します。</returns>
    public UIActionState GetActionState()
    {
        // 現在の入力状態を返します。
        return currentState;
    }

    /// <summary>
    /// 現在、何も反応していないかどうかを取得します。
    /// </summary>
    /// <returns>NoActionならtrue、それ以外ならfalseです。</returns>
    public bool IsNoAction()
    {
        // 現在の状態がNoActionかどうかを返します。
        return currentState == UIActionState.NoAction;
    }

    /// <summary>
    /// 現在、ポインターがUI上に乗っているかどうかを取得します。
    /// </summary>
    /// <returns>Hoverならtrue、それ以外ならfalseです。</returns>
    public bool IsHover()
    {
        // 現在の状態がHoverかどうかを返します。
        return currentState == UIActionState.Hover;
    }

    /// <summary>
    /// 現在、UIが押された瞬間かどうかを取得します。
    /// </summary>
    /// <returns>Triggerならtrue、それ以外ならfalseです。</returns>
    public bool IsTrigger()
    {
        // 現在の状態がTriggerかどうかを返します。
        return currentState == UIActionState.Trigger;
    }

    /// <summary>
    /// 現在、UIが離された瞬間かどうかを取得します。
    /// </summary>
    /// <returns>Releasedならtrue、それ以外ならfalseです。</returns>
    public bool IsReleased()
    {
        // 現在の状態がReleasedかどうかを返します。
        return currentState == UIActionState.Released;
    }

    /// <summary>
    /// 現在、UIが押されている最中かどうかを取得します。
    /// </summary>
    /// <returns>押されている最中ならtrue、それ以外ならfalseです。</returns>
    public bool IsPressed()
    {
        // 押下中フラグを返します。
        return isPressed;
    }

    /// <summary>
    /// ポインターがこのUIの上に入った時に呼ばれます。
    /// </summary>
    /// <param name="eventData">ポインターイベントの情報です。</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // ポインターがUI上に乗っている状態にします。
        isHover = true;

        // 押下中ではない場合だけ、状態をHoverにします。
        if (!isPressed)
        {
            // 現在の状態をHoverに変更します。
            currentState = UIActionState.Hover;
        }
    }

    /// <summary>
    /// ポインターがこのUIの上から外れた時に呼ばれます。
    /// </summary>
    /// <param name="eventData">ポインターイベントの情報です。</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        // ポインターがUI上に乗っていない状態にします。
        isHover = false;

        // 押下中ではない場合だけ、状態をNoActionに戻します。
        if (!isPressed)
        {
            // 現在の状態をNoActionに変更します。
            currentState = UIActionState.NoAction;
        }
    }

    /// <summary>
    /// このUIが押された時に呼ばれます。
    /// </summary>
    /// <param name="eventData">ポインターイベントの情報です。</param>
    public void OnPointerDown(PointerEventData eventData)
    {
        // 押下中の状態にします。
        isPressed = true;

        // 現在の状態を押された瞬間として扱います。
        currentState = UIActionState.Trigger;
    }

    /// <summary>
    /// このUIの押下が離された時に呼ばれます。
    /// </summary>
    /// <param name="eventData">ポインターイベントの情報です。</param>
    public void OnPointerUp(PointerEventData eventData)
    {
        // 押下中ではない状態にします。
        isPressed = false;

        // 現在の状態を離された瞬間として扱います。
        currentState = UIActionState.Released;
    }
}
