/**
 * 使用するサウンドのデータをまとめたSO
 * 
 * テラダ
 */

using System;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SoundData", menuName = "Scriptable Objects/SoundData")]
public class SoundData : ScriptableObject
{
    [Serializable]
    public struct AudioData
    {
        public AK.Wwise.Event WwiseEvent;
        public string Name;
        public int bpm;
    }

    [SerializeField]
    public List<AudioData> SEDataList;

    [SerializeField]
    public List<AudioData> BGMDataList;

}
