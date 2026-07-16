/**
 * サウンドをまとめつつ、再生を行うクラス
 * 
 * サウンドの登録をSOにて行い、登録したリストから再生を行う
 * 
 * テラダ
 */

using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;

public class SoundManager : MonoBehaviour
{
    static SoundManager instance { get; set; }

    AudioSource _audioSrc;

    [SerializeField]
    private SoundData _soundData;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(this);
        _audioSrc = GetComponent<AudioSource>();
    }

    

    public void PlaySE(string name)
    {
        var se = _soundData.SEDataList.Find(x => x.Name == name).AudioClip;
        _audioSrc.PlayOneShot(se);
    }

    public void PlayBGM(string name)
    {
        _audioSrc.Stop();
        var bgm = _soundData.BGMDataList.Find(x => x.Name == name).AudioClip;
        _audioSrc.clip = bgm;
        _audioSrc.Play();
    }
}
