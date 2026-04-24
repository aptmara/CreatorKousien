//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// @file   UIManager.cs
// @brief  入力を取得。UI個別ではなく、共通操作の取得
// @author 山本郁也
// @date   2026/04/15
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [System.Serializable]
    private class ViewEntry
    {
        public ViewType viewtype;
        public GameObject RootObject;
    }

    [System.Serializable]
    private class StateDefinition
    {
        public UIState state;
        public List<ViewType> ActioveView = new List<ViewType>();
    }

    [SerializeField] private List<ViewEntry> viewEntries = new List<ViewEntry>();
    [SerializeField] private List<StateDefinition> stateDeiniftions = new List<StateDefinition>();

    private readonly Dictionary<ViewType, GameObject> viewMap = new Dictionary<ViewType, GameObject>();
    private readonly Dictionary<UIState, List<ViewType>> stateMap = new Dictionary<UIState, List<ViewType>>();

    private UIState currentState = UIState.None;
    private bool isHandInputBlocked;

    private void Awake()
    {
        BuildMap();
        BuildStateMap();
    }

    /// <summary>
    /// UI状態を変更し、対応するView表示に切り替える
    /// </summary>
    /// <param name="newState">遷移先のUI状態</param>
    public void ChangeState(UIState newState)
    {
        currentState = newState;
        ApplyState(newState);
    }

    /// <summary>
    /// 現在のUI状態を取得する
    /// </summary>
    /// <returns>現在のUI状態</returns>
    public UIState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// 手札入力のブロック状態を設定する
    /// </summary>
    /// <param name="blocked">入力を禁止するならtrue</param>
    public void SetHandInputBlocked(bool blocked)
    {
        isHandInputBlocked = blocked;
    }
    /// <summary>
    /// 指定したViewを個別に表示する
    /// </summary>
    /// <param name="type">表示対象のView種別</param>
    public void OpenView(ViewType type)
    {
        if(viewMap.TryGetValue(type,out GameObject obj) && obj != null)
        {
            obj.SetActive(true);
        }
    }

    /// <summary>
    /// 手札入力のブロック状態を設定する
    /// </summary>
    /// <param name="type">入力を禁止するならtrue</param>
    public void CloseView(ViewType type)
    {
        if (viewMap.TryGetValue(type, out GameObject obj) && obj != null)
        {
            obj.SetActive(false);
        }
    }

    /// <summary>
    /// 指定したViewが現在表示中か判定する
    /// </summary>
    /// <param name="type">判定対象のView種別</param>
    /// <returns>表示中ならtrue</returns>
    public bool IsViewOpen(ViewType type)
    {
        if(viewMap.TryGetValue(type,out GameObject obj) && obj != null)
        {
            return obj.activeSelf;
        }
        return false;
    }

    /// <summary>
    /// 手札入力を許可している状態か判定する
    /// </summary>
    /// <returns>手札入力を受け付ける状態ならtrue</returns>
    public bool IsHandInputAllowed()
    {
        return currentState == UIState.InGame;
    }

    private void ApplyState(UIState state)
    {
        foreach(var pair in viewMap)
        {
            if(pair.Value != null)
            {
                pair.Value.SetActive(false);
            }
        }
        if(!stateMap.TryGetValue(state,out List<ViewType>activeViews) || activeViews == null)
        {
            return;
        }

        foreach(ViewType type in activeViews)
        {
            OpenView(type);
        }
    }

    private void BuildMap()
    {
        viewMap.Clear();
        foreach(ViewEntry entry in viewEntries)
        {
            if(entry == null || entry.RootObject == null)
            {
                continue;
            }
            if (viewMap.ContainsKey(entry.viewtype))
            {
                continue;
            }

            viewMap.Add(entry.viewtype, entry.RootObject);
        }
    }

    private void BuildStateMap()
    {
        stateMap.Clear();

        foreach(StateDefinition definition in stateDeiniftions)
        {
            if (definition == null)
            {
                continue;
            }
            if(stateMap.ContainsKey(definition.state))
            {
                continue;
            }

            stateMap.Add(definition.state, definition.ActioveView);
        }
    }
}
