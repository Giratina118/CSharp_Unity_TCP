using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioMixer audioMixer;
    public Slider MasterSoundScrollbar;
    public Slider BGMScrollbar;
    public Slider SFXScrollbar;

    public AudioSource BGM;
    public AudioSource ButtonClickSound;

    // 오디오 믹서, 저장할때 사용하는 키값
    private string _masterVolumeKey = "Master";
    private string _bgmVolumeKey = "BGM";
    private string _sfxVolumeKey = "SFX";

    // 현재 볼륨(저장해야 갱신됨)
    private float _masterVolume;
    private float _bgmVolume;
    private float _sfxVolume;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _masterVolume = PlayerPrefs.GetFloat(_masterVolumeKey, 1.0f);
        _bgmVolume = PlayerPrefs.GetFloat(_bgmVolumeKey, 1.0f);
        _sfxVolume = PlayerPrefs.GetFloat(_sfxVolumeKey, 1.0f);

        SetVolume();
    }

    private void Update()
    {
        // 마우스 왼쪽 버튼 클릭 시
        if (Input.GetMouseButtonDown(0))
        {
            // 현재 마우스 아래에 있는 UI 요소 확인
            GameObject currentObj = EventSystem.current.currentSelectedGameObject;

            // 클릭된 오브젝트가 버튼 컴포넌트를 가지고 있다면 소리 재생
            if (currentObj != null && currentObj.GetComponent<Button>() != null)
                ButtonClickSound.PlayOneShot(ButtonClickSound.clip);
        }
    }

    public void SetMasterVolume(float volume)
    {
        // 오디오 믹서의 값은 -80 ~ 0까지이기 때문에 0.0001 ~ 1의 Log10 * 20을 한다.
        audioMixer.SetFloat(_masterVolumeKey, Mathf.Log10(volume) * 20);
    }

    public void SetBGMVolume(float volume)
    {
        audioMixer.SetFloat(_bgmVolumeKey, Mathf.Log10(volume) * 20);
    }

    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat(_sfxVolumeKey, Mathf.Log10(volume) * 20);
    }

    public void Save()
    {
        _masterVolume = MasterSoundScrollbar.value;
        _bgmVolume = BGMScrollbar.value;
        _sfxVolume = SFXScrollbar.value;

        PlayerPrefs.SetFloat(_masterVolumeKey, _masterVolume);
        PlayerPrefs.SetFloat(_bgmVolumeKey, _bgmVolume);
        PlayerPrefs.SetFloat(_sfxVolumeKey, _sfxVolume);
        PlayerPrefs.Save();
    }

    public void SetVolume()
    {
        MasterSoundScrollbar.value = _masterVolume;
        BGMScrollbar.value = _bgmVolume;
        SFXScrollbar.value = _sfxVolume;
    }
}
