using System.Collections;
using BBJ.EventSystem;
using Gamelib.EventSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BBJ.Scene
{
    public class GameSceneManager : MonoBehaviour
    {
        [SerializeField] private EventChannelSO _sceneChannel;
        [SerializeField] private FadeUI         _fadeUI;

        private SceneType  _current;
        private SceneType? _additiveLoaded;
        private bool       _sceneReady;
        private bool       _isTransitioning;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _sceneChannel.AddListener<SceneTransitionRequestEvent>(OnTransitionRequested);
            _sceneChannel.AddListener<SceneReadyEvent>(OnSceneReady);
        }

        private void Start()
        {
            _sceneChannel.RaiseEvent(new SceneTypeChangedEvent(SceneType.Main));
            //TransitionTo(SceneType.Title);
        }

        private void OnDestroy()
        {
            _sceneChannel.RemoveListener<SceneTransitionRequestEvent>(OnTransitionRequested);
            _sceneChannel.RemoveListener<SceneReadyEvent>(OnSceneReady);
        }

        private void OnTransitionRequested(SceneTransitionRequestEvent e) => TransitionTo(e.Target);

        private void OnSceneReady(SceneReadyEvent _) => _sceneReady = true;

        private void TransitionTo(SceneType target)
        {
            if (_isTransitioning) return;
            StartCoroutine(DoTransition(target));
        }

        private IEnumerator DoTransition(SceneType target)
        {
            _isTransitioning = true;

            yield return StartCoroutine(_fadeUI.FadeOut());

            bool returnToMain = target == SceneType.Main && _additiveLoaded.HasValue;

            if (_additiveLoaded.HasValue)
            {
                yield return SceneManager.UnloadSceneAsync(SceneName(_additiveLoaded.Value));
                _additiveLoaded = null;
            }

            if (target == SceneType.Story || target == SceneType.Cocktail)
            {
                yield return SceneManager.LoadSceneAsync(SceneName(target), LoadSceneMode.Additive);
                _additiveLoaded = target;
            }
            else if (!returnToMain)
            {
                // Replace: Title→Main 또는 Bootstrap→Title
                yield return SceneManager.LoadSceneAsync(SceneName(target), LoadSceneMode.Single);
            }

            _current = target;
            _sceneChannel.RaiseEvent(new SceneTypeChangedEvent(target));

            if (!returnToMain)
            {
                _sceneReady = false;
                yield return new WaitUntil(() => _sceneReady);
            }

            yield return StartCoroutine(_fadeUI.FadeIn());

            _isTransitioning = false;
        }

        private static string SceneName(SceneType t) => t switch
        {
            SceneType.Title    => "Title",
            SceneType.Main     => "Main",
            SceneType.Story    => "Story",
            SceneType.Cocktail => "Cocktail",
            _                  => throw new System.ArgumentOutOfRangeException(nameof(t))
        };
    }
}
