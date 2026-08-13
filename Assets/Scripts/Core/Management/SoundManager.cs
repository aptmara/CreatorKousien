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

    AudioSource _bgmAudioSource;
    AudioSource _seAudioSource;

    [SerializeField]
    private SoundData _soundData;
    private float _masterVolume = 1.0f;
    private float _bgmVolume = 1.0f;
    private float _seVolume = 1.0f;
    private float _bgmVolumeMultiplier = 1.0f;

    [Header("==== 揺れのBPM対応 ====")]
    [SerializeField, Tooltip("BPMに合わせたい揺れるマテリアル")]
    private List<Material> _swayMaterial;
    private SoundData.AudioData _currentBGM;
    private int _bpm = 100;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        _bgmAudioSource = GetComponent<AudioSource>();
        _seAudioSource = gameObject.AddComponent<AudioSource>();
        _seAudioSource.playOnAwake = false;
        _seAudioSource.spatialBlend = 0.0f;
        ApplyVolumes();
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }


    private void FixedUpdate()
    {
        if (_bgmAudioSource == null)
        {
            return;
        }

        double beat = 1.0f;
        if (!_bgmAudioSource.isPlaying)
        {
            beat = Time.time * (float)_bpm / 60.0f;
        }
        else
        {
            beat = _bgmAudioSource.time * _currentBGM.bpm / 60.0f;
        }
        Debug.Log(beat);
        foreach(var mat in _swayMaterial)
        {
            mat.SetFloat("_Beat", (float)beat);
        }
    }

    public void PlaySE(string name,float volume = 1.0f)
    {
        if (_seAudioSource == null || _soundData == null)
        {
            return;
        }

        var SEData = _soundData.SEDataList.Find(x => x.Name == name);
        if (SEData.AudioClip == null)
        {
            return;
        }
        
        Debug.Log("[SoundManager]PlaySE : " + name);
        _seAudioSource.PlayOneShot(SEData.AudioClip, volume);
    }

    public void PlayBGM(string name)
    {
        if (_bgmAudioSource == null) return;
        var BGMData = _soundData.BGMDataList.Find(x => x.Name == name);
        
        var bgm = BGMData.AudioClip;

        _currentBGM = BGMData;
        _bgmAudioSource.clip = bgm;
        _bgmAudioSource.loop = true;
        _bgmAudioSource.Play();
        Debug.Log("[SoundManager]PlayBGM : " + name);
    }

    public void StopBGM()
    {
        _bgmAudioSource?.Stop();
    }

    public void PauseBGM()
    {
        _bgmAudioSource?.Pause();
    }

    public void SoundVolume(float volume)
    {
        _bgmVolumeMultiplier = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetBGMVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetSEVolume(float volume)
    {
        _seVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void ApplyVolumeSettings(float masterVolume, float bgmVolume, float seVolume)
    {
        _masterVolume = Mathf.Clamp01(masterVolume);
        _bgmVolume = Mathf.Clamp01(bgmVolume);
        _seVolume = Mathf.Clamp01(seVolume);
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        AudioListener.volume = _masterVolume;

        if (_bgmAudioSource != null)
        {
            _bgmAudioSource.volume = _bgmVolume * _bgmVolumeMultiplier;
        }

        if (_seAudioSource != null)
        {
            _seAudioSource.volume = _seVolume;
        }

        if (AkUnitySoundEngine.IsInitialized())
        {
            AkUnitySoundEngine.SetOutputVolume(0, _masterVolume * _seVolume);
        }
    }
}
