using System.Reflection;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Aspect;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Camera;
using Unity.Cinemachine;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors.CameraSystems
{
    /// <summary>
    /// Cached runtime camera controller for story stage playback.
    ///
    /// The StoryCore can live separately from the scene CinemachineCamera. If the references
    /// are not assigned, this controller searches the scene only once and then reuses the
    /// cached result. Runtime calls never perform repeated FindFirstObjectByType calls.
    /// </summary>
    public sealed class StoryStageCameraController : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Camera runtimeCamera;
        [SerializeField] private bool searchSceneCameraOnceIfMissing = true;

        [Header("Cinemachine")]
        [Tooltip("Optional. Drag CinemachineCamera here. If empty, this component searches once and caches the result.")]
        [SerializeField] private MonoBehaviour cinemachineCameraSource;

        [Tooltip("Optional. If empty, runtime focus target will be created automatically.")]
        [SerializeField] private Transform focusTarget;

        [Header("Aspect Override")]
        [SerializeField] private StoryAspectRatioController aspectRatioController;
        [Tooltip("Lightweight alternative to StoryAspectRatioController. Assign StoryAspectSettingsSO to enable StoryVisibleFrame-based actor positioning without the full controller MonoBehaviour.")]
        [SerializeField] private StoryAspectSettingsSO aspectSettingsFallback;

        [Header("Camera Init")]
        [Tooltip("Optional. Assign StoryCameraInitSettingsSO to use a shared baseOrthographicSize for Preview and Runtime.")]
        [SerializeField] private StoryCameraInitSettingsSO cameraInitSettings;

        [Header("Fallback Metrics")]
        [SerializeField] private float fallbackAspect = 9f / 16f;
        [SerializeField] private float fallbackCameraWorldWidth = StoryStageVisualSizing.DefaultCameraWorldWidth;
        [SerializeField] private float fallbackOrthographicSize = 5.16f;

        private bool _initialized;
        private bool _runtimeCameraSearchAttempted;
        private bool _cinemachineSearchAttempted;
        private bool _stageReferenceInitialized;

        private Vector3 _stageReferenceCenter;
        private float _stageReferenceWorldWidth;
        private float _stageReferenceOrthoSize;

        private MonoBehaviour _resolvedCinemachineCamera;
        private CinemachineCamera _typedCinemachineCamera;
        private PropertyInfo _followProperty;
        private CinemachineFollow _followBody;
        private bool _followBindingCached;
        private bool _canBindCinemachineFollow;
        private bool _cinemachineConnected;

        public Camera RuntimeCamera => ResolveRuntimeCamera();

        private void Awake()
        {
            // Domain reload 비활성화 시 이전 Play Mode의 _initialized / _stageReferenceInitialized 값이
            // 남아 있어 Initialize()가 no-op이 된다. Awake마다 강제 초기화한다.
            _stageReferenceInitialized = false;
            _initialized = false;

            Initialize();

            // focusTarget이 이전 Play Mode에서 actor-follow로 이동한 위치를 보유하고 있을 수 있다.
            // 항상 controller.transform.position 기준으로 reset한다.
            ResetFocusTargetToReferencePosition();
        }

        public void Initialize()
        {
            if (_initialized)
                return;

            ResolveRuntimeCamera();
            ResolveCinemachineCamera();

            EnsureStageReferenceMetrics();

            _initialized = true;
        }

        public void SetRuntimeCamera(Camera camera)
        {
            runtimeCamera = camera;
            _runtimeCameraSearchAttempted = camera != null;
            ResetStageReferenceMetrics();
        }

        public void SetCinemachineCameraSource(MonoBehaviour source)
        {
            cinemachineCameraSource = source;
            _resolvedCinemachineCamera = null;
            _typedCinemachineCamera = null;
            _cinemachineSearchAttempted = source != null;
            _followBindingCached = false;
            _canBindCinemachineFollow = false;
            _followProperty = null;
            _followBody = null;
        }

        public StoryStageCameraMetrics ResolveRuntimeCameraMetrics()
        {
            Camera cam = ResolveRuntimeCamera();

            if (aspectRatioController != null)
            {
                float storyAspect = aspectRatioController.StoryVisibleAspect;
                if (cam != null && cam.orthographic)
                {
                    float height = cam.orthographicSize * 2f;
                    Vector3 center = cam.transform.position;
                    center.z = 0f;
                    return StoryStageCameraMetrics.FromCenteredFrame(center, height * storyAspect, storyAspect);
                }

                EnsureStageReferenceMetrics();
                float refHeight = _stageReferenceOrthoSize > 0f
                    ? _stageReferenceOrthoSize * 2f
                    : fallbackCameraWorldWidth / Mathf.Max(0.0001f, fallbackAspect);
                return StoryStageCameraMetrics.FromCenteredFrame(_stageReferenceCenter, refHeight * storyAspect, storyAspect);
            }

            if (cam != null && cam.orthographic)
                return StoryStageCameraMetrics.FromOrthographicCamera(cam);

            float aspect = cam != null ? cam.aspect : fallbackAspect;
            EnsureStageReferenceMetrics();
            float width = _stageReferenceWorldWidth > 0f
                ? _stageReferenceWorldWidth
                : fallbackCameraWorldWidth;
            return StoryStageCameraMetrics.FromCenteredFrame(_stageReferenceCenter, width, aspect);
        }

        public StoryStageCameraMetrics ResolveStageReferenceMetrics()
        {
            Camera cam = ResolveRuntimeCamera();
            EnsureStageReferenceMetrics();

            float storyAspect = ResolveStoryVisibleAspect(cam);
            if (storyAspect > 0f)
            {
                float refHeight = _stageReferenceOrthoSize > 0f
                    ? _stageReferenceOrthoSize * 2f
                    : fallbackCameraWorldWidth / Mathf.Max(0.0001f, fallbackAspect);
                return StoryStageCameraMetrics.FromCenteredFrame(_stageReferenceCenter, refHeight * storyAspect, storyAspect);
            }

            // Physical camera fallback (no aspect override assigned)
            float aspect = cam != null ? cam.aspect : fallbackAspect;
            float refWidth = _stageReferenceWorldWidth > 0f
                ? _stageReferenceWorldWidth
                : fallbackCameraWorldWidth;
            return StoryStageCameraMetrics.FromCenteredFrame(_stageReferenceCenter, refWidth, aspect);
        }

        private float ResolveStoryVisibleAspect(Camera cam)
        {
            if (aspectRatioController != null)
                return aspectRatioController.StoryVisibleAspect;

            if (aspectSettingsFallback != null)
            {
                float physAspect = cam != null ? cam.aspect : fallbackAspect;
                return physAspect * aspectSettingsFallback.VisibleWidthRatio;
            }

            return -1f; // no override available
        }

        public Vector3 GetCurrentCameraCenter()
        {
            Camera cam = ResolveRuntimeCamera();
            if (cam != null)
            {
                Vector3 pos = cam.transform.position;
                pos.z = 0f;
                return pos;
            }

            EnsureStageReferenceMetrics();
            return _stageReferenceCenter;
        }

        public Vector3 StageReferenceCenter
        {
            get
            {
                EnsureStageReferenceMetrics();
                return _stageReferenceCenter;
            }
        }

        /// <summary>
        /// Applies camera stageLocalPosition and zoom from a StoryCameraStateData.
        /// stageLocalPosition (0,0) = StageRoot center (explicit move, not a no-op).
        /// zoom formula: finalOrtho = baseOrtho / zoom.
        /// </summary>
        public void ApplyStageCamera(StoryCameraStateData state)
        {
            if (state == null)
                return;

            float safeZoom = Mathf.Max(0.01f, state.zoom);
            if (!Mathf.Approximately(safeZoom, 1f))
                ApplyZoom(safeZoom);

            EnsureStageReferenceMetrics();
            float worldX = _stageReferenceCenter.x + state.stageLocalPosition.x;
            float worldY = _stageReferenceCenter.y + state.stageLocalPosition.y;
            MoveToImmediate(worldX, worldY);
        }

        public void ApplyZoom(float zoomMultiplier)
        {
            float safeZoom = Mathf.Max(0.01f, zoomMultiplier);

            EnsureStageReferenceMetrics();

            float baseOrtho = _stageReferenceOrthoSize > 0f
                ? _stageReferenceOrthoSize
                : fallbackOrthographicSize;

            float nextOrtho = baseOrtho / safeZoom;

            CinemachineCamera cm = ResolveTypedCinemachineCamera();
            if (cm != null)
            {
                LensSettings lens = cm.Lens;
                lens.OrthographicSize = nextOrtho;
                cm.Lens = lens;
            }

            Camera cam = ResolveRuntimeCamera();
            if (cam != null && cam.orthographic)
                cam.orthographicSize = nextOrtho;
        }

        public void MoveToImmediate(float desiredX, float desiredY)
        {
            Transform target = EnsureFocusTargetTransform();
            _cinemachineConnected = TryBindCinemachineFollow(target);

            if (_cinemachineConnected)
            {
                Vector3 pos = target.position;
                pos.x = desiredX;
                pos.y = desiredY;
                target.position = pos;

                CinemachineCamera cm = ResolveTypedCinemachineCamera();
                if (cm != null)
                    cm.PreviousStateIsValid = false;

                return;
            }

            Camera cam = ResolveRuntimeCamera();
            if (cam != null)
            {
                Vector3 pos = cam.transform.position;
                pos.x = desiredX;
                pos.y = desiredY;
                cam.transform.position = pos;
            }
        }

        /// <summary>
        /// Computes parallaxBase in world space:
        ///   parallaxBase = Lerp(stageRootCenter, cameraCenter, parallaxFactor)
        /// Background final position = parallaxBase + backgroundState.stageLocalPosition.
        /// </summary>
        public Vector3 ResolveParallaxBase(StoryBackgroundStateData state)
        {
            EnsureStageReferenceMetrics();
            float parallaxFactor = state?.EffectiveParallaxFactor ?? 0f;

            if (parallaxFactor <= 0f)
                return _stageReferenceCenter;

            Vector3 camCenter = GetCurrentCameraCenter();
            camCenter.z = 0f;
            return Vector3.Lerp(_stageReferenceCenter, camCenter, parallaxFactor);
        }

        private Camera ResolveRuntimeCamera()
        {
            if (runtimeCamera != null)
                return runtimeCamera;

            if (_runtimeCameraSearchAttempted)
                return null;

            _runtimeCameraSearchAttempted = true;

            runtimeCamera = Camera.main;
            if (runtimeCamera != null)
                return runtimeCamera;

            if (!searchSceneCameraOnceIfMissing)
                return null;

            Camera[] cameras = FindObjectsByType<Camera>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            if (cameras.Length > 0)
                runtimeCamera = cameras[0];

            return runtimeCamera;
        }

        private void EnsureStageReferenceMetrics()
        {
            if (_stageReferenceInitialized)
                return;

            Camera cam = ResolveRuntimeCamera();

            // Stage anchor = controller's own world position, NOT cam.transform.position.
            // cam is driven by CinemachineBrain and may carry leftover position from a
            // previous Play Mode session when domain reload is disabled.
            _stageReferenceCenter = transform.position;
            _stageReferenceCenter.z = 0f;

            if (cameraInitSettings != null)
            {
                _stageReferenceOrthoSize = cameraInitSettings.BaseOrthographicSize;
                float camAspect = cam != null ? cam.aspect : fallbackAspect;
                _stageReferenceWorldWidth = _stageReferenceOrthoSize * 2f * camAspect;
            }
            else if (cam != null && cam.orthographic)
            {
                _stageReferenceWorldWidth = cam.orthographicSize * 2f * cam.aspect;
                _stageReferenceOrthoSize = cam.orthographicSize;
            }
            else
            {
                _stageReferenceWorldWidth = fallbackCameraWorldWidth;
                _stageReferenceOrthoSize = fallbackOrthographicSize;
            }

            _stageReferenceInitialized = true;
        }

        private void ResetStageReferenceMetrics()
        {
            _stageReferenceInitialized = false;
            EnsureStageReferenceMetrics();
        }

        private void ResetFocusTargetToReferencePosition()
        {
            if (focusTarget == null)
                return;

            EnsureStageReferenceMetrics();
            focusTarget.position = _stageReferenceCenter;

            CinemachineCamera cm = ResolveTypedCinemachineCamera();
            if (cm != null)
                cm.PreviousStateIsValid = false;
        }

        private Transform EnsureFocusTargetTransform()
        {
            if (focusTarget != null)
                return focusTarget;

            GameObject go = new GameObject("[StoryCameraFocusTarget]");
            go.transform.SetParent(transform, false);

            EnsureStageReferenceMetrics();
            go.transform.position = _stageReferenceCenter;

            focusTarget = go.transform;
            return focusTarget;
        }

        private MonoBehaviour ResolveCinemachineCamera()
        {
            if (_resolvedCinemachineCamera != null)
                return _resolvedCinemachineCamera;

            if (_cinemachineSearchAttempted && cinemachineCameraSource == null)
                return null;

            _cinemachineSearchAttempted = true;

            if (cinemachineCameraSource == null)
            {
                CinemachineCamera local = GetComponentInChildren<CinemachineCamera>(true);
                if (local == null)
                    local = GetComponentInParent<CinemachineCamera>();
                if (local == null && searchSceneCameraOnceIfMissing)
                    local = FindFirstObjectByType<CinemachineCamera>();

                cinemachineCameraSource = local;
            }

            _resolvedCinemachineCamera = cinemachineCameraSource;
            _typedCinemachineCamera = cinemachineCameraSource as CinemachineCamera;
            return _resolvedCinemachineCamera;
        }

        private CinemachineCamera ResolveTypedCinemachineCamera()
        {
            if (_typedCinemachineCamera != null)
                return _typedCinemachineCamera;

            _typedCinemachineCamera = ResolveCinemachineCamera() as CinemachineCamera;
            return _typedCinemachineCamera;
        }

        private bool TryBindCinemachineFollow(Transform followTarget)
        {
            if (followTarget == null)
                return false;

            if (!_followBindingCached)
                CacheCinemachineFollowBinding();

            if (!_canBindCinemachineFollow || _resolvedCinemachineCamera == null || _followProperty == null)
                return false;

            _followProperty.SetValue(_resolvedCinemachineCamera, followTarget);

            if (_followBody != null)
            {
                Transform vcamTransform = _resolvedCinemachineCamera.transform;
                Vector3 followOffset = _followBody.FollowOffset;
                followOffset.x = 0f;
                followOffset.y = vcamTransform.position.y - followTarget.position.y;
                followOffset.z = vcamTransform.position.z - followTarget.position.z;
                _followBody.FollowOffset = followOffset;
            }

            return true;
        }

        private void CacheCinemachineFollowBinding()
        {
            _followBindingCached = true;
            _canBindCinemachineFollow = false;
            _followProperty = null;
            _followBody = null;

            MonoBehaviour vcam = ResolveCinemachineCamera();
            if (vcam == null)
                return;

            _followProperty = vcam.GetType().GetProperty("Follow");
            if (_followProperty == null || !typeof(Transform).IsAssignableFrom(_followProperty.PropertyType))
                return;

            _followBody = vcam.GetComponent<CinemachineFollow>();
            if (_followBody == null)
                _followBody = vcam.gameObject.AddComponent<CinemachineFollow>();

            _canBindCinemachineFollow = true;
        }
    }
}
