using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    public GameObject[] muteChecks; // 0: Master, 1: BGM, 2: SFX

    private bool _isMasterMuted;
    private bool _isBGMMuted;
    private bool _isSFXMuted;

    private float _lastMaster = 1f;
    private float _lastBGM = 1f;
    private float _lastSFX = 1f;

    void Start()
    {
        InitSlider(masterSlider, "MasterVolume", SetMasterVolume);
        InitSlider(bgmSlider, "BGMVolume", SetBGMVolume);
        InitSlider(sfxSlider, "SFXVolume", SetSFXVolume);
    }

    private void InitSlider(Slider slider, string mixerParam, UnityEngine.Events.UnityAction<float> action)
    {
        slider.minValue = 0.0001f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.onValueChanged.AddListener(action);
        action.Invoke(slider.value);
    }

    public void SetMasterVolume(float value)
    {
        if (!_isMasterMuted)
            _lastMaster = value;

        float dB = Mathf.Log10(value) * 20f;
        audioMixer.SetFloat("MasterVolume", dB);

        bool isMuted = Mathf.Approximately(value, 0.0001f);
        muteChecks[0].SetActive(isMuted); // 뮤트됐을 때 체크 ON
        _isMasterMuted = isMuted;
    }

    public void SetBGMVolume(float value)
    {
        if (!_isBGMMuted)
            _lastBGM = value;

        float dB = Mathf.Log10(value) * 20f;
        audioMixer.SetFloat("BGMVolume", dB);

        bool isMuted = Mathf.Approximately(value, 0.0001f);
        muteChecks[1].SetActive(isMuted);
        _isBGMMuted = isMuted;
    }

    public void SetSFXVolume(float value)
    {
        if (!_isSFXMuted)
            _lastSFX = value;

        float dB = Mathf.Log10(value) * 20f;
        audioMixer.SetFloat("SFXVolume", dB);

        bool isMuted = Mathf.Approximately(value, 0.0001f);
        muteChecks[2].SetActive(isMuted);
        _isSFXMuted = isMuted;
    }

    public void MuteMaster()
    {
        if (!_isMasterMuted)
        {
            _lastMaster = Mathf.Approximately(masterSlider.value, 0.0001f) ? 1f : masterSlider.value;
            _isMasterMuted = true; // 슬라이더 변경 전에 먼저 플래그 세우기
            masterSlider.value = masterSlider.minValue;
        }
        else
        {
            _isMasterMuted = false; // 해제 플래그 먼저
            masterSlider.value = _lastMaster;
        }
    }

    public void MuteBGM()
    {
        if (!_isBGMMuted)
        {
            _lastBGM = Mathf.Approximately(bgmSlider.value, 0.0001f) ? 1f : bgmSlider.value;
            _isBGMMuted = true;
            bgmSlider.value = bgmSlider.minValue;
        }
        else
        {
            _isBGMMuted = false;
            bgmSlider.value = _lastBGM;
        }
    }

    public void MuteSFX()
    {
        if (!_isSFXMuted)
        {
            _lastSFX = Mathf.Approximately(sfxSlider.value, 0.0001f) ? 1f : sfxSlider.value;
            _isSFXMuted = true;
            sfxSlider.value = sfxSlider.minValue;
        }
        else
        {
            _isSFXMuted = false;
            sfxSlider.value = _lastSFX;
        }
    }
}