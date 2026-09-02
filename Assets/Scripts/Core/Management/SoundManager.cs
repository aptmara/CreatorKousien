/**
 * サウンドをまとめつつ、再生を行うクラス
 * 
 * サウンドの登録をSOにて行い、登録したリストから再生を行う
 * 
 * テラダ
 */

using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AkGameObj))]
public class SoundManager : MonoBehaviour
{
    public static SoundManager instance { get; set; }

    [SerializeField]
    private SoundData _soundData;
    [SerializeField]
    private AK.Wwise.RTPC _masterVolumeRtpc;
    [SerializeField]
    private AK.Wwise.RTPC _bgmVolumeRtpc;
    [SerializeField]
    private AK.Wwise.RTPC _seVolumeRtpc;
    private float _masterVolume = 1.0f;
    private float _bgmVolume = 1.0f;
    private float _seVolume = 1.0f;
    private float _bgmVolumeMultiplier = 1.0f;
    private uint _currentBgmPlayingId = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;

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
        AkUnitySoundEngineInitialization.Instance.initializationDelegate += ApplyVolumes;
        AkUnitySoundEngineInitialization.Instance.reInitializationDelegate += ApplyVolumes;
        ApplyVolumes();
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        AkUnitySoundEngineInitialization.Instance.initializationDelegate -= ApplyVolumes;
        AkUnitySoundEngineInitialization.Instance.reInitializationDelegate -= ApplyVolumes;

        if (instance == this)
        {
            instance = null;
        }
    }


    private void FixedUpdate()
    {
        double beat = 1.0f;
        if (_currentBgmPlayingId == AkUnitySoundEngine.AK_INVALID_PLAYING_ID ||
            AkUnitySoundEngine.GetSourcePlayPosition(
                _currentBgmPlayingId,
                out int positionMilliseconds,
                true) != AKRESULT.AK_Success)
        {
            beat = Time.time * (float)_bpm / 60.0f;
        }
        else
        {
            beat = positionMilliseconds * 0.001 * _currentBGM.bpm / 60.0f;
        }
        // Debug.Log(beat);
        foreach(var mat in _swayMaterial)
        {
            mat.SetFloat("_Beat", (float)beat);
        }
    }

    public void PlaySE(string name)
    {
        if (_soundData == null)
        {
            return;
        }

        var SEData = _soundData.SEDataList.Find(x => x.Name == name);
        if (SEData.WwiseEvent == null || !SEData.WwiseEvent.IsValid())
        {
            return;
        }
        
        Debug.Log("[SoundManager]PlaySE : " + name);
        SEData.WwiseEvent.Post(gameObject);
    }

    public void PlayBGM(string name)
    {
        if (_soundData == null) return;

        var BGMData = _soundData.BGMDataList.Find(x => x.Name == name);
        if (BGMData.WwiseEvent == null || !BGMData.WwiseEvent.IsValid()) return;

        StopBGM();
        _currentBGM = BGMData;
        _currentBgmPlayingId = BGMData.WwiseEvent.Post(gameObject);
        Debug.Log("[SoundManager]PlayBGM : " + name);
    }

    public void StopBGM()
    {
        if (_currentBgmPlayingId == AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            return;
        }

        AkUnitySoundEngine.ExecuteActionOnPlayingID(
            AkActionOnEventType.AkActionOnEventType_Stop,
            _currentBgmPlayingId);
        _currentBgmPlayingId = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
    }

    public void PauseBGM()
    {
        if (_currentBgmPlayingId == AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            return;
        }

        AkUnitySoundEngine.ExecuteActionOnPlayingID(
            AkActionOnEventType.AkActionOnEventType_Pause,
            _currentBgmPlayingId);
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
        if (!AkUnitySoundEngine.IsInitialized())
        {
            return;
        }

        _masterVolumeRtpc?.SetGlobalValue(_masterVolume * 100.0f);
        _bgmVolumeRtpc?.SetGlobalValue(_bgmVolume * _bgmVolumeMultiplier * 100.0f);
        _seVolumeRtpc?.SetGlobalValue(_seVolume * 100.0f);
    }
}
