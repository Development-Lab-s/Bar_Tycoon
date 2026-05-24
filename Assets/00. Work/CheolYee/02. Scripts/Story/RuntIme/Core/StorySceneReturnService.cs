using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events;
using Gamelib.EventSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core
{
    public sealed class StorySceneReturnService : MonoBehaviour
    {
        [Header("Channels")]
        [SerializeField] private EventChannelSO storyCommandChannel;

        [Header("Scene")]
        [SerializeField] private string mainSceneName;

        private void OnEnable()
        {
            if (storyCommandChannel == null)
                return;

            storyCommandChannel.AddListener<StoryClosed>(HandleStoryClosed);
        }

        private void OnDisable()
        {
            if (storyCommandChannel == null)
                return;

            storyCommandChannel.RemoveListener<StoryClosed>(HandleStoryClosed);
        }

        private void HandleStoryClosed(StoryClosed evt)
        {
            ReturnToMainScene();
        }

        private void ReturnToMainScene()
        {
            if (string.IsNullOrWhiteSpace(mainSceneName))
            {
                Debug.LogWarning("[StorySceneReturnService] mainSceneName이 비어 있습니다. scene load를 중단합니다.");
                return;
            }

            SceneManager.LoadScene(mainSceneName);
        }
    }
}
