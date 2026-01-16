using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioMixer audioMixer;        // 오디오 믹서
    public Slider MasterVolumeScrollbar; // 마스터 볼륨 스크롤바
    public Slider BGMVolumeScrollbar;    // BGM 볼룸 스크롤바
    public Slider SFXVolumeScrollbar;    // SFX 볼륨 스크롤바
    public AudioSource BGM;              // BGM
    public AudioSource ButtonClickSound; // 버튼 클릭음

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

    // 마스터 볼륨 설정
    public void SetMasterVolume(float volume)
    {
        // 오디오 믹서의 값은 -80 ~ 0까지이기 때문에 0.0001 ~ 1의 Log10 * 20을 한다.
        audioMixer.SetFloat(_masterVolumeKey, Mathf.Log10(volume) * 20);
    }

    // BGM 볼륨 설정
    public void SetBGMVolume(float volume)
    {
        audioMixer.SetFloat(_bgmVolumeKey, Mathf.Log10(volume) * 20);
    }

    // SFX 볼륨 설정
    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat(_sfxVolumeKey, Mathf.Log10(volume) * 20);
    }

    // 볼륨 설정 저장
    public void Save()
    {
        _masterVolume = MasterVolumeScrollbar.value;
        _bgmVolume = BGMVolumeScrollbar.value;
        _sfxVolume = SFXVolumeScrollbar.value;

        PlayerPrefs.SetFloat(_masterVolumeKey, _masterVolume);
        PlayerPrefs.SetFloat(_bgmVolumeKey, _bgmVolume);
        PlayerPrefs.SetFloat(_sfxVolumeKey, _sfxVolume);
        PlayerPrefs.Save();
    }

    // 볼륨 저장 정보 불러오기
    public void SetVolume()
    {
        MasterVolumeScrollbar.value = _masterVolume;
        BGMVolumeScrollbar.value = _bgmVolume;
        SFXVolumeScrollbar.value = _sfxVolume;
    }
}
