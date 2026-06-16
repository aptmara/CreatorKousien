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
    public event Action<int> OnExecuteResultRequest;

    public void ExecuteEvent(int val)
    {
        OnExecuteResultRequest?.Invoke(val);
    }

}
