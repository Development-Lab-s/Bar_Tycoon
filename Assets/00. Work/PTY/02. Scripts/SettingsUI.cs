using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace _00._Work.PTY._02._Scripts
{
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

        private const string KEY_MASTER = "MasterVolume";
        private const string KEY_BGM    = "BGMVolume";
        private const string KEY_SFX    = "SFXVolume";
        private const string KEY_MASTER_MUTE = "MasterMuted";
        private const string KEY_BGM_MUTE    = "BGMMuted";
        private const string KEY_SFX_MUTE    = "SFXMuted";

        void Start()
        {
            LoadSettings();
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(KEY_MASTER, _lastMaster);
            PlayerPrefs.SetFloat(KEY_BGM,    _lastBGM);
            PlayerPrefs.SetFloat(KEY_SFX,    _lastSFX);
            PlayerPrefs.SetInt(KEY_MASTER_MUTE, _isMasterMuted ? 1 : 0);
            PlayerPrefs.SetInt(KEY_BGM_MUTE,    _isBGMMuted    ? 1 : 0);
            PlayerPrefs.SetInt(KEY_SFX_MUTE,    _isSFXMuted    ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            _lastMaster = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
            _lastBGM    = PlayerPrefs.GetFloat(KEY_BGM,    1f);
            _lastSFX    = PlayerPrefs.GetFloat(KEY_SFX,    1f);

            bool savedMasterMute = PlayerPrefs.GetInt(KEY_MASTER_MUTE, 0) == 1;
            bool savedBGMMute    = PlayerPrefs.GetInt(KEY_BGM_MUTE,    0) == 1;
            bool savedSFXMute    = PlayerPrefs.GetInt(KEY_SFX_MUTE,    0) == 1;

            // 슬라이더 초기화 (리스너 등록 포함)
            InitSlider(masterSlider, SetMasterVolume);
            InitSlider(bgmSlider,    SetBGMVolume);
            InitSlider(sfxSlider,    SetSFXVolume);

            // 뮤트 상태 복원: 뮤트였으면 MuteXXX() 호출, 아니면 저장값으로 슬라이더 세팅
            if (savedMasterMute) MuteMaster();
            else masterSlider.value = _lastMaster;

            if (savedBGMMute) MuteBGM();
            else bgmSlider.value = _lastBGM;

            if (savedSFXMute) MuteSFX();
            else sfxSlider.value = _lastSFX;
        }

        private void InitSlider(Slider slider, UnityEngine.Events.UnityAction<float> action)
        {
            slider.minValue = 0.0001f;
            slider.maxValue = 1f;
            slider.onValueChanged.AddListener(action);
        }

        public void SetMasterVolume(float value)
        {
            if (!_isMasterMuted)
                _lastMaster = value;

            audioMixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20f);

            bool isMuted = Mathf.Approximately(value, 0.0001f);
            muteChecks[0].SetActive(isMuted);
            _isMasterMuted = isMuted;

            SaveSettings();
        }

        public void SetBGMVolume(float value)
        {
            if (!_isBGMMuted)
                _lastBGM = value;

            audioMixer.SetFloat("BGMVolume", Mathf.Log10(value) * 20f);

            bool isMuted = Mathf.Approximately(value, 0.0001f);
            muteChecks[1].SetActive(isMuted);
            _isBGMMuted = isMuted;

            SaveSettings();
        }

        public void SetSFXVolume(float value)
        {
            if (!_isSFXMuted)
                _lastSFX = value;

            audioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20f);

            bool isMuted = Mathf.Approximately(value, 0.0001f);
            muteChecks[2].SetActive(isMuted);
            _isSFXMuted = isMuted;

            SaveSettings();
        }

        public void MuteMaster()
        {
            if (!_isMasterMuted)
            {
                _lastMaster = Mathf.Approximately(masterSlider.value, 0.0001f) ? 1f : masterSlider.value;
                _isMasterMuted = true;
                masterSlider.value = masterSlider.minValue;
            }
            else
            {
                _isMasterMuted = false;
                masterSlider.value = _lastMaster;
            }
            SaveSettings();
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
            SaveSettings();
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
            SaveSettings();
        }
    }
}