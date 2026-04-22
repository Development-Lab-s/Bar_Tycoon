using Algorithm;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BBJ.GridSystem.Pathfind
{
    public class Pathfinding : MonoBehaviour
    {
        // public → private: 인스펙터 할당 누락으로 인한 NPE 방지
        [SerializeField] private PathRequestManager _requestManager;
        private GridManager _gridManager;

        private void Awake()
        {
            _gridManager    = GetComponent<GridManager>();
            _requestManager = GetComponent<PathRequestManager>();
        }

        public void StartFindPath(Vector3 pathStart, Vector3 pathEnd)
            => StartCoroutine(FindPath(pathStart, pathEnd));

        private IEnumerator FindPath(Vector3 startPos, Vector3 targetPos)
        {
            Vector3[] waypoints  = Array.Empty<Vector3>();
            bool      pathSuccess = false;

            Node startNode  = _gridManager.NodeFromWorldPoint(startPos);
            Node targetNode = _gridManager.NodeFromWorldPoint(targetPos);

            // NodeFromWorldPoint가 null을 반환할 수 있으므로 null 체크 추가
            if (startNode == null || targetNode == null)
            {
                yield return null;
                _requestManager.FinishedProcessingPath(waypoints, false);
                yield break;
            }

            if (startNode.walkable)
            {
                // targetNode가 walkable이 아닌 경우 그 위치로 이동 불가.
                // Workplace InteractOffset이 벽 인접 셀을 가리킬 수 있으므로
                // 목표 노드 walkable 체크는 제거하고 경로를 최대한 탐색한다.
                // (startNode만 walkable이면 탐색 시작)
                var closedSet = new HashSet<Node>();
                var openSet   = new Heap<Node>(_gridManager.MaxSize);
                openSet.Add(startNode);

                while (openSet.Count > 0)
                {
                    Node node = openSet.RemoveFirst();
                    closedSet.Add(node);

                    if (node == targetNode)
                    {
                        pathSuccess = true;
                        break;
                    }

                    foreach (Node neighbour in GetNeighbours(node))
                    {
                        if (!neighbour.walkable || closedSet.Contains(neighbour))
                            continue;

                        int newCost = node.gCost + GetDistance(node, neighbour);
                        if (newCost < neighbour.gCost || !openSet.Contains(neighbour))
                        {
                            neighbour.gCost  = newCost;
                            neighbour.hCost  = GetDistance(neighbour, targetNode);
                            neighbour.parent = node;

                            if (!openSet.Contains(neighbour)) openSet.Add(neighbour);
                            else openSet.UpdateItem(neighbour);
                        }
                    }
                }
            }

            yield return null;

            if (pathSuccess)
                waypoints = RetracePath(startNode, targetNode);

            _requestManager.FinishedProcessingPath(waypoints, pathSuccess);
        }

        private List<Node> GetNeighbours(Node node)
        {
            var neighbours = new List<Node>();
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                if (_gridManager.TryGetCellToNode(node.gridX + x, node.gridY + y, out Node n))
                    neighbours.Add(n);
            }
            return neighbours;
        }

        private Vector3[] RetracePath(Node startNode, Node endNode)
        {
            var  path        = new List<Node>();
            Node currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent;
            }

            Vector3[] waypoints = SimplifyPath(path);
            Array.Reverse(waypoints);
            return waypoints;
        }

        private Vector3[] SimplifyPath(List<Node> path)
        {
            var     waypoints    = new List<Vector3>();
            Vector2 directionOld = Vector2.zero;

            for (int i = 1; i < path.Count; i++)
            {
                var directionNew = new Vector2(
                    path[i - 1].gridX - path[i].gridX,
                    path[i - 1].gridY - path[i].gridY);

                if (directionNew != directionOld)
                    waypoints.Add(path[i].worldPosition);

                directionOld = directionNew;
            }
            return waypoints.ToArray();
        }

        private int GetDistance(Node a, Node b)
        {
            int dstX = Mathf.Abs(a.gridX - b.gridX);
            int dstY = Mathf.Abs(a.gridY - b.gridY);
            return dstX > dstY
                ? 14 * dstY + 10 * (dstX - dstY)
                : 14 * dstX + 10 * (dstY - dstX);
        }
    }
}
