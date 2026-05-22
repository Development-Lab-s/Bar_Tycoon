using System;
using System.Collections;
using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Core.CameraSystems;
using _00._Work.Goat._02._Scripts.Events;
using Gamelib.EventSystem;
using Unity.Cinemachine;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Camera
{
    public class CameraManager : MonoBehaviour
    {
        [Header("Event")]
        [SerializeField] private EventChannelSO cameraEventSO;
        
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
        
        private Queue<List<Vector2>> _objectPositionQueue = new();

        private bool _isPlaying = false;
        
        private CameraPanController _cameraPanController;
        private FreeZoomController _freeZoomController;

        private void Awake()
        {
            cameraEventSO.AddListener<CameraManagerEvent>(HandleCameraEvent);
            _cameraPanController = playCamera.GetComponent<CameraPanController>();
            _freeZoomController = playCamera.GetComponent<FreeZoomController>();
        }

        private void OnDestroy()
        {
            cameraEventSO.RemoveListener<CameraManagerEvent>(HandleCameraEvent);
        }
        
        private void HandleCameraEvent(CameraManagerEvent obj)
        {
            _objectPositionQueue.Enqueue(obj.objectPositionList);

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
            
            focusCamera.Priority = focusCameraActivePriority;
            
            while(_objectPositionQueue.Count > 0)
            {
                List<Vector2> nowObjectPositions = _objectPositionQueue.Dequeue();
                yield return MovePositionCoroutine(nowObjectPositions);
                yield return ZoomInCoroutine();
                yield return new WaitForSeconds(waitDuration);
            }
            
            Vector3 finalPosition = focusCamera.transform.position;
            _cameraPanController.MoveTo(finalPosition);
            
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
            
            focusCamera.transform.position = new Vector3(targetPos.x, targetPos.y, focusCamera.transform.position.z);
        }

        private IEnumerator ZoomInCoroutine()
        {
            float startLens = focusCamera.Lens.OrthographicSize;
            float targetLens = startLens * zoomPercent;

            float startTime = Time.time;

            while (Time.time < startTime + moveZoomDuration)
            {
                float t = (Time.time - startTime) / moveZoomDuration;
                t = Mathf.SmoothStep(0f, 1f, t);

                float nextLens = Mathf.Lerp(startLens, targetLens, t);

                var focusLens = focusCamera.Lens;
                focusLens.OrthographicSize = nextLens;
                focusCamera.Lens = focusLens;

                yield return null;
            }

            var finalLens = focusCamera.Lens;
            finalLens.OrthographicSize = targetLens;
            focusCamera.Lens = finalLens;
        }

        private Vector2 CalculateCameraPosition(List<Vector2> objectPositions)
        {
            Vector2 cameraPosition = new Vector2();
            foreach (Vector2 vec in objectPositions)
            {
                cameraPosition += vec;
            }
            cameraPosition /= objectPositions.Count;
            
            return cameraPosition;
        }
    }
}