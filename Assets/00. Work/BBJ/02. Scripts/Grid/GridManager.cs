using BBJ.GridSystem.Objects;
using BBJ.GridSystem.Pathfind;
using UnityEngine;

namespace BBJ.GridSystem
{
    [RequireComponent(typeof(Grid))]
    public class GridManager : MonoBehaviour
    {
        [field: SerializeField] public Vector2Int Size   { get; private set; }
        [field: SerializeField] public Vector2Int Offset { get; private set; }

        [SerializeField] private Grid _gridCompo;
        private Node[,] _grid;

        private static readonly Vector2Int[] rhombusMatrix =
        {
            new(0, 0), new(0, 1),
            new(1, 1), new(1, 0)
        };

        private Vector3[] _mapGridEdges;

#if UNITY_EDITOR
        private Vector3[] _selectGridEdges;
        private bool      _isSelectGridWalkable = true;
#endif

        private void Awake()
        {
            _gridCompo = GetComponent<Grid>();
            CreateGrid();
            // 오브젝트 배치 및 장애물 등록은 ObjectManager 단독 담당.
            // GridManager의 ApplyObstacle() 제거 — ObjectManager와 이중 스폰 발생했던 버그 수정.
        }

        private void CreateGrid()
        {
            _grid = new Node[Size.x, Size.y];
            for (int x = 0; x < Size.x; x++)
            for (int y = 0; y < Size.y; y++)
            {
                Vector3 worldPoint = _gridCompo.CellToWorld(
                    new Vector3Int(x, y, 0) + (Vector3Int)Offset);
                _grid[x, y] = new Node(true, worldPoint, x, y);
            }
        }

        public int MaxSize => Size.x * Size.y;

        public bool TryGetCellToNode(int x, int y, out Node node)
        {
            node = null;
            if (x >= 0 && x < Size.x && y >= 0 && y < Size.y)
                node = _grid[x, y];
            return node != null;
        }

        /// <summary>
        /// 월드 좌표 → 노드. 그리드 범위 밖이면 null 반환.
        /// 기존: 범위 체크 없는 직접 인덱싱 → IndexOutOfRangeException 가능.
        /// </summary>
        public Node NodeFromWorldPoint(Vector3 worldPosition)
        {
            Vector3Int pos = _gridCompo.WorldToCell(worldPosition) - (Vector3Int)Offset;
            if (TryGetCellToNode(pos.x, pos.y, out Node node))
                return node;

            Debug.LogWarning($"[GridManager] 그리드 범위 밖: {worldPosition} → cell({pos.x},{pos.y})");
            return null;
        }

        public void ApplyObstacleAt(ObjectData data, Vector2Int cellIndex)
        {
            if (data == null) return;

            if (TryGetCellToNode(cellIndex.x, cellIndex.y, out Node rootNode))
                rootNode.walkable = data.IsWalkable;

            if (data.BlockedOffsets == null) return;
            foreach (var offset in data.BlockedOffsets)
            {
                if (TryGetCellToNode(cellIndex.x + offset.x, cellIndex.y + offset.y, out Node offsetNode))
                    offsetNode.walkable = data.IsWalkable;
            }
        }

        public Vector3 CellToWorld(Vector2Int cellIndex)
            => _gridCompo.CellToWorld((Vector3Int)(cellIndex + Offset));

#if UNITY_EDITOR
        private void Update()
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(
                UnityEngine.InputSystem.Mouse.current.position.value);
            SelectGridGizmoSetting(mousePos);
        }

        private void SelectGridGizmoSetting(Vector3 selectWorldPos)
        {
            Vector3Int selectGridPos = _gridCompo.WorldToCell(selectWorldPos * 2);
            selectGridPos /= 2;
            FindEdges(Vector3Int.one, selectGridPos, out _selectGridEdges);

            Vector3Int indexPos = selectGridPos - (Vector3Int)Offset;
            indexPos.x = Mathf.Clamp(indexPos.x, 0, Size.x - 1);
            indexPos.y = Mathf.Clamp(indexPos.y, 0, Size.y - 1);
            _isSelectGridWalkable = _grid[indexPos.x, indexPos.y].walkable;
        }
#endif

        private void OnValidate() => MapGridGizmoSetting();

        private void MapGridGizmoSetting()
            => FindEdges((Vector3Int)Size, (Vector3Int)Offset, out _mapGridEdges);

        private void FindEdges(Vector3Int gridSize, Vector3Int offset, out Vector3[] gridEdges)
        {
            gridEdges = new Vector3[rhombusMatrix.Length];
            for (int i = 0; i < rhombusMatrix.Length; ++i)
                gridEdges[i] = _gridCompo.CellToWorld(gridSize * (Vector3Int)rhombusMatrix[i] + offset);
        }

        private void OnDrawGizmosSelected()
        {
            if (_mapGridEdges != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLineStrip(_mapGridEdges, true);
            }
#if UNITY_EDITOR
            if (_selectGridEdges != null)
            {
                Gizmos.color = _isSelectGridWalkable ? Color.green : Color.red;
                Gizmos.DrawLineStrip(_selectGridEdges, true);
            }
#endif
        }
    }
}
