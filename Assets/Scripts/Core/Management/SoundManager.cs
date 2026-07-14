using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;

public class SoundManager : MonoBehaviour
{
    static SoundManager instance { get; set; }

    AudioSource _audioSrc;

    [SerializeField]
    private List<AudioClip> _allSEData;

    [SerializeField]
    private List<AudioClip> _bgmList;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(this);
        _audioSrc = GetComponent<AudioSource>();
    }

    

    public void PlaySE(string name)
    {
        var se = _bgmList.Find(x => x.name == name);
        _audioSrc.PlayOneShot(se);
    }

    public void PlayBGM(string name)
    {
        _audioSrc.Stop();
        var bgm = _bgmList.Find(x => x.name == name);
        _audioSrc.clip = bgm;
        _audioSrc.Play();
    }
}
