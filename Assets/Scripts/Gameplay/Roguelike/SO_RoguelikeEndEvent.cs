//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : SO_RoguelikeEndEvent.cs
// brief  : ローグライクシーン終了通知
//
// auther : Shohei Takitani
// date   : 2026/06/30 - begin.
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "SO_RoguelikeEndEvent", menuName = "Scriptable Objects/SO_RoguelikeEndEvent")]
public class SO_RoguelikeEndEvent : ScriptableObject
{
    public event Action OnRaised;

    public void Raise()
    {
        OnRaised?.Invoke();
    }
}
