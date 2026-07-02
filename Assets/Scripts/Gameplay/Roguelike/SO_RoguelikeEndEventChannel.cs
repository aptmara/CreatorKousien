//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : SO_RoguelikeEndEventChannel.cs
// brief  : 移動速度Up.
//
// auther : Shohei Takitani
// date   : 2026/07/01 - begin
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "SO_RoguelikeEndEventChannel", menuName = "Scriptable Objects/SO_RoguelikeEndEventChannel")]
public class SO_RoguelikeEndEventChannel : ScriptableObject
{
    public event Action OnRaised;

    public void Raise()
    {
        OnRaised?.Invoke();
    }
}
