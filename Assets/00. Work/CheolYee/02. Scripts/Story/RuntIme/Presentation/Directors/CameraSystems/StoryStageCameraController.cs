using System.Reflection;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
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

        [Header("Fallback Metrics")]
        [SerializeField] private float fallbackAspect = 9f / 16f;
        [SerializeField] private float fallbackCameraWorldWidth = StoryStageVisualSizing.DefaultCameraWorldWidth;
        [SerializeField] private float fallbackOrthographicSize = 5.16f;

        [Header("Motion")]
        [SerializeField] private float defaultFocusDuration = 0.45f;

        private bool _initialized;
        private bool _runtimeCameraSearchAttempted;
        private bool _cinemachineSearchAttempted;
        private bool _stageReferenceInitialized;

        private Vector3 _stageReferenceCenter;
        private float _stageReferenceWorldWidth;
        private float _stageReferenceOrthoSize;

        private bool _focusMoveActive;
        private Vector3 _focusStartPosition;
        private Vector3 _focusTargetPosition;
        private float _focusMoveElapsed;
        private float _focusMoveDuration;

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
            Initialize();
        }

        private void Update()
        {
            TickFocusMove(Time.deltaTime);
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
            float aspect = cam != null ? cam.aspect : fallbackAspect;

            EnsureStageReferenceMetrics();

            float width = _stageReferenceWorldWidth > 0f
                ? _stageReferenceWorldWidth
                : fallbackCameraWorldWidth;

            return StoryStageCameraMetrics.FromCenteredFrame(_stageReferenceCenter, width, aspect);
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

                _focusMoveActive = false;
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

            _focusMoveActive = false;
        }

        public void MoveToSmooth(float desiredX, float desiredY, float duration = -1f)
        {
            Transform target = EnsureFocusTargetTransform();
            _cinemachineConnected = TryBindCinemachineFollow(target);

            Vector3 start = _cinemachineConnected
                ? target.position
                : ResolveFallbackCameraPosition();

            Vector3 end = start;
            end.x = desiredX;
            end.y = desiredY;

            _focusStartPosition = start;
            _focusTargetPosition = end;
            _focusMoveElapsed = 0f;
            _focusMoveDuration = duration > 0f ? duration : defaultFocusDuration;
            _focusMoveActive = true;
        }

        public void FocusXImmediate(float worldX)
        {
            float y = GetCurrentCameraCenter().y;
            MoveToImmediate(worldX, y);
        }

        public void FocusXSmooth(float worldX, float duration = -1f)
        {
            float y = GetCurrentCameraCenter().y;
            MoveToSmooth(worldX, y, duration);
        }

        public Vector2 ResolveParallaxOffset(StoryBackgroundStateData state)
        {
            if (state?.background == null)
                return Vector2.zero;

            float parallaxFactor = state.background.ParallaxFactor;
            if (parallaxFactor <= 0f)
                return Vector2.zero;

            EnsureStageReferenceMetrics();

            StoryStageCameraMetrics stage = ResolveStageReferenceMetrics();
            Vector3 camCenter = GetCurrentCameraCenter();
            camCenter.z = 0f;

            Vector3 delta = camCenter - stage.Center;

            return new Vector2(
                -delta.x / Mathf.Max(0.0001f, stage.Width) * parallaxFactor,
                0f);
        }

        private void TickFocusMove(float deltaTime)
        {
            if (!_focusMoveActive)
                return;

            _focusMoveElapsed += deltaTime;

            float t = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(_focusMoveElapsed / Mathf.Max(0.01f, _focusMoveDuration)));

            Vector3 next = Vector3.Lerp(_focusStartPosition, _focusTargetPosition, t);

            if (_cinemachineConnected)
            {
                Transform target = EnsureFocusTargetTransform();
                target.position = next;
            }
            else
            {
                Camera cam = ResolveRuntimeCamera();
                if (cam != null)
                {
                    Vector3 pos = cam.transform.position;
                    pos.x = next.x;
                    pos.y = next.y;
                    cam.transform.position = pos;
                }
            }

            if (t >= 1f)
                _focusMoveActive = false;
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

        private Vector3 ResolveFallbackCameraPosition()
        {
            Camera cam = ResolveRuntimeCamera();
            if (cam != null)
                return cam.transform.position;

            EnsureStageReferenceMetrics();
            return _stageReferenceCenter;
        }

        private void EnsureStageReferenceMetrics()
        {
            if (_stageReferenceInitialized)
                return;

            Camera cam = ResolveRuntimeCamera();
            if (cam != null && cam.orthographic)
            {
                StoryStageCameraMetrics metrics = StoryStageCameraMetrics.FromOrthographicCamera(cam);
                _stageReferenceCenter = metrics.Center;
                _stageReferenceCenter.z = 0f;
                _stageReferenceWorldWidth = metrics.Width;
                _stageReferenceOrthoSize = cam.orthographicSize;
            }
            else
            {
                _stageReferenceCenter = transform.position;
                _stageReferenceCenter.z = 0f;
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
