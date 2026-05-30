using BBJ.GridSystem.Objects;
using BBJ.GridSystem.Pathfind;
using UnityEngine;

namespace BBJ.GridSystem
{
    [RequireComponent(typeof(Grid))]
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private Grid _gridCompo;
        [field: SerializeField] public Vector2Int Size { get; private set; }
        [field: SerializeField] public Vector2Int Offset { get; private set; }

        private Node[,] _grid;
        public int MaxSize => Size.x * Size.y;

        public int Version { get; private set; }

        private void Awake()
        {
            _gridCompo = GetComponent<Grid>();
            CreateGrid();
        }

        private void CreateGrid()
        {
            if (Size.x <= 0 || Size.y <= 0) return;

            _grid = new Node[Size.x, Size.y];
            for (int x = 0; x < Size.x; x++)
                for (int y = 0; y < Size.y; y++)
                {
                    Vector3 worldPoint = _gridCompo.CellToWorld(
                        new Vector3Int(x, y, 0) + (Vector3Int)Offset);
                    _grid[x, y] = new Node(true, worldPoint, x, y);
                }
        }


        public bool TryGetCellToNode(int x, int y, out Node node)
        {
            node = null;
            if (x >= 0 && x < Size.x && y >= 0 && y < Size.y)
                node = _grid[x, y];
            return node != null;
        }

        public Node NodeFromWorldPoint(Vector3 worldPosition)
        {
            if (_gridCompo == null) return null;
            Vector3Int pos = _gridCompo.WorldToCell(worldPosition) - (Vector3Int)Offset;
            if (TryGetCellToNode(pos.x, pos.y, out Node node))
                return node;

            return null;
        }

        public void ApplyObstacleAt(TileSetData data, Vector2Int cellIndex)
        {
            if (data == null) return;

            if (TryGetCellToNode(cellIndex.x, cellIndex.y, out Node rootNode))
                rootNode.walkable = data.IsWalkable;

            if (data.BlockedOffsets == null) return;
            foreach (var offset in data.BlockedOffsets)
            {
                if (TryGetCellToNode(cellIndex.x + offset.x, cellIndex.y + offset.y, out Node node))
                    node.walkable = data.IsWalkable;
            }
        }

        public void SetNodeWalkable(Vector2Int cell, bool walkable)
        {
            if (TryGetCellToNode(cell.x, cell.y, out Node node))
            {
                node.walkable = walkable;
                Version++;
            }
        }
        public Vector3 CellToWorld(Vector2Int cellIndex)
        {
            if (_gridCompo == null) return Vector3.zero;
            return _gridCompo.CellToWorld((Vector3Int)(cellIndex + Offset));
        }
        private void OnValidate()
        {
            if (_gridCompo == null)
                _gridCompo = GetComponent<Grid>();

#if UNITY_EDITOR
            MapGridGizmoSetting();
            GenerateCellTemplateMesh();
#endif
        }

#if UNITY_EDITOR
        private static readonly Vector2Int[] rhombusMatrix =
        {
            new(0, 0), new(0, 1),
            new(1, 1), new(1, 0)
        };
        private Mesh _mapGridMesh;
        private Mesh _cellTemplateMesh;

        private void MapGridGizmoSetting()
        {
            if (_gridCompo == null || Size.x <= 0 || Size.y <= 0) return;
            _mapGridMesh = CreateQuadMesh((Vector3Int)Size, (Vector3Int)Offset);
        }

        private void GenerateCellTemplateMesh()
        {
            if (_gridCompo == null) return;

            Vector3[] vertices = new Vector3[rhombusMatrix.Length];
            Vector3 originPos = _gridCompo.CellToWorld(Vector3Int.zero);

            for (int i = 0; i < rhombusMatrix.Length; ++i)
            {
                Vector3Int localCellPos = new Vector3Int(rhombusMatrix[i].x, rhombusMatrix[i].y, 0);
                vertices[i] = _gridCompo.CellToWorld(localCellPos) - originPos;
            }

            _cellTemplateMesh = new Mesh();
            _cellTemplateMesh.vertices = vertices;
            _cellTemplateMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            _cellTemplateMesh.RecalculateNormals();
        }

        private Mesh CreateQuadMesh(Vector3Int gridSize, Vector3Int offset)
        {
            Vector3[] vertices = new Vector3[rhombusMatrix.Length];

            for (int i = 0; i < rhombusMatrix.Length; ++i)
            {
                Vector3Int localCellPos = new Vector3Int(
                    gridSize.x * rhombusMatrix[i].x,
                    gridSize.y * rhombusMatrix[i].y,
                    0
                );
                vertices[i] = _gridCompo.CellToWorld(localCellPos + offset);
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();

            return mesh;
        }
        private void OnDrawGizmosSelected()
        {
            Matrix4x4 originalMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.identity;

            if (_mapGridMesh != null)
            {
                Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
                Gizmos.DrawMesh(_mapGridMesh, Vector3.zero, Quaternion.identity);
            }

            if (_grid != null && _cellTemplateMesh != null)
            {
                foreach (Node node in _grid)
                {
                    if (node == null) continue;

                    Color tileColor = node.walkable ? new Color(0f, 1f, 0f, 0.3f) : new Color(1f, 0f, 0f, 0.5f);
                    Gizmos.color = tileColor;
                    Gizmos.DrawMesh(_cellTemplateMesh, node.worldPosition, Quaternion.identity);
                }
            }
            Gizmos.matrix = originalMatrix;
        }
#endif
    }
}