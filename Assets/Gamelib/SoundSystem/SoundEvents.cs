using Gamelib.EventSystem;
using UnityEngine;

namespace Gamelib.SoundSystem
{
    public enum SoundChannelId
    {
        None = 0,
        Bgm = 1,
    }

    public readonly struct SoundRef
    {
        public AudioTypes AudioType { get; }
        public int Index { get; }

        public SoundRef(AudioTypes audioType, int index)
        {
            AudioType = audioType;
            Index = index;
        }

        //사용자 정의 자동 형변환으로 이넘들을 처리하기
        public static implicit operator SoundRef(SfxSounds sfx)
            => new SoundRef(AudioTypes.SFX, (int)sfx);

        public static implicit operator SoundRef(BgmSounds bgm)
            => new SoundRef(AudioTypes.MUSIC, (int)bgm);
    }

    public sealed class PlaySoundEvent : GameEvent
    {
        public SoundRef Sound { get; }
        public Vector3 Position { get; }
        public SoundChannelId ChannelId { get; }

        public PlaySoundEvent(SoundRef sound, Vector3 position, SoundChannelId channelId = SoundChannelId.None)
        {
            Sound = sound;
            Position = position;
            ChannelId = channelId;
        }

        //생성자 요약문법 this를 이용하여 중복코드 제거하기
        public PlaySoundEvent(SfxSounds sfxSound, Vector3 position, SoundChannelId channelId = SoundChannelId.None)
            : this((SoundRef)sfxSound, position, channelId)
        {
        }

        public PlaySoundEvent(BgmSounds bgmSound, Vector3 position, SoundChannelId channelId = SoundChannelId.Bgm)
            : this((SoundRef)bgmSound, position, channelId)
        {
        }
    }

    public sealed class StopSoundEvent : GameEvent
    {
        public SoundChannelId ChannelId { get; }

        public StopSoundEvent(SoundChannelId channelId)
        {
            ChannelId = channelId;
        }
    }
}