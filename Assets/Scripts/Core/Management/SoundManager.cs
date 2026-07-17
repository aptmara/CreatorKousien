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
using Unity.VisualScripting;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance { get; set; }

    AudioSource _audioSrc;

    [SerializeField]
    private SoundData _soundData;
    private float _playVolume = 1.0f;
    private float _erapsedTime;
    [SerializeField] private float _duration = 1.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(instance == null)
            instance = this;
        DontDestroyOnLoad(this);
        _audioSrc = GetComponent<AudioSource>();
    }


    private void FixedUpdate()
    {
        if(_erapsedTime < _duration)
        {
            _erapsedTime += Time.unscaledDeltaTime;
        }
        _playVolume = Mathf.Lerp(_audioSrc.volume, _playVolume, _erapsedTime / _duration);
    }

    public void PlaySE(string name,float volume = 1.0f)
    {
        var se = _soundData.SEDataList.Find(x => x.Name == name).AudioClip;
        Debug.Log("[SoundManager]PlaySE : " + name);
        _audioSrc.PlayOneShot(se,volume);
    }

    public void PlayBGM(string name)
    {
        if (_audioSrc == null) return;
        var bgm = _soundData.BGMDataList.Find(x => x.Name == name).AudioClip;
        _audioSrc.clip = bgm;
        _audioSrc.loop = true;
        _audioSrc.Play();
        Debug.Log("[SoundManager]PlayBGM : " + name);
    }

    public void StopBGM()
    {
        _audioSrc?.Stop();
    }

    public void PauseBGM()
    {
        _audioSrc?.Pause();
    }

    public void SoundVolume(float volume)
    {
        _audioSrc.volume = volume;
        _erapsedTime = 0.0f;
    }
}
