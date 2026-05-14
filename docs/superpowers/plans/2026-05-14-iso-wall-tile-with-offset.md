# IsoWallTile with Offset Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기존 IsoAutoWallTile을 대체하는 새 ScriptableObject 타일 `IsoWallTile`을 작성한다. 하나의 SO로 Left/Right wall을 모두 관리하며, 중앙 기준 타일 대비 각 타일의 간격을 픽셀 단위로 조절하는 오프셋 기능을 포함한다.

**Architecture:**
- 새 스크립트 `IsoWallTile.cs` 를 작성한다. 기존 `IsoAutoWallTile.cs`는 건드리지 않는다.
- 오프셋은 `GetTileData` 내에서 `centerTilePosition`(SO 인스펙터 지정)과의 그리드 델타를 기반으로 world-space 평행이동을 계산하여 transform matrix에 추가한다.
- 중앙 타일 선택을 돕는 커스텀 에디터 `IsoWallTileEditor.cs`를 함께 작성한다.

**Tech Stack:** Unity 2D Tilemap (Isometric Z as Y, Cell Swizzle XYZ), TileBase, ScriptableObject, UnityEditor CustomEditor

---

## 파일 구조

| 역할 | 파일 경로 |
|---|---|
| 신규 타일 (runtime) | `Assets/00. Work/CheolYee/02. Scripts/TileMaps/IsoWallTile.cs` |
| 커스텀 에디터 | `Assets/00. Work/CheolYee/02. Scripts/TileMaps/Editor/IsoWallTileEditor.cs` |
| EditMode 테스트 | `Assets/00. Work/CheolYee/02. Scripts/TileMaps/Editor/IsoWallTileTests.cs` ← **기존 파일에 추가하지 않고 분리** |

기존 파일 변경 없음: `IsoAutoWallTile.cs`, `IsoAutoWallTileTests.cs`

---

## 설계 메모 (코드 작성 전 참고)

### 오프셋 방향 계산 (isoAngle 활용)

Unity Isometric Z as Y (cellSize 1, 0.5, 1) 기준:
- 그리드 X+ 방향 world: `(cosA, -sinA)` (angle = isoAngleDegrees in radians, default 26.565°)
- 그리드 Y+ 방향 world: `(-cosA, -sinA)`

타일 at `delta = position - centerTilePosition`의 추가 world offset:
```
worldOffset.x = delta.x * (clampedOffX / ppu) * cosA
              + delta.y * (clampedOffY / ppu) * (-cosA)

worldOffset.y = delta.x * (clampedOffX / ppu) * (-sinA)
              + delta.y * (clampedOffY / ppu) * (-sinA)
```

### 오프셋 클램프 (중앙을 넘지 못하는 제한)

자연 셀 간격(월드)은 그리드 1칸 이동 시 `sqrt((cellX/2)² + (cellY/2)²)`이다.  
오프셋이 이 이상의 음수이면 타일이 중앙을 넘어버린다.

```
naturalStep = sqrt((gridCellSize.x * 0.5)^2 + (gridCellSize.y * 0.5)^2)
minOffsetPixels = -naturalStep * pixelsPerUnit
clampedOffX = Max(offsetPixels.x, minOffsetPixels)
clampedOffY = Max(offsetPixels.y, minOffsetPixels)
```

표준 iso (cellSize 1, 0.5, ppu 512): `naturalStep ≈ 0.559`, `minOffset ≈ -286px`

### Mirror (Right wall)

- Right wall: `scale.x = -1` (기존 IsoAutoWallTile과 동일)
- Left offset 값 ≠ Right offset 값 → 각각 별도 필드
- mirror 타일에는 `rightWallOffsetPixels`를 사용

### 인스펙터 중앙 타일 Picker

- SO에 `Vector3Int centerTilePosition` 저장
- Editor에서 씬 뷰 클릭으로 좌표를 세팅하는 Pick 모드 제공
- Pick 모드는 씬에서 찾은 첫 번째 Tilemap을 기준으로 WorldToCell 변환

---

## Task 1: IsoWallTile 필드 선언 + 컴파일 확인

**Files:**
- Create: `Assets/00. Work/CheolYee/02. Scripts/TileMaps/IsoWallTile.cs`

- [ ] **Step 1: 파일 생성 (stub 포함)**

```csharp
using UnityEngine;
using UnityEngine.Tilemaps;
using _00._Work.CheolYee._02._Scripts.TileMaps;

namespace _00._Work.CheolYee._02._Scripts.TileMaps
{
    [CreateAssetMenu(fileName = "IsoWallTile", menuName = "Tiles/Iso Wall Tile (Offset)")]
    public class IsoWallTile : TileBase
    {
        [Header("Visual")]
        [SerializeField] private Sprite sprite;
        [SerializeField] private bool useTint;
        [SerializeField] private Color leftWallColor = Color.white;
        [SerializeField] private Color rightWallColor = Color.white;

        [Header("Rule")]
        [Tooltip("X축으로 이어진 타일이 Left Wall인지 Right Wall인지 결정")]
        [SerializeField] private WallSide xAxisWallSide = WallSide.Left;
        [Tooltip("코너(X·Y 둘 다 이웃)일 때 어느 축을 우선할지")]
        [SerializeField] private AxisPriority axisPriority = AxisPriority.XFirst;
        [Tooltip("고립된 1칸 타일의 기본 방향")]
        [SerializeField] private WallSide isolatedWallSide = WallSide.Left;

        [Header("Transform")]
        [SerializeField] private Vector2 defaultPlacementPivotPixels = Vector2.zero;
        [SerializeField] private bool useMirrorPivotCompensation = true;
        [SerializeField] private Vector2 mirroredPlacementPivotPixels = new(256f, 128f);

        [Header("Physics")]
        [SerializeField] private Tile.ColliderType colliderType = Tile.ColliderType.None;

        [Header("Offset")]
        [Tooltip("이 타일 좌표를 기준점(중앙)으로 삼아 오프셋 계산")]
        [SerializeField] private Vector3Int centerTilePosition = Vector3Int.zero;
        [Tooltip("아이소메트릭 그리드 기울기 각도 (기본 26.565°)")]
        [SerializeField] private float isoAngleDegrees = 26.565f;
        [Tooltip("그리드 셀 크기 (X, Y). 기본값: Unity 표준 Iso (1, 0.5)")]
        [SerializeField] private Vector2 gridCellSize = new(1f, 0.5f);
        [Tooltip("스프라이트 PPU (기본 512)")]
        [SerializeField] private float pixelsPerUnit = 512f;
        [Tooltip("Left Wall 타일의 오프셋 (픽셀/그리드 단위). X = 그리드 X축 간격, Y = 그리드 Y축 간격")]
        [SerializeField] private Vector2 leftWallOffsetPixels = Vector2.zero;
        [Tooltip("Right Wall 타일의 오프셋 (픽셀/그리드 단위). X = 그리드 X축 간격, Y = 그리드 Y축 간격")]
        [SerializeField] private Vector2 rightWallOffsetPixels = Vector2.zero;

        private static readonly Matrix4x4 NormalMatrix = Matrix4x4.identity;

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            tilemap.RefreshTile(position);
            tilemap.RefreshTile(position + Vector3Int.left);
            tilemap.RefreshTile(position + Vector3Int.right);
            tilemap.RefreshTile(position + Vector3Int.up);
            tilemap.RefreshTile(position + Vector3Int.down);
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            // TODO: Task 2에서 구현
        }
    }
}
```

- [ ] **Step 2: Unity MCP로 컴파일 확인**

`read_console(types=["error"], count=10)` 실행 후 0 errors 확인.

- [ ] **Step 3: Commit**

```bash
git add "Assets/00. Work/CheolYee/02. Scripts/TileMaps/IsoWallTile.cs"
git commit -m "feat: add IsoWallTile stub with all fields"
```

---

## Task 2: WallSide 로직 이식 + GetTileData 구현 + 기본 테스트

**Files:**
- Modify: `Assets/00. Work/CheolYee/02. Scripts/TileMaps/IsoWallTile.cs`
- Create: `Assets/00. Work/CheolYee/02. Scripts/TileMaps/Editor/IsoWallTileTests.cs`

- [ ] **Step 1: 실패하는 테스트 작성**

```csharp
// Assets/00. Work/CheolYee/02. Scripts/TileMaps/Editor/IsoWallTileTests.cs
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
using _00._Work.CheolYee._02._Scripts.TileMaps;

public class IsoWallTileTests
{
    private readonly List<Object> _created = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = _created.Count - 1; i >= 0; i--)
        {
            Object obj = _created[i];
            if (obj != null) Object.DestroyImmediate(obj);
        }
        _created.Clear();
    }

    // --- Task 2: 기본 WallSide ---

    [Test]
    public void LeftWall_XNeighbor_IsNotMirrored()
    {
        // xAxisWallSide = Left이면, X이웃이 있을 때 mirror 없이 identity에 가까운 transform
        Tilemap tilemap = CreateTilemap();
        IsoWallTile tile = CreateTile(xAxisWallSide: WallSide.Left);
        SetField(tile, "leftWallOffsetPixels", Vector2.zero);
        SetField(tile, "rightWallOffsetPixels", Vector2.zero);

        tilemap.SetTile(Vector3Int.zero, tile);
        tilemap.SetTile(Vector3Int.right, tile);
        tilemap.RefreshAllTiles();

        Matrix4x4 m = tilemap.GetTransformMatrix(Vector3Int.zero);
        // scale.x = 1 (not mirrored)
        Assert.That(m.m00, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void RightWall_XNeighbor_IsMirrored()
    {
        // xAxisWallSide = Right이면, X이웃이 있을 때 mirror (scale.x = -1)
        Tilemap tilemap = CreateTilemap();
        IsoWallTile tile = CreateTile(xAxisWallSide: WallSide.Right);
        SetField(tile, "leftWallOffsetPixels", Vector2.zero);
        SetField(tile, "rightWallOffsetPixels", Vector2.zero);

        tilemap.SetTile(Vector3Int.zero, tile);
        tilemap.SetTile(Vector3Int.right, tile);
        tilemap.RefreshAllTiles();

        Matrix4x4 m = tilemap.GetTransformMatrix(Vector3Int.zero);
        // scale.x = -1 (mirrored)
        Assert.That(m.m00, Is.EqualTo(-1f).Within(0.001f));
    }

    // --- Task 3: 오프셋 ---

    [Test]
    public void CenterTile_HasZeroOffset()
    {
        Tilemap tilemap = CreateTilemap();
        IsoWallTile tile = CreateTileWithOffset(
            center: Vector3Int.zero,
            leftOffset: new Vector2(512f, 0f));

        tilemap.SetTile(Vector3Int.zero, tile);
        tilemap.SetTile(Vector3Int.right, tile);
        tilemap.RefreshAllTiles();

        // 중앙(0,0)은 offset delta = (0,0) → 추가 이동 없음 → m03, m13 = 0
        Matrix4x4 m = tilemap.GetTransformMatrix(Vector3Int.zero);
        Assert.That(m.m03, Is.EqualTo(0f).Within(0.001f));
        Assert.That(m.m13, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void TileAtDeltaX1_WithLeftOffsetX512_HasExpectedWorldOffset()
    {
        // delta=(1,0), leftOffsetX=512px, ppu=512
        // worldOffX = 1*(512/512)*cos(26.565°) ≈ 0.894
        // worldOffY = 1*(512/512)*(-sin(26.565°)) ≈ -0.447
        Tilemap tilemap = CreateTilemap();
        IsoWallTile tile = CreateTileWithOffset(
            center: Vector3Int.zero,
            leftOffset: new Vector2(512f, 0f));

        tilemap.SetTile(new Vector3Int(1, 0, 0), tile);
        tilemap.SetTile(new Vector3Int(2, 0, 0), tile); // X이웃을 만들어 Left wall 확정
        tilemap.RefreshAllTiles();

        Matrix4x4 m = tilemap.GetTransformMatrix(new Vector3Int(1, 0, 0));
        Assert.That(m.m03, Is.EqualTo(Mathf.Cos(26.565f * Mathf.Deg2Rad)).Within(0.005f));
        Assert.That(m.m13, Is.EqualTo(-Mathf.Sin(26.565f * Mathf.Deg2Rad)).Within(0.005f));
    }

    [Test]
    public void NegativeOffset_ClampedAtCenter_DoesNotCrossCenter()
    {
        // offset = minOffsetPixels = -(sqrt(0.5^2 + 0.25^2) * 512) ≈ -286.2px
        // tile at (1,0):
        //   ox = minPx / 512 = -naturalStep ≈ -0.5590
        //   worldX = 1 * (-naturalStep) * cosA = -0.5590 * 0.8944 = -0.5000  (수학적으로 정확히 -0.5)
        //   → 타일이 중앙(0,0)의 world X 위치까지 정확히 수렴
        float naturalStep = Mathf.Sqrt(0.5f * 0.5f + 0.25f * 0.25f); // ≈ 0.5590
        float minPx = -naturalStep * 512f;

        Tilemap tilemap = CreateTilemap();
        IsoWallTile tile = CreateTileWithOffset(
            center: Vector3Int.zero,
            leftOffset: new Vector2(minPx, 0f));

        tilemap.SetTile(new Vector3Int(1, 0, 0), tile);
        tilemap.SetTile(new Vector3Int(2, 0, 0), tile);
        tilemap.RefreshAllTiles();

        Matrix4x4 m = tilemap.GetTransformMatrix(new Vector3Int(1, 0, 0));
        // worldX = -naturalStep * cosA = -sqrt(5/16) * (2/sqrt(5)) = -0.5 (정확한 값)
        float expectedX = -naturalStep * Mathf.Cos(26.565f * Mathf.Deg2Rad);
        Assert.That(m.m03, Is.EqualTo(expectedX).Within(0.005f));
    }

    [Test]
    public void OffsetBeyondMin_ClampedToMin()
    {
        float naturalStep = Mathf.Sqrt(0.5f * 0.5f + 0.25f * 0.25f);
        float minPx = -naturalStep * 512f;
        float beyondMin = minPx - 100f; // 클램프 범위 초과

        Tilemap tilemap = CreateTilemap();
        IsoWallTile tile = CreateTileWithOffset(
            center: Vector3Int.zero,
            leftOffset: new Vector2(beyondMin, 0f));
        IsoWallTile tileSameMin = CreateTileWithOffset(
            center: Vector3Int.zero,
            leftOffset: new Vector2(minPx, 0f));

        tilemap.SetTile(new Vector3Int(1, 0, 0), tile);
        tilemap.SetTile(new Vector3Int(2, 0, 0), tile);
        tilemap.RefreshAllTiles();

        GameObject go2 = new("G2");
        go2.hideFlags = HideFlags.HideAndDontSave;
        _created.Add(go2);
        Grid grid2 = go2.AddComponent<Grid>();
        grid2.cellLayout = GridLayout.CellLayout.Isometric;
        grid2.cellSize = new Vector3(1f, 0.5f, 1f);
        GameObject to2 = new("T2");
        to2.hideFlags = HideFlags.HideAndDontSave;
        _created.Add(to2);
        to2.transform.SetParent(go2.transform, false);
        Tilemap tilemap2 = to2.AddComponent<Tilemap>();
        to2.AddComponent<TilemapRenderer>();
        tilemap2.tileAnchor = new Vector3(0.5f, 0.5f, 0f);

        tilemap2.SetTile(new Vector3Int(1, 0, 0), tileSameMin);
        tilemap2.SetTile(new Vector3Int(2, 0, 0), tileSameMin);
        tilemap2.RefreshAllTiles();

        Matrix4x4 m1 = tilemap.GetTransformMatrix(new Vector3Int(1, 0, 0));
        Matrix4x4 m2 = tilemap2.GetTransformMatrix(new Vector3Int(1, 0, 0));
        Assert.That(m1.m03, Is.EqualTo(m2.m03).Within(0.001f));
        Assert.That(m1.m13, Is.EqualTo(m2.m13).Within(0.001f));
    }

    [Test]
    public void RightWallUsesRightOffsetPixels()
    {
        // xAxisWallSide = Right → X이웃 있을 때 mirror (Right wall)
        // rightOffset = (256, 0), leftOffset = (0, 0)
        // tile at (1,0) from center: worldOff.x = 1*(256/512)*cos ≈ 0.447
        Tilemap tilemap = CreateTilemap();
        IsoWallTile tile = CreateTile(xAxisWallSide: WallSide.Right);
        SetField(tile, "centerTilePosition", Vector3Int.zero);
        SetField(tile, "isoAngleDegrees", 26.565f);
        SetField(tile, "gridCellSize", new Vector2(1f, 0.5f));
        SetField(tile, "pixelsPerUnit", 512f);
        SetField(tile, "leftWallOffsetPixels", Vector2.zero);
        SetField(tile, "rightWallOffsetPixels", new Vector2(256f, 0f));

        tilemap.SetTile(new Vector3Int(1, 0, 0), tile);
        tilemap.SetTile(new Vector3Int(2, 0, 0), tile);
        tilemap.RefreshAllTiles();

        Matrix4x4 m = tilemap.GetTransformMatrix(new Vector3Int(1, 0, 0));
        Assert.That(m.m00, Is.EqualTo(-1f).Within(0.001f)); // mirror
        // rightOffset 적용: worldOff.x = 1*(256/512)*cos(26.565°) ≈ 0.447
        Assert.That(m.m03, Is.EqualTo(0.5f * Mathf.Cos(26.565f * Mathf.Deg2Rad)).Within(0.005f));
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private Tilemap CreateTilemap()
    {
        GameObject gridGo = new("TestGrid") { hideFlags = HideFlags.HideAndDontSave };
        _created.Add(gridGo);
        Grid grid = gridGo.AddComponent<Grid>();
        grid.cellLayout = GridLayout.CellLayout.Isometric;
        grid.cellSize = new Vector3(1f, 0.5f, 1f);

        GameObject tmGo = new("TestTilemap") { hideFlags = HideFlags.HideAndDontSave };
        _created.Add(tmGo);
        tmGo.transform.SetParent(gridGo.transform, false);
        Tilemap tilemap = tmGo.AddComponent<Tilemap>();
        tmGo.AddComponent<TilemapRenderer>();
        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
        return tilemap;
    }

    private IsoWallTile CreateTile(WallSide xAxisWallSide = WallSide.Left)
    {
        IsoWallTile tile = ScriptableObject.CreateInstance<IsoWallTile>();
        tile.hideFlags = HideFlags.HideAndDontSave;
        _created.Add(tile);

        Texture2D tex = new(256, 512, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
        _created.Add(tex);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 256, 512), new Vector2(0f, 0f), 512f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        _created.Add(sprite);

        SetField(tile, "sprite", sprite);
        SetField(tile, "xAxisWallSide", xAxisWallSide);
        SetField(tile, "useMirrorPivotCompensation", false); // 피벗 오프셋 없애서 순수 offset만 검사
        SetField(tile, "pixelsPerUnit", 512f);
        SetField(tile, "gridCellSize", new Vector2(1f, 0.5f));
        SetField(tile, "isoAngleDegrees", 26.565f);
        SetField(tile, "centerTilePosition", Vector3Int.zero);
        return tile;
    }

    private IsoWallTile CreateTileWithOffset(Vector3Int center, Vector2 leftOffset)
    {
        IsoWallTile tile = CreateTile(WallSide.Left);
        SetField(tile, "centerTilePosition", center);
        SetField(tile, "leftWallOffsetPixels", leftOffset);
        SetField(tile, "rightWallOffsetPixels", Vector2.zero);
        return tile;
    }

    private void SetField(object target, string name, object value)
    {
        FieldInfo f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(f, Is.Not.Null, $"Field '{name}' not found");
        f.SetValue(target, value);
    }
}
```

- [ ] **Step 2: 테스트 실행 → FAIL 확인**

Unity Test Runner (Window > General > Test Runner) > EditMode 탭에서 `IsoWallTileTests` 실행.  
`GetTileData` 미구현이므로 FAIL 예상.

- [ ] **Step 3: IsoWallTile.cs에 WallSide 로직 + GetTileData 구현**

`IsoWallTile.cs`의 `GetTileData` 및 private 메서드를 다음으로 교체/추가:

```csharp
public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
{
    tileData.sprite = sprite;
    tileData.gameObject = null;
    tileData.colliderType = colliderType;
    tileData.flags = TileFlags.LockTransform | TileFlags.LockColor;

    bool hasXNeighbor = HasSameTile(tilemap, position + Vector3Int.left)
                     || HasSameTile(tilemap, position + Vector3Int.right);
    bool hasYNeighbor = HasSameTile(tilemap, position + Vector3Int.up)
                     || HasSameTile(tilemap, position + Vector3Int.down);

    WallSide resolvedSide = ResolveWallSide(hasXNeighbor, hasYNeighbor);
    bool mirrorX = resolvedSide == WallSide.Right;

    tileData.transform = BuildTransform(mirrorX, position);
    tileData.color = useTint
        ? (resolvedSide == WallSide.Left ? leftWallColor : rightWallColor)
        : Color.white;
}

private WallSide ResolveWallSide(bool hasXNeighbor, bool hasYNeighbor)
{
    if (hasXNeighbor && !hasYNeighbor) return xAxisWallSide;
    if (!hasXNeighbor && hasYNeighbor) return Opposite(xAxisWallSide);
    if (hasXNeighbor && hasYNeighbor)
        return axisPriority == AxisPriority.XFirst ? xAxisWallSide : Opposite(xAxisWallSide);
    return isolatedWallSide;
}

private WallSide Opposite(WallSide side) =>
    side == WallSide.Left ? WallSide.Right : WallSide.Left;

private bool HasSameTile(ITilemap tilemap, Vector3Int pos) =>
    tilemap.GetTile(pos) == this;

private Matrix4x4 BuildTransform(bool mirrorX, Vector3Int position)
{
    Vector2 offsetPixels = mirrorX ? rightWallOffsetPixels : leftWallOffsetPixels;
    Vector2 extraOffset = ComputeOffsetWorld(position, offsetPixels);

    if (sprite == null)
        return mirrorX
            ? BuildMirrorMatrix(extraOffset)
            : BuildPlacementMatrix(extraOffset);

    if (!mirrorX)
        return BuildPlacementMatrix(GetDefaultPlacementWorld(sprite) + extraOffset);

    return useMirrorPivotCompensation
        ? BuildMirrorMatrix(GetMirroredPlacementWorld(sprite) + extraOffset)
        : BuildMirrorMatrix(extraOffset);
}

private Vector2 ComputeOffsetWorld(Vector3Int position, Vector2 offsetPixels)
{
    Vector3Int delta = position - centerTilePosition;
    if (delta.x == 0 && delta.y == 0) return Vector2.zero;

    float ppu = pixelsPerUnit > 0f ? pixelsPerUnit : 512f;

    // 클램프: 자연 셀 간격 이상의 음수 오프셋 방지
    float halfX = gridCellSize.x * 0.5f;
    float halfY = gridCellSize.y * 0.5f;
    float naturalStep = Mathf.Sqrt(halfX * halfX + halfY * halfY);
    float minPx = -naturalStep * ppu;

    float ox = Mathf.Max(offsetPixels.x, minPx) / ppu;
    float oy = Mathf.Max(offsetPixels.y, minPx) / ppu;

    float angle = isoAngleDegrees * Mathf.Deg2Rad;
    float cosA = Mathf.Cos(angle);
    float sinA = Mathf.Sin(angle);

    // 그리드 X 방향: (cosA, -sinA), 그리드 Y 방향: (-cosA, -sinA)
    float worldX = delta.x * ox * cosA  + delta.y * oy * (-cosA);
    float worldY = delta.x * ox * (-sinA) + delta.y * oy * (-sinA);

    return new Vector2(worldX, worldY);
}

private static Matrix4x4 BuildPlacementMatrix(Vector2 t) =>
    Matrix4x4.TRS(new Vector3(t.x, t.y, 0f), Quaternion.identity, Vector3.one);

private static Matrix4x4 BuildMirrorMatrix(Vector2 t) =>
    Matrix4x4.TRS(new Vector3(t.x, t.y, 0f), Quaternion.identity, new Vector3(-1f, 1f, 1f));

private Vector2 GetDefaultPlacementWorld(Sprite s)
{
    float ppu = GetSpritePPU(s);
    return new Vector2(
        (s.pivot.x - defaultPlacementPivotPixels.x) / ppu,
        (s.pivot.y - defaultPlacementPivotPixels.y) / ppu);
}

private Vector2 GetMirroredPlacementWorld(Sprite s)
{
    float ppu = GetSpritePPU(s);
    return new Vector2(
        (mirroredPlacementPivotPixels.x - s.pivot.x) / ppu,
        (s.pivot.y - mirroredPlacementPivotPixels.y) / ppu);
}

private static float GetSpritePPU(Sprite s) =>
    s != null && s.pixelsPerUnit > 0f ? s.pixelsPerUnit : 100f;
```

- [ ] **Step 4: 컴파일 확인 → 테스트 실행 → PASS 확인**

`read_console(types=["error"], count=10)` → 0 errors.  
Test Runner에서 `IsoWallTileTests` 전체 PASS 확인.

- [ ] **Step 5: Commit**

```bash
git add "Assets/00. Work/CheolYee/02. Scripts/TileMaps/IsoWallTile.cs"
git add "Assets/00. Work/CheolYee/02. Scripts/TileMaps/Editor/IsoWallTileTests.cs"
git commit -m "feat: implement IsoWallTile with offset and TDD tests"
```

---

## Task 3: 커스텀 에디터 (중앙 타일 Picker)

**Files:**
- Create: `Assets/00. Work/CheolYee/02. Scripts/TileMaps/Editor/IsoWallTileEditor.cs`

- [ ] **Step 1: 에디터 스크립트 작성**

```csharp
// Assets/00. Work/CheolYee/02. Scripts/TileMaps/Editor/IsoWallTileEditor.cs
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _00._Work.CheolYee._02._Scripts.TileMaps.Editor
{
    [CustomEditor(typeof(IsoWallTile))]
    public class IsoWallTileEditor : UnityEditor.Editor
    {
        private bool _isPicking;
        private Tilemap _referenceTilemap;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("─ Center Tile Picker ─", EditorStyles.boldLabel);

            _referenceTilemap = (Tilemap)EditorGUILayout.ObjectField(
                "Reference Tilemap", _referenceTilemap, typeof(Tilemap), allowSceneObjects: true);

            using (new EditorGUI.DisabledScope(_referenceTilemap == null))
            {
                string label = _isPicking ? "[ 씬 클릭으로 타일 선택 중... 취소 ]" : "Pick Center Tile from Scene";
                if (GUILayout.Button(label))
                    _isPicking = !_isPicking;
            }

            if (_isPicking)
                EditorGUILayout.HelpBox("씬 뷰에서 타일을 클릭하면 Center Tile Position이 설정됩니다.", MessageType.Info);

            if (_referenceTilemap == null)
                EditorGUILayout.HelpBox("Reference Tilemap을 씬에서 드래그해서 연결하세요.", MessageType.Warning);
        }

        private void OnSceneGUI()
        {
            if (!_isPicking || _referenceTilemap == null) return;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            Event e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0) return;

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            // 아이소메트릭 Z=0 평면과 교차
            float t = -ray.origin.z / ray.direction.z;
            if (float.IsInfinity(t) || float.IsNaN(t)) return;

            Vector3 worldPos = ray.origin + ray.direction * t;
            Vector3Int cellPos = _referenceTilemap.WorldToCell(worldPos);

            SerializedObject so = new(target);
            so.FindProperty("centerTilePosition").vector3IntValue = cellPos;
            so.ApplyModifiedProperties();

            _isPicking = false;
            e.Use();

            // 변경된 타일맵을 갱신
            _referenceTilemap.RefreshAllTiles();
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

`read_console(types=["error"], count=10)` → 0 errors.

- [ ] **Step 3: Commit**

```bash
git add "Assets/00. Work/CheolYee/02. Scripts/TileMaps/Editor/IsoWallTileEditor.cs"
git commit -m "feat: add IsoWallTileEditor with center tile picker"
```

---

## Task 4: Unity 내 수동 통합 검증

**Files:** 없음 (에디터 조작)

- [ ] **Step 1: IsoWallTile SO 생성**

Project 창 우클릭 > Create > Tiles > Iso Wall Tile (Offset) → `IsoWallTile_Test.asset` 생성.

- [ ] **Step 2: 기본 세팅**

| 필드 | 값 |
|---|---|
| Sprite | 기존 벽 스프라이트 |
| xAxisWallSide | Left |
| isolatedWallSide | Left |
| defaultPlacementPivotPixels | (0, 0) |
| useMirrorPivotCompensation | true |
| mirroredPlacementPivotPixels | (256, 128) |
| pixelsPerUnit | 512 |
| gridCellSize | (1, 0.5) |
| isoAngleDegrees | 26.565 |
| centerTilePosition | (0, 0, 0) |
| leftWallOffsetPixels | (0, 0) |
| rightWallOffsetPixels | (0, 0) |

- [ ] **Step 3: 타일맵에 타일 배치 및 offset=0 기준 외관 확인**

Tilemap에 IsoWallTile_Test를 10칸 가로로 배치.  
`xAxisWallSide = Left` → 전부 Left wall 스프라이트 표시 확인.  
Y이웃 한 줄 추가 → mirror(Right wall) 적용 확인.

- [ ] **Step 4: Center Tile Picker 검증**

Inspector > Reference Tilemap에 해당 타일맵 드래그.  
"Pick Center Tile from Scene" 클릭 → 씬 중앙 타일 클릭.  
`centerTilePosition` 값이 클릭한 셀 좌표로 설정됨 확인.

- [ ] **Step 5: Offset 동작 검증**

`leftWallOffsetPixels.x = 50` 설정 → 타일맵 RefreshAllTiles() 실행.  
타일들이 중앙 기준 좌우로 벌어지는 것 시각 확인.

`leftWallOffsetPixels.x = -50` → 타일들이 중앙 기준으로 모이는 것 확인.  
극단값(`-286px 이하`) 설정 → 클램프되어 중앙에 겹치는 선 이상으로 가지 않음 확인.

---

## 알려진 제약

- `centerTilePosition` 또는 `offsetPixels` 값을 Inspector에서 변경하면 타일맵이 자동으로 갱신되지 않는다. 변경 후 Tilemap 컴포넌트 컨텍스트 메뉴 또는 스크립트에서 `tilemap.RefreshAllTiles()`를 수동 호출해야 한다.
- Picker는 Z=0 평면 기준 ray intersection을 사용한다. 카메라가 완전 수직인 경우(ray.direction.z == 0) 좌표 계산이 실패한다. Isometric view에서는 정상 동작한다.
