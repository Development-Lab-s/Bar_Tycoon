using System.Collections.Generic;
using Gamelib.EventSystem;
using Gamelib.ObjectPool.Runtime;
using UnityEngine;

namespace Gamelib.SoundSystem
{
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private SoundListSo sfxSoundList;
        [SerializeField] private SoundListSo bgmSoundList;

        [SerializeField] private PoolManagerSo poolManager;
        [SerializeField] private PoolItemSo soundItem;

        [field: SerializeField] public EventChannelSO SoundChannel { get; private set; }

        private readonly Dictionary<SoundChannelId, SoundPlayer> _channelPlayers = new();

        private void Awake()
        {
            SoundChannel.AddListener<PlaySoundEvent>(HandlePlaySoundEvent);
            SoundChannel.AddListener<StopSoundEvent>(HandleStopSoundEvent);
        }

        private void OnDestroy()
        {
            SoundChannel.RemoveListener<PlaySoundEvent>(HandlePlaySoundEvent);
            SoundChannel.RemoveListener<StopSoundEvent>(HandleStopSoundEvent);
        }

        private void HandlePlaySoundEvent(PlaySoundEvent evt)
        {
            SoundClipSo clipData = ResolveClip(evt.Sound);
            if (clipData == null)
            {
                Debug.LogWarning($"사운드를 찾을 수 없습니다. Type={evt.Sound.AudioType}, Index={evt.Sound.Index}");
                return;
            }
            if (clipData.loop && evt.ChannelId != SoundChannelId.None)
            {
                StopChannel(evt.ChannelId);
            }
            else if (clipData.loop && evt.ChannelId == SoundChannelId.None)
            {
                Debug.LogWarning($"루프 사운드는 채널 지정이 필요합니다. : {clipData.name}");
            }

            SoundPlayer player = poolManager.Pop<SoundPlayer>(soundItem);
            player.transform.position = evt.Position;
            player.OnSoundFinished += HandleSoundFinish;
            player.PlaySound(clipData);

            if (clipData.loop && evt.ChannelId != SoundChannelId.None)
            {
                _channelPlayers[evt.ChannelId] = player;
            }
        }

        private SoundClipSo ResolveClip(SoundRef soundRef)
        {
            SoundListSo targetList = soundRef.AudioType switch
            {
                AudioTypes.SFX => sfxSoundList,
                AudioTypes.MUSIC => bgmSoundList,
                _ => null
            };

            if (targetList == null || targetList.sounds == null)
                return null;

            if (soundRef.Index < 0 || soundRef.Index >= targetList.sounds.Length)
                return null;

            return targetList.sounds[soundRef.Index];
        }

        private void HandleStopSoundEvent(StopSoundEvent evt)
        {
            StopChannel(evt.ChannelId);
        }

        private void StopChannel(SoundChannelId channelId)
        {
            //if (!_channelPlayers.TryGetValue(channelId, out SoundPlayer player))
            //    return;

            //player.OnSoundFinished -= HandleSoundFinish;
            //player.ForceStopSound();
            //poolManager.Push(player);
            //_channelPlayers.Remove(channelId);
            if (_channelPlayers.Remove(channelId, out SoundPlayer player)) 
            {
                player.OnSoundFinished -= HandleSoundFinish;
                player.ForceStopSound();
                poolManager.Push(player);
            }
        }

        private void HandleSoundFinish(SoundPlayer player)
        {
            player.OnSoundFinished -= HandleSoundFinish;

            SoundChannelId foundChannel = SoundChannelId.None;
            bool exists = false;

            foreach (var pair in _channelPlayers)
            {
                if (pair.Value == player)
                {
                    foundChannel = pair.Key;
                    exists = true;
                    break;
                }
            }

            if (exists)
            {
                _channelPlayers.Remove(foundChannel);
            }

            poolManager.Push(player);
        }
    }
}