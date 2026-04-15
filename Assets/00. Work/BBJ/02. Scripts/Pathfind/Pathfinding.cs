using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfind.Algorithm;

namespace Pathfind
{

    public class Pathfinding : MonoBehaviour
    {
        public PathRequestManager requestManager;
        Grid grid;

        public void Awake()
        {
            grid = GetComponent<Grid>();
        }
        public void StartFindPath(Vector3 pathStart, Vector3 pathEnd)
        {
            StartCoroutine(FindPath(pathStart, pathEnd));
        }

        private IEnumerator FindPath(Vector3 startPos, Vector3 targetPos)
        {
            Vector3[] waypoints = new Vector3[0];
            bool pathSuccess = false;

            Node startNode = grid.NodeFromWorldPoint(startPos);
            Node targetNode = grid.NodeFromWorldPoint(targetPos);

            if (startNode.walkable && targetNode.walkable)
            {
                HashSet<Node> closedSet = new HashSet<Node>();
                Heap<Node> openSet = new Heap<Node>(grid.MaxSize);
                openSet.Add(startNode);

                while (openSet.Count > 0)
                {
                    Node node = openSet.RemoveFirst();
                    closedSet.Add(node);

                    if (node == targetNode)
                    {
                        //RetracePath(startNode, targetNode);
                        pathSuccess = true;
                        break;
                    }

                    foreach (Node neighbour in grid.GetNeighbours(node))
                    {
                        if (!neighbour.walkable || closedSet.Contains(neighbour))
                            continue;

                        int newCostToNeighbour = node.gCost + GetDistance(node, neighbour);
                        if (newCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                        {
                            neighbour.gCost = newCostToNeighbour;
                            neighbour.hCost = GetDistance(neighbour, targetNode);
                            neighbour.parent = node;

                            if (!openSet.Contains(neighbour))
                                openSet.Add(neighbour);
                            else
                                openSet.UpdateItem(neighbour);
                        }
                    }
                }
            }
            yield return null;
            if (pathSuccess == true)
                waypoints = RetracePath(startNode, targetNode);
            requestManager.FinishedProcessingPath(waypoints, pathSuccess);
        }

        // 경로 얻기
        private Vector3[] RetracePath(Node startNode, Node endNode)
        {
            List<Node> path = new List<Node>();
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
        Vector3[] SimplifyPath(List<Node> path)
        {
            List<Vector3> waypoints = new List<Vector3>();
            Vector2 directionOld = Vector2.zero;

            for (int i = 1; i < path.Count; ++i)
            {
                // 방향이 같은가 확인
                Vector2 directionNew = new Vector2(path[i - 1].gridX - path[i].gridX, path[i - 1].gridY - path[i].gridY);
                if (directionNew != directionOld)
                    waypoints.Add(path[i].worldPosition);
                directionOld = directionNew;
            }
            return waypoints.ToArray();
        }
        // 거리(비용) 구하기

        int GetDistance(Node nodeA, Node nodeB)
        {
            int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
            int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

            if (dstX > dstY)
                return 14 * dstY + 10 * (dstX - dstY);
            return 14 * dstX + 10 * (dstY - dstX);
        }

    }
}