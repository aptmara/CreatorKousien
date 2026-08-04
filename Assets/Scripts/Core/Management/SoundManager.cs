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

    [Header("==== 揺れのBPM対応 ====")]
    [SerializeField, Tooltip("BPMに合わせたい揺れるマテリアル")]
    private List<Material> _swayMaterial;
    private SoundData.AudioData _currentBGM;
    private int _bpm = 100;

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

        double beat = 1.0f;
        if (!_audioSrc.isPlaying)
        {
            beat = Time.time * (float)_bpm / 60.0f;
        }
        else
        {
            beat = _audioSrc.time * _currentBGM.bpm / 60.0f;
        }
        Debug.Log(beat);
        foreach(var mat in _swayMaterial)
        {
            mat.SetFloat("_Beat", (float)beat);
        }
    }

    public void PlaySE(string name,float volume = 1.0f)
    {
        var SEData = _soundData.SEDataList.Find(x => x.Name == name);
        
        Debug.Log("[SoundManager]PlaySE : " + name);
        _audioSrc.PlayOneShot(SEData.AudioClip,volume);
    }

    public void PlayBGM(string name)
    {
        if (_audioSrc == null) return;
        var BGMData = _soundData.BGMDataList.Find(x => x.Name == name);
        
        var bgm = BGMData.AudioClip;

        _currentBGM = BGMData;
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
