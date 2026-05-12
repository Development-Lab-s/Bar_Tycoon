using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Views;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors.CameraSystems;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors.Util;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors
{
    public sealed class StoryBackgroundStageRuntime : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private Transform backgroundRoot;

        [Header("Camera")]
        [SerializeField] private StoryStageCameraController cameraController;

        [Header("Shared Visual Prefab")]
        [SerializeField] private StoryBackgroundVisualView sharedBackgroundPrefab;

        [Header("Legacy Fallback")]
        [SerializeField] private bool useLegacyBackgroundPrefabFallback;

        [Header("Fallback Metrics")]
        [SerializeField] private float fallbackAspect = 9f / 16f;
        [SerializeField] private float fallbackCameraWorldWidth = StoryStageVisualSizing.DefaultCameraWorldWidth;

        private GameObject _instance;
        private StoryBackgroundVisualView _view;
        private string _activeKey = "";
        private StoryBackgroundStateData _currentState;

        public bool HasBackground => _instance != null;
        public StoryBackgroundStateData CurrentState => _currentState;

        private void LateUpdate()
        {
            RefreshCurrentView();
        }

        public StoryBackgroundStateData BuildCurrentStateClone()
        {
            return _currentState?.ShallowClone();
        }

        public void ApplyState(StoryBackgroundStateData state)
        {
            if (state == null || !state.HasBackground || !state.visible)
            {
                Hide(state);
                return;
            }

            string key = state.ResolvedBackgroundKey;

            if (_instance == null || _activeKey != key)
                RecreateInstance(state, key);

            if (_instance == null)
                return;

            if (!_instance.activeSelf)
                _instance.SetActive(true);

            ApplyToView(state);
            _currentState = state.ShallowClone();
        }

        public void RefreshCurrentView()
        {
            if (_view == null)
                return;

            if (_currentState == null)
                return;

            if (!_currentState.HasBackground || !_currentState.visible)
                return;

            ApplyToView(_currentState);
        }

        public void ClearAll()
        {
            if (_instance != null)
                Destroy(_instance);

            _instance = null;
            _view = null;
            _activeKey = "";
            _currentState = null;
        }

        private void Hide(StoryBackgroundStateData state)
        {
            if (_instance != null && _instance.activeSelf)
                _instance.SetActive(false);

            _currentState = state?.ShallowClone();
        }

        private void ApplyToView(StoryBackgroundStateData state)
        {
            if (_view == null || state == null)
                return;

            StoryStageCameraMetrics metrics = ResolveRuntimeCameraMetrics();
            Vector2 parallax = ResolveParallaxOffset(state);

            _view.Apply(state, metrics, parallax);
        }

        private void RecreateInstance(StoryBackgroundStateData state, string key)
        {
            if (_instance != null)
                Destroy(_instance);

            Transform parent = ResolveBackgroundRoot();

            if (sharedBackgroundPrefab != null)
            {
                _view = Instantiate(sharedBackgroundPrefab, parent);
                _instance = _view.gameObject;
            }
            else if (useLegacyBackgroundPrefabFallback
                     && state.background != null
                     && state.background.RuntimePrefab != null)
            {
                _instance = Instantiate(state.background.RuntimePrefab, parent);
                _view = _instance.GetComponent<StoryBackgroundVisualView>();

                if (_view == null)
                    _view = _instance.AddComponent<StoryBackgroundVisualView>();
            }
            else
            {
                _instance = new GameObject("Story Background");
                _instance.transform.SetParent(parent, false);
                _view = _instance.AddComponent<StoryBackgroundVisualView>();
            }

            if (_instance != null)
                _instance.name = string.IsNullOrWhiteSpace(key)
                    ? "Story Background"
                    : $"Story Background - {key}";

            _activeKey = key;
        }

        private StoryStageCameraMetrics ResolveRuntimeCameraMetrics()
        {
            StoryStageCameraController controller = ResolveCameraController();

            if (controller != null)
                return controller.ResolveRuntimeCameraMetrics();

            return StoryStageCameraMetrics.FromCenteredFrame(
                Vector3.zero,
                fallbackCameraWorldWidth,
                fallbackAspect);
        }

        private Vector2 ResolveParallaxOffset(StoryBackgroundStateData state)
        {
            StoryStageCameraController controller = ResolveCameraController();

            if (controller != null)
                return controller.ResolveParallaxOffset(state);

            return Vector2.zero;
        }

        private StoryStageCameraController ResolveCameraController()
        {
            return StoryRuntimeComponentResolver.GetInSelfOrParent(this, ref cameraController);
        }

        private Transform ResolveBackgroundRoot()
        {
            return backgroundRoot != null ? backgroundRoot : transform;
        }
    }
}