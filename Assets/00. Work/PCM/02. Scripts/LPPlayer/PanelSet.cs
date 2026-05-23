using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using UnityEngine;

public class PanelSet : MonoBehaviour
{
    [SerializeField] private EventChannelSO _Soundchannel;
    void Start()
    {
        _Soundchannel.RaiseEvent(new PlaySoundEvent((BgmSounds)0, Vector3.zero, SoundChannelId.Bgm));
        gameObject.SetActive(false);     
    }
}
