using System;
using System.Collections;
using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events;
using _00._Work.Goat._02._Scripts.Events;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI.StoryRewardUI
{
    public class StoryRewardCanvas : MonoBehaviour
    {
        [Header("EventChannel")] 
        [SerializeField] private EventChannelSO storyUnlockEventChannel;
        [SerializeField] private EventChannelSO levelUpRewardExitClickChannel;
        [SerializeField] private EventChannelSO storyCommandChannel;
        
        [Header("UI")]
        [SerializeField] private RectTransform rewardPanel;
        [SerializeField] private TextMeshProUGUI storyTitleText;
        
        [Header("Motion")]
        [SerializeField] private Vector2 showOffset = new Vector2(0f, -500f);
        [SerializeField] private float moveDuration = 0.45f;
        [SerializeField] private float showTime = 1.2f;
        
        private Vector2 _hidePosition;
        private Vector2 _showPosition;
        private readonly Queue<StoryEpisodeSO> storyEpisodeQueue = new();
        
        private bool _isPlaying;

        private void Awake()
        {
            _hidePosition = rewardPanel.anchoredPosition;
            _showPosition = _hidePosition + showOffset;
            
            storyUnlockEventChannel.AddListener<StoryEpisodeUnlockRequested>(HandleStoryUnlock);
            levelUpRewardExitClickChannel.AddListener<LevelUpRewardeExitBtnClickEvent>(HandleLevelUpExitClick);
        }

        private void OnDestroy()
        {
            storyUnlockEventChannel.RemoveListener<StoryEpisodeUnlockRequested>(HandleStoryUnlock);
            levelUpRewardExitClickChannel.RemoveListener<LevelUpRewardeExitBtnClickEvent>(HandleLevelUpExitClick);
        }

        private void HandleStoryUnlock(StoryEpisodeUnlockRequested obj)
        {
            storyEpisodeQueue.Enqueue(obj.Episode);
        }
        
        private void HandleLevelUpExitClick(LevelUpRewardeExitBtnClickEvent obj)
        {
            if (obj.playGoScene)
            {
                if (storyEpisodeQueue.Count <= 0)
                    return;
                
                StoryEpisodeSO episode = storyEpisodeQueue.Dequeue();
                storyCommandChannel.RaiseEvent(new StoryEpisodeLaunchRequested(episode));    
            }
            
            if (_isPlaying)
                return;
            
            StartCoroutine(PlayStoryRewardQueue());
        }

        private IEnumerator PlayStoryRewardQueue()
        {
            _isPlaying = true;

            while (storyEpisodeQueue.Count > 0)
            {
                StoryEpisodeSO episode = storyEpisodeQueue.Dequeue();

                ShowEpisodeText(episode);

                yield return MovePanel(_hidePosition, _showPosition);

                yield return new WaitForSeconds(showTime);

                yield return MovePanel(_showPosition, _hidePosition);
            }
            
            _isPlaying = false;
        }
        
        private void ShowEpisodeText(StoryEpisodeSO episode)
        {
            
            if (storyTitleText != null)
                storyTitleText.text = episode.Title;
        }
        
        private IEnumerator MovePanel(Vector2 from, Vector2 to)
        {
            rewardPanel.anchoredPosition = from;

            LMotion.Create(from, to, moveDuration)
                .WithEase(Ease.OutCubic)
                .Bind(value => rewardPanel.anchoredPosition = value)
                .AddTo(gameObject);

            yield return new WaitForSeconds(moveDuration);
        }
        
    }
}