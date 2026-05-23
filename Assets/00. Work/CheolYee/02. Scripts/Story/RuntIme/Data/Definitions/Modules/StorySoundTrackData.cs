using System;
using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    public enum StoryBgmKeyOperation
    {
        Play,
        Stop,
    }

    public enum StoryBgmTransitionMode
    {
        HardReplace,
        Crossfade,
    }

    [Serializable]
    public sealed class StorySoundSettingsData
    {
        [Tooltip("SoundManager가 구독하는 EventChannelSO. 비어 있으면 이 라인의 사운드 키는 실행되지 않는다.")]
        public EventChannelSO soundChannel;

        [Tooltip("BGM key의 재생/정지 시 사용되는 페이드 시간.")]
        [Min(0f)]
        public float bgmFadeDuration = 0.25f;

        [Tooltip("SFX key의 인스턴스 envelope 및 line 종료 정리 시 사용되는 페이드 시간.")]
        [Min(0f)]
        public float sfxFadeDuration = 0.08f;

        public StorySoundSettingsData ShallowClone() => (StorySoundSettingsData)MemberwiseClone();

        public void CopyFrom(StorySoundSettingsData source)
        {
            if (source == null)
            {
                soundChannel = null;
                bgmFadeDuration = 0.25f;
                sfxFadeDuration = 0.08f;
                return;
            }

            soundChannel = source.soundChannel;
            bgmFadeDuration = source.bgmFadeDuration;
            sfxFadeDuration = source.sfxFadeDuration;
        }
    }

    [Serializable]
    public sealed class StoryBgmKeyframeData
    {
        [Range(0f, 1f)]
        public float normalizedTime = 0f;

        [Min(0f)]
        public float timeSeconds = 0f;

        public StoryBgmKeyOperation operation = StoryBgmKeyOperation.Play;

        public BgmSounds bgmSound;

        [Tooltip("HardReplace = 이전 BGM을 정리한 뒤 다음 BGM 시작, Crossfade = 이전 BGM fade-out과 새 BGM fade-in을 동시에 시작.")]
        public StoryBgmTransitionMode transitionMode = StoryBgmTransitionMode.HardReplace;

        public StoryBgmKeyframeData ShallowClone() => (StoryBgmKeyframeData)MemberwiseClone();
    }

    [Serializable]
    public sealed class StorySfxKeyframeData
    {
        [Range(0f, 1f)]
        public float normalizedTime = 0f;

        [Min(0f)]
        public float timeSeconds = 0f;

        public SfxSounds sfxSound;

        public StorySfxKeyframeData ShallowClone() => (StorySfxKeyframeData)MemberwiseClone();
    }

    [Serializable]
    public sealed class StorySoundTrackData
    {
        [Tooltip("Line 내부 BGM keyframes. 한 시점에는 한 key만 유지한다.")]
        public List<StoryBgmKeyframeData> bgmKeyframes = new();

        [Tooltip("Line 내부 SFX keyframes. 한 시점에는 한 key만 유지한다.")]
        public List<StorySfxKeyframeData> sfxKeyframes = new();

        public bool HasAnyKeyframes =>
            (bgmKeyframes?.Count ?? 0) > 0
            || (sfxKeyframes?.Count ?? 0) > 0;
    }

    public static class StorySoundSettingsUtility
    {
        private static readonly StorySoundSettingsData DefaultSettings = new();

        public static StorySoundSettingsData Clone(StorySoundSettingsData source)
        {
            var clone = new StorySoundSettingsData();
            clone.CopyFrom(source);
            return clone;
        }

        public static bool AreEquivalent(StorySoundSettingsData left, StorySoundSettingsData right)
        {
            StorySoundSettingsData a = left ?? DefaultSettings;
            StorySoundSettingsData b = right ?? DefaultSettings;
            return a.soundChannel == b.soundChannel
                && Mathf.Approximately(a.bgmFadeDuration, b.bgmFadeDuration)
                && Mathf.Approximately(a.sfxFadeDuration, b.sfxFadeDuration);
        }

        public static bool IsStructDefault(StorySoundSettingsData settings) =>
            AreEquivalent(settings, DefaultSettings);

        public static bool HasAnyLegacySoundContent(StoryStageLayoutModuleSO layout)
        {
            if (layout == null)
                return false;

            if (layout.SoundTrack?.HasAnyKeyframes == true)
                return true;

            StorySoundSettingsData settings = layout.SoundSettings;
            return settings != null && !IsStructDefault(settings);
        }

        public static StorySoundSettingsData ResolveEpisodeDefaults(StoryEpisodeSO episode)
        {
            if (episode == null)
                return DefaultSettings;

            if (episode.HasExplicitDefaultSoundSettings)
                return episode.DefaultSoundSettings ?? DefaultSettings;

            StorySoundSettingsData legacySeed = FindFirstLegacySoundSettings(episode);
            return legacySeed ?? episode.DefaultSoundSettings ?? DefaultSettings;
        }

        public static bool ResolveLegacyLineOverride(StoryEpisodeSO episode, StoryStageLayoutModuleSO layout)
        {
            if (layout == null)
                return false;

            if (layout.SoundTrack?.HasAnyKeyframes == true)
                return true;

            return !AreEquivalent(layout.SoundSettings, ResolveEpisodeDefaults(episode));
        }

        public static bool ShouldUseLineOverride(StoryEpisodeSO episode, StoryStageLayoutModuleSO layout)
        {
            if (layout == null)
                return false;

            if (layout.HasExplicitSoundSettingsOverrideState)
                return layout.UseSoundSettingsOverride;

            return ResolveLegacyLineOverride(episode, layout);
        }

        public static StorySoundSettingsData ResolveEffectiveLineSettings(StoryEpisodeSO episode, StoryStageLayoutModuleSO layout)
        {
            if (ShouldUseLineOverride(episode, layout))
                return layout?.SoundSettings ?? DefaultSettings;

            return ResolveEpisodeDefaults(episode);
        }

        private static StorySoundSettingsData FindFirstLegacySoundSettings(StoryEpisodeSO episode)
        {
            if (episode?.Lines == null)
                return null;

            foreach (StoryLineSO line in episode.Lines)
            {
                StoryStageLayoutModuleSO layout = FindStageLayout(line);
                if (HasAnyLegacySoundContent(layout))
                    return layout.SoundSettings;
            }

            return null;
        }

        private static StoryStageLayoutModuleSO FindStageLayout(StoryLineSO line)
        {
            if (line?.Modules == null)
                return null;

            foreach (StoryModuleSO module in line.Modules)
            {
                if (module is StoryStageLayoutModuleSO layout)
                    return layout;
            }

            return null;
        }
    }
}
