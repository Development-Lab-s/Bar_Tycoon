using BBJ.EventSystem;
using BBJ.Movement;
using Gamelib.EventSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BBJ.GridSystem.Pathfind
{

    public class PathRequestManager : MonoBehaviour, IPathRequestManager
    {
        [SerializeField] private EventChannelSO pathRequestChannel;
        [SerializeField] private RuntimeReference<IPathRequestManager> pathRequestSO;

        private readonly Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
        private PathRequest currentPathRequest;

        private Pathfinding pathfinding;
        bool isProcessingPath = false;

        public delegate void PathRequestAction(Vector3[] path, bool pathSuccesful);

        private void Awake()
        {
            pathfinding = GetComponent<Pathfinding>();
        }

        private void OnEnable()
        {
            pathRequestChannel.AddListener<PathRequestEvent>(RequestPathHandler);
            pathRequestSO?.Initialize(this);
        }

        private void OnDisable()
        {
            pathRequestChannel.RemoveListener<PathRequestEvent>(RequestPathHandler);
        }
        private void RequestPathHandler(PathRequestEvent pathRequestEvent)
        {
            var request = pathRequestEvent.PathRequest;
            RequestPath(request.pathStart, request.pathEnd, request.callback);
        }
        public void RequestPath(Vector3 pathStart, Vector3 pathEnd, PathRequestAction callback)
        {
            PathRequest newPathRequest = new PathRequest(pathStart, pathEnd, callback);
            pathRequestQueue.Enqueue(newPathRequest);
            TryProcessNext();
        }

        private void TryProcessNext()
        {
            if (isProcessingPath == false && pathRequestQueue.Count > 0)
            {
                var request = pathRequestQueue.Dequeue();
                currentPathRequest = request;
                isProcessingPath = true;
                pathfinding.StartFindPath(request.pathStart, request.pathEnd);
            }
        }
        public void FinishedProcessingPath(Vector3[] path, bool success)
        {
            currentPathRequest.callback?.Invoke(path, success);
            isProcessingPath = false;
            TryProcessNext();
        }
        [Serializable]
        public struct PathRequest
        {
            public Vector3 pathStart;
            public Vector3 pathEnd;
            public PathRequestAction callback;

            public PathRequest(Vector3 start, Vector3 end, PathRequestAction callback)
            {
                this.pathStart = start;
                this.pathEnd = end;
                this.callback = callback;
            }
        }

    }

}