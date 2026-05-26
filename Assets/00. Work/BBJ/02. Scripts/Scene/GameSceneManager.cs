    using System;
    using System.Collections;
    using System.Collections.Generic;
    using BBJ.EventSystem;
    using Gamelib.EventSystem;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    namespace BBJ.Scene
    {
        public class GameSceneManager : MonoBehaviour
        {
            public static GameSceneManager Instance { get; private set; }

            [SerializeField] private EventChannelSO _sceneChannel;
            [SerializeField] private FadeUI         _fadeUI;

            private readonly Dictionary<SceneType, ISceneHost> _hosts        = new();
            private readonly HashSet<SceneType>                _loadedScenes = new();

            private SceneType _foreground;
            private bool      _sceneReady;
            private bool      _isTransitioning;

            private void Awake()
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject);
                    return;
                }
                Instance = this;
                DontDestroyOnLoad(gameObject);
                _sceneChannel.AddListener<SceneTransitionRequestEvent>(OnTransitionRequested);
                _sceneChannel.AddListener<SceneReadyEvent>(OnSceneReady);
            }

            private void Start() => StartCoroutine(Init());

            private void OnDestroy()
            {
                _sceneChannel.RemoveListener<SceneTransitionRequestEvent>(OnTransitionRequested);
                _sceneChannel.RemoveListener<SceneReadyEvent>(OnSceneReady);
            }

            public void RegisterHost(ISceneHost host) => _hosts[host.SceneType] = host;

            private void OnTransitionRequested(SceneTransitionRequestEvent e) => TransitionTo(e.Target);
            private void OnSceneReady(SceneReadyEvent _) => _sceneReady = true;

            private void TransitionTo(SceneType target)
            {
                if (_isTransitioning) return;
                StartCoroutine(DoTransition(target));
            }

            private IEnumerator Init()
            {
                _isTransitioning = true;

                yield return SceneManager.LoadSceneAsync("Title", LoadSceneMode.Additive);
                _loadedScenes.Add(SceneType.Title);
                yield return new WaitUntil(() => _hosts.ContainsKey(SceneType.Title));

                _foreground = SceneType.Title;
                _sceneChannel.RaiseEvent(new SceneTypeChangedEvent(SceneType.Title));
                _hosts[SceneType.Title].OnForeground();

                yield return StartCoroutine(_fadeUI.FadeIn());

                _isTransitioning = false;
            }

            private IEnumerator DoTransition(SceneType target)
            {
                _isTransitioning = true;

                // 새 씬 로드를 페이드 아웃과 동시에 시작
                AsyncOperation loadOp = null;
                if (!_loadedScenes.Contains(target))
                {
                    loadOp = SceneManager.LoadSceneAsync(SceneName(target), LoadSceneMode.Additive);
                    loadOp.allowSceneActivation = false;
                }

                yield return StartCoroutine(_fadeUI.FadeOut());

                if (_hosts.TryGetValue(_foreground, out var prev))
                    prev.OnBackground();

                // Cocktail / Story 씬은 복귀 시 언로드
                if (_foreground != SceneType.Main && _loadedScenes.Contains(_foreground))
                {
                    yield return SceneManager.UnloadSceneAsync(SceneName(_foreground));
                    _loadedScenes.Remove(_foreground);
                    _hosts.Remove(_foreground);
                }

                if (loadOp != null)
                {
                    while (loadOp.progress < 0.9f) yield return null;
                    loadOp.allowSceneActivation = true;
                    yield return loadOp;
                    _loadedScenes.Add(target);
                }

                // Host 등록 대기 (씬 Awake 완료 시점)
                yield return new WaitUntil(() => _hosts.ContainsKey(target));

                // Main 복귀 시 GameLoader 초기화 완료 대기 (_sceneReady는 GameLoader가 한 번만 발행하므로 영구적으로 true 유지)
                if (target == SceneType.Main && !_sceneReady)
                    yield return new WaitUntil(() => _sceneReady);

                _foreground = target;
                _sceneChannel.RaiseEvent(new SceneTypeChangedEvent(target));
                _hosts[target].OnForeground();

                yield return StartCoroutine(_fadeUI.FadeIn());

                _isTransitioning = false;
            }

            private static string SceneName(SceneType t) => t switch
            {
                SceneType.Title    => "Title",
                SceneType.Main     => "Main",
                SceneType.Story    => "Story",
                SceneType.Cocktail => "Cocktail",
                _ => throw new ArgumentOutOfRangeException(nameof(t))
            };
        }
    }
