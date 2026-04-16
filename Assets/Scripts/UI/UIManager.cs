using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Build.Content;
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

    private void Awake()
    {
        BuildMap();
        BuildStateMap();
    }

    private void ChangeState(UIState newState)
    {
        currentState = newState;
        ApplyState(newState);
    }

    public UIState GetCurrentState()
    {
        return currentState;
    }

    public void OpenView(ViewType type)
    {
        if(viewMap.TryGetValue(type,out GameObject obj) && obj != null)
        {
            obj.SetActive(true);
        }
    }

    public void CloseView(ViewType type)
    {
        if (viewMap.TryGetValue(type, out GameObject obj) && obj != null)
        {
            obj.SetActive(false);
        }
    }

    public bool IsViewOpen(ViewType type)
    {
        if(viewMap.TryGetValue(type,out GameObject obj) && obj != null)
        {
            return obj.activeSelf;
        }
        return false;
    }

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
