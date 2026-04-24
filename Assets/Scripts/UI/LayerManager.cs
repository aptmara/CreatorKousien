//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// @file   LayerManager.cs
// @brief  レイヤーを管理
// @author 山本郁也
// @date   2026/04/15
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;
using System.Collections.Generic;

public class LayerManager : MonoBehaviour
{
    private readonly Stack<GameObject> focusStack = new Stack<GameObject>();

    /// <summary>
    /// 指定Viewの描画優先度を設定する
    /// </summary>
    /// <param name="viewobj">対象View</param>
    /// <param name="order">設定するソート順</param>
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

    /// <summary>
    /// 指定Viewをフォーカススタックの先頭に積む
    /// </summary>
    /// <param name="viewObj">フォーカス対象のView</param>
    public void PushFocus(GameObject viewObj)
    {
        if(viewObj == null)
        {
            return;
        }
        focusStack.Push(viewObj);
    }
    /// <summary>
    /// 現在の最前面フォーカスを取り除く
    /// </summary>
    public void PopTopFocus()
    {
        if (focusStack.Count == 0)
        {
            return;
        }
        focusStack.Pop();
    }

    /// <summary>
    /// 現在フォーカスを持つ最前面Viewを取得する
    /// </summary>
    /// <returns>最前面のView。存在しない場合はnull</returns>
    public GameObject GetTopLayer()
    {
        return focusStack.Count > 0 ? focusStack.Peek() : null;
    }

    /// <summary>
    /// 指定Viewが現在フォーカスを持っているか判定する
    /// フォーカスが存在しない場合は入力可能とみなす
    /// </summary>
    /// <param name="target">判定対象のView</param>
    /// <returns>入力対象ならtrue</returns>
    public bool HasFocus(GameObject target)
    {
        if (focusStack.Count == 0)
        {
            return true;
        }
        return focusStack.Peek() == target;
    }

    /// <summary>
    /// すべてのフォーカス情報をクリアする
    /// </summary>
    public void ClearFocus()
    {
        focusStack.Clear();
    }

}
