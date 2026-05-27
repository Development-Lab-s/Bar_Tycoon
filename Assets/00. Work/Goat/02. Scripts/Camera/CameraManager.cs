using System;
using System.Collections;
using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Core.CameraSystems;
using _00._Work.Goat._02._Scripts.Events;
using Gamelib.EventSystem;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Camera
{
    public class CameraManager : MonoBehaviour
    {
        [Header("Event")]
        [SerializeField] private EventChannelSO cameraEventSO;
        [SerializeField] private EventChannelSO levelUpExitBtnClickEvent;

        [Header("References")]
        [SerializeField] private CinemachineCamera playCamera;
        [SerializeField] private CinemachineCamera focusCamera;

        [Header("Settings")]
        [SerializeField] private float movePositionDuration;
        [SerializeField] private float moveZoomDuration;
        [SerializeField] private int focusCameraActivePriority = 100;
        [SerializeField] private int focusCameraUnActivePriority = -1;
        [SerializeField] private float waitDuration = 1;
        [SerializeField] private float zoomPercent = 0.7f;
        [SerializeField] private float space = 0.6f;

        private Queue<List<Vector2>> _objectPositionQueue = new();

        private bool _isPlaying = false;

        private CameraPanController _cameraPanController;
        private FreeZoomController _freeZoomController;
        
        private Coroutine _coroutine;

        private void Awake()
        {
            cameraEventSO.AddListener<CameraManagerEvent>(HandleCameraEvent);
            levelUpExitBtnClickEvent.AddListener<LevelUpRewardeExitBtnClickEvent>(HandleExitBtnClickEvent);

            _cameraPanController = playCamera.GetComponent<CameraPanController>();
            _freeZoomController = playCamera.GetComponent<FreeZoomController>();
        }
        
        private void OnDisable()
        {
            _isPlaying = false;
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
            
            if (focusCamera != null)
                focusCamera.Priority = focusCameraUnActivePriority;
        }

        private void OnDestroy()
        {
            cameraEventSO.RemoveListener<CameraManagerEvent>(HandleCameraEvent);
            levelUpExitBtnClickEvent.RemoveListener<LevelUpRewardeExitBtnClickEvent>(HandleExitBtnClickEvent);
        }

        private void HandleExitBtnClickEvent(LevelUpRewardeExitBtnClickEvent obj)
        {
            if (_objectPositionQueue.Count > 0)
            {
                _coroutine = StartCoroutine(CameraMotionCoroutine());
            }
        }

        private void HandleCameraEvent(CameraManagerEvent evt)
        {
            _objectPositionQueue.Enqueue(evt.objectPositionList);

            if (evt.isImmediateStart)
            {
                if (!_isPlaying)
                {
                    _coroutine = StartCoroutine(CameraMotionCoroutine());
                }
            }
        }

        private IEnumerator CameraMotionCoroutine()
        {
            _isPlaying = true;

            List<Vector2> targetList = _objectPositionQueue.Dequeue();

            Vector3 originPosition = playCamera.transform.position;
            float originZoom = _freeZoomController.CurrentOrthoSize;

            Vector2 targetPosition = CalculateCameraPosition(targetList);
            float targetLens = CalculateContainLens(targetList);

            focusCamera.transform.position = new Vector3(targetPosition.x, targetPosition.y, focusCamera.transform.position.z);
            focusCamera.Lens.OrthographicSize = originZoom;

            focusCamera.Priority = focusCameraActivePriority;

            yield return StartCoroutine(MoveZoomCoroutine(targetLens));

            yield return new WaitForSeconds(waitDuration);

            float startTime = Time.time;
            float startLens = focusCamera.Lens.OrthographicSize;

            while (Time.time < startTime + movePositionDuration)
            {
                float t = (Time.time - startTime) / movePositionDuration;
                t = Mathf.SmoothStep(0f, 1f, t);

                focusCamera.transform.position = Vector3.Lerp(
                    new Vector3(targetPosition.x, targetPosition.y, focusCamera.transform.position.z),
                    new Vector3(originPosition.x, originPosition.y, focusCamera.transform.position.z), t);

                focusCamera.Lens.OrthographicSize = Mathf.Lerp(startLens, originZoom, t);

                yield return null;
            }

            Vector3 finalPosition = focusCamera.transform.position;
            _cameraPanController.MoveTo(finalPosition);

            _freeZoomController.SetZoom(originZoom, true);

            focusCamera.Priority = focusCameraUnActivePriority;
            _isPlaying = false;
            _coroutine = null;
        }

        private IEnumerator MoveZoomCoroutine(float targetLens)
        {
            float startTime = Time.time;
            float startLens = focusCamera.Lens.OrthographicSize;

            while (Time.time < startTime + moveZoomDuration)
            {
                float t = (Time.time - startTime) / moveZoomDuration;
                t = Mathf.SmoothStep(0f, 1f, t);

                focusCamera.Lens.OrthographicSize = Mathf.Lerp(startLens, targetLens, t);

                yield return null;
            }

            focusCamera.Lens.OrthographicSize = targetLens;
        }

        private Vector2 CalculateCameraPosition(List<Vector2> objectPositions)
        {
            Bounds bounds = CalculateBounds(objectPositions);
            return bounds.center;
        }

        private Bounds CalculateBounds(List<Vector2> objectPositions)
        {
            Bounds bounds = new Bounds(objectPositions[0], Vector3.zero);

            for (int i = 1; i < objectPositions.Count; i++)
            {
                bounds.Encapsulate(objectPositions[i]);
            }

            return bounds;
        }

        private float CalculateContainLens(List<Vector2> vecList)
        {
            Bounds bounds = CalculateBounds(vecList);

            float boundsSizeX = bounds.size.x + space;
            float boundsSizeY = bounds.size.y + space;

            float aspect = UnityEngine.Camera.main.aspect;

            float needSizeByX = boundsSizeX / (2f * aspect);
            float needSizeByY = boundsSizeY / 2f;

            float targetLens = Mathf.Max(needSizeByX, needSizeByY);
            return targetLens / zoomPercent;
        }
    }
}