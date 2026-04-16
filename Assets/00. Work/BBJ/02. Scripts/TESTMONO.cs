using Pathfind;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class TESTMONO : MonoBehaviour
{
    public UnityEngine.Grid grid;
    public GameObject prefab;
    public Vector2Int size;
    public Vector2Int offset;

    private Vector3[] _mapGridEdges;
    private Vector3[] _selectGridEdges;

    private static readonly Vector2Int[] rhombusMatrix ={
        new(0, 0), new(0, 1),
        new(1, 1), new(1, 0) };

    public LayerMask unwalkableMask;
    public Vector2 gridWorldSize;
    public float nodeRadius;
    Node[,] dataGrid;

    float nodeDiameter;

    void Awake()
    {
        CreateGrid();
    }
    public int MaxSize => size.x * size.y;
    void CreateGrid()
    {
        dataGrid = new Node[size.x, size.y];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector3 worldPoint = grid.CellToWorld(new Vector3Int(x,y,0));
                //bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));
                dataGrid[x, y] = new Node(true, worldPoint, x, y);
            }
        }
    }

    // 인접 노드 찾기
    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < size.x && checkY >= 0 && checkY < size.y)
                {
                    neighbours.Add(dataGrid[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }

    // 월드 포인트 그리드 좌표계로 변경
    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((size.x - 1) * percentX);
        int y = Mathf.RoundToInt((size.y - 1) * percentY);
        return dataGrid[x, y];
    }


    private void Update()
    {
        //Ray ray =Camera.main.ScreenPointToRay(Input.mousePosition);
        //Physics.Raycast(ray, out RaycastHit hit, Camera.main.farClipPlane);
        //if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
            SelectGridGizemoSetting(mousePos);
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLineStrip(_mapGridEdges, true);
        if (_selectGridEdges != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLineStrip(_selectGridEdges, true);
        }
        //Gizmos.DrawLineList(gizmoList);
    }
    private void OnValidate()
    {
        MapGridGizemoSetting();
    }

    private void SelectGridGizemoSetting(Vector3 selectWorldPos)  
    {
        // 양자화
        Vector3Int selectGridPos = grid.WorldToCell(selectWorldPos * 2);
        FindEdges(Vector3Int.zero,selectGridPos, out _selectGridEdges);

        for (int i = 0; i < _selectGridEdges.Length; ++i)
            _selectGridEdges[i] /= 2;
    }

    private void MapGridGizemoSetting() => FindEdges((Vector3Int)size, (Vector3Int)offset, out _mapGridEdges);

    private void FindEdges(Vector3Int gridSize, Vector3Int offset, out Vector3[] gridEdges)
    {
        gridEdges = new Vector3[rhombusMatrix.Length];

        for (int i = 0; i < rhombusMatrix.Length; ++i)
        {
            Vector3Int edgeGridPos = gridSize * (Vector3Int)rhombusMatrix[i] + offset;
            Vector3 edgeWorldPos = grid.CellToWorld(edgeGridPos);

            gridEdges[i] = edgeWorldPos;
        }
    }
}
