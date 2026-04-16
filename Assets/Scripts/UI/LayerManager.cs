using UnityEngine;
using System.Collections.Generic;

public class LayerManager : MonoBehaviour
{
    private readonly Stack<GameObject> focusStack = new Stack<GameObject>();

    public void SetLayerPriority(GameObject viewobj,int order)
    {
        if (viewobj ==null)
        {
            return;
        }
        Canvas canvas = viewobj.GetComponent<Canvas>();
        if(canvas != null)
        {
            canvas.sortingOrder = order;
        }
    }

    public void PushFocus(GameObject viewObj)
    {
        if(viewObj == null)
        {
            return;
        }
        focusStack.Push(viewObj);
    }
    public void PopTopFocus()
    {
        if (focusStack.Count == 0)
        {
            return;
        }
        focusStack.Pop();
    }

    public GameObject GetTopLayer()
    {
        return focusStack.Count > 0 ? focusStack.Peek() : null;
    }

    public bool HasFocus(GameObject target)
    {
        if (focusStack.Count == 0)
        {
            return true;
        }
        return focusStack.Peek() == target;
    }

    public void ClearFocus()
    {
        focusStack.Clear();
    }

}
