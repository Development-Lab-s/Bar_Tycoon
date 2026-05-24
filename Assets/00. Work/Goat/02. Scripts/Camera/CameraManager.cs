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
        [SerializeField] private int focusCameraUnActivePriority = 1;
        [SerializeField] private float waitDuration = 1;
        [SerializeField] private float zoomPercent = 0.7f;
        [SerializeField] private float space = 0.6f;
        
        private Queue<List<Vector2>> _objectPositionQueue = new();

        private bool _isPlaying = false;
        
        private CameraPanController _cameraPanController;
        private FreeZoomController _freeZoomController;

        private void Awake()
        {
            cameraEventSO.AddListener<CameraManagerEvent>(HandleCameraEvent);
            levelUpExitBtnClickEvent.AddListener<LevelUpRewardeExitBtnClickEvent>(HandleExitBtnClickEvent);
            _cameraPanController = playCamera.GetComponent<CameraPanController>();
            _freeZoomController = playCamera.GetComponent<FreeZoomController>();
        }

        private void OnDestroy()
        {
            cameraEventSO.RemoveListener<CameraManagerEvent>(HandleCameraEvent);
            levelUpExitBtnClickEvent.RemoveListener<LevelUpRewardeExitBtnClickEvent>(HandleExitBtnClickEvent);
        }
        
        private void HandleCameraEvent(CameraManagerEvent obj)
        {
            if (obj.objectPositionList.Count < 1) return;
            
            _objectPositionQueue.Enqueue(obj.objectPositionList);

            if (obj.isImmediateStart)
            {
                if (!_isPlaying)
                {
                    _isPlaying = true;
                    StartCoroutine(CameraMotionCoroutine());
                }   
            }
        }
        
        private void HandleExitBtnClickEvent(LevelUpRewardeExitBtnClickEvent obj)
        {
            if (!_isPlaying)
            {
                _isPlaying = true;
                StartCoroutine(CameraMotionCoroutine());
            }   
        }

        private IEnumerator CameraMotionCoroutine()
        {
            _cameraPanController.SetInputEnabled(false);
            _freeZoomController.SetInputEnabled(false);
            
            focusCamera.transform.position = playCamera.transform.position;
            focusCamera.Lens.OrthographicSize = playCamera.Lens.OrthographicSize;
            
            float originZoom = playCamera.Lens.OrthographicSize;
            
            focusCamera.Priority = focusCameraActivePriority;
            
            while(_objectPositionQueue.Count > 0)
            {
                List<Vector2> nowObjectPositions = _objectPositionQueue.Dequeue();
                
                float containLens = CalculateContainLens(nowObjectPositions);
                
                yield return MovePositionCoroutine(nowObjectPositions);
                
                float zoomInTarget = focusCamera.Lens.OrthographicSize * zoomPercent;
                yield return ZoomToCoroutine(zoomInTarget, containLens);
                
                yield return new WaitForSeconds(waitDuration);
                
                yield return ZoomToCoroutine(originZoom);
            }
            
            Vector3 finalPosition = focusCamera.transform.position;
            _cameraPanController.MoveTo(finalPosition);
            
            _freeZoomController.SetZoom(originZoom, true);
            
            _cameraPanController.SetInputEnabled(true);
            _freeZoomController.SetInputEnabled(true);
            
            focusCamera.Priority = focusCameraUnActivePriority;
            _isPlaying = false;
        }

        private IEnumerator MovePositionCoroutine(List<Vector2> objectPositions)
        {
            Vector2 targetPos = CalculateCameraPosition(objectPositions);
            float cameraPosition = Vector2.Distance(targetPos, focusCamera.transform.position);
            float speed = cameraPosition / movePositionDuration;
            
            while (Vector2.Distance(targetPos, focusCamera.transform.position) > 0.01f)
            {
                Vector2 nextPos = Vector2.MoveTowards( focusCamera.transform.position, targetPos, speed * Time.deltaTime);
                focusCamera.transform.position = new Vector3(nextPos.x, nextPos.y, focusCamera.transform.position.z);
                
                yield return null;
            }
        }
        
        private IEnumerator ZoomToCoroutine(float targetLens, float minContainLens = 0f)
        {
            targetLens = Mathf.Max(targetLens, minContainLens);
            
            float startLens = focusCamera.Lens.OrthographicSize;
            float startTime = Time.time;

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

            return Mathf.Max(needSizeByX, needSizeByY);
        }
    }
}