using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace BBJ.UI.Settings
{
    public class SettingsUI : MonoBehaviour
    {
        [SerializeField] private AudioMixer   _audioMixer;
        [SerializeField] private Slider       _masterSlider;
        [SerializeField] private Slider       _bgmSlider;
        [SerializeField] private Slider       _sfxSlider;
        [SerializeField] private GameObject[] _muteChecks; // 0: Master, 1: BGM, 2: SFX

        private bool  _isMasterMuted;
        private bool  _isBGMMuted;
        private bool  _isSFXMuted;
        private float _lastMaster = 1f;
        private float _lastBGM    = 1f;
        private float _lastSFX    = 1f;

        private void Start()
        {
            InitSlider(_masterSlider, "MasterVolume", SetMasterVolume);
            InitSlider(_bgmSlider,    "BGMVolume",    SetBGMVolume);
            InitSlider(_sfxSlider,    "SFXVolume",    SetSFXVolume);
        }

        private void InitSlider(Slider slider, string mixerParam, UnityEngine.Events.UnityAction<float> action)
        {
            slider.minValue = 0.0001f;
            slider.maxValue = 1f;
            slider.value    = 1f;
            slider.onValueChanged.AddListener(action);
            action.Invoke(slider.value);
        }

        public void SetMasterVolume(float value)
        {
            if (!_isMasterMuted) _lastMaster = value;
            SetMixerVolume("MasterVolume", value);
            _isMasterMuted = ApplyMuteCheck(_muteChecks[0], value);
        }

        public void SetBGMVolume(float value)
        {
            if (!_isBGMMuted) _lastBGM = value;
            SetMixerVolume("BGMVolume", value);
            _isBGMMuted = ApplyMuteCheck(_muteChecks[1], value);
        }

        public void SetSFXVolume(float value)
        {
            if (!_isSFXMuted) _lastSFX = value;
            SetMixerVolume("SFXVolume", value);
            _isSFXMuted = ApplyMuteCheck(_muteChecks[2], value);
        }

        public void MuteMaster() => ToggleMute(ref _isMasterMuted, ref _lastMaster, _masterSlider);
        public void MuteBGM()    => ToggleMute(ref _isBGMMuted,    ref _lastBGM,    _bgmSlider);
        public void MuteSFX()    => ToggleMute(ref _isSFXMuted,    ref _lastSFX,    _sfxSlider);

        private void ToggleMute(ref bool isMuted, ref float last, Slider slider)
        {
            if (!isMuted)
            {
                last         = Mathf.Approximately(slider.value, 0.0001f) ? 1f : slider.value;
                isMuted      = true;
                slider.value = slider.minValue;
            }
            else
            {
                isMuted      = false;
                slider.value = last;
            }
        }

        private void SetMixerVolume(string param, float value)
        {
            _audioMixer.SetFloat(param, Mathf.Log10(value) * 20f);
        }

        private static bool ApplyMuteCheck(GameObject check, float value)
        {
            bool muted = Mathf.Approximately(value, 0.0001f);
            check.SetActive(muted);
            return muted;
        }
    }
}
