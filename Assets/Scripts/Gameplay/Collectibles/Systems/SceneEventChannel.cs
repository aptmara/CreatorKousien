/*
 * SOを経由してシーン間実行してみます
 * 
 * 
 */

using UnityEngine;
using System;

[CreateAssetMenu(fileName = "SceneEventChannel", menuName = "Scriptable Objects/SceneEventChannel")]
public class SceneEventChannel : ScriptableObject
{
    public event Action<int> OnExecuteInt;
    public event Action<float> OnExecuteFloat;

    public void ExecuteEvent(int val)
    {
        OnExecuteInt?.Invoke(val);
    }

    public void ExecuteEvent(float val)
    {
        OnExecuteFloat?.Invoke(val);
    }
}
