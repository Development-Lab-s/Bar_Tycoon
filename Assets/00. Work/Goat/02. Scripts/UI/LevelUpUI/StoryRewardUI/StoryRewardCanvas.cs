using System;
using System.Collections;
using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events;
using Gamelib.EventSystem;
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
        
        [Header("UI")]
        [SerializeField] private RectTransform rewardPanel;
        [SerializeField] private TextMeshProUGUI storyTitleText;
        
        [Header("Motion")]
        [SerializeField] private Vector2 hidePosition = new Vector2(0f, 500f);
        [SerializeField] private Vector2 showPosition = new Vector2(0f, 0f);
        [SerializeField] private float moveDuration = 0.45f;
        [SerializeField] private float showTime = 1.2f;
        
        private readonly Queue<StoryEpisodeSO> storyEpisodeQueue = new();
        
        private bool _isPlaying;

        private void Awake()
        {
            rewardPanel.anchoredPosition = hidePosition;
            
            storyUnlockEventChannel.AddListener<StoryEpisodeUnlockRequested>(HandleStoryUnlock);
            levelUpRewardExitClickChannel.AddListener<StoryEpisodeUnlockRequested>(HandleLevelUpExitClick);
        }

        private void OnDestroy()
        {
            storyUnlockEventChannel.RemoveListener<StoryEpisodeUnlockRequested>(HandleStoryUnlock);
            levelUpRewardExitClickChannel.RemoveListener<StoryEpisodeUnlockRequested>(HandleLevelUpExitClick);
        }

        private void HandleStoryUnlock(StoryEpisodeUnlockRequested obj)
        {
            storyEpisodeQueue.Enqueue(obj.Episode);
        }
        
        private void HandleLevelUpExitClick(StoryEpisodeUnlockRequested obj)
        {
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

                yield return MovePanel(hidePosition, showPosition);

                yield return new WaitForSeconds(showTime);

                yield return MovePanel(showPosition, hidePosition);
            }

            rewardPanel.gameObject.SetActive(false);
            _isPlaying = false;
        }
        
        private void ShowEpisodeText(StoryEpisodeSO episode)
        {
            rewardPanel.gameObject.SetActive(true);
            
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