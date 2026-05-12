# IsoAutoWallTile Pivot Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `IsoAutoWallTile` place mirrored isometric wall sprites correctly on all four sides without requiring sprite import pivot hacks.

**Architecture:** Keep the existing neighbor-based side resolution and tint logic, but replace the raw mirror transform with a compensation-aware transform builder. The compensation will be derived from the delta between the sprite's current pivot and a nominal mirrored pivot target so existing assets and future `(0, 0)` pivot assets both remain usable.

**Tech Stack:** Unity 6.3, C#, `TileBase`, Unity Test Framework EditMode tests, Unity CLI batch test execution

---

### Task 1: Add EditMode regression coverage for mirrored wall transforms

**Files:**
- Create: `Assets/00. Work/CheolYee/02. Scripts/TileMaps/Editor/IsoAutoWallTileTests.cs`
- Modify: none
- Test: `Assets/00. Work/CheolYee/02. Scripts/TileMaps/Editor/IsoAutoWallTileTests.cs`

- [ ] **Step 1: Write the failing regression test for a `(0, 0)` pivot wall sprite**

```csharp
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using _00._Work.CheolYee._02._Scripts.TileMaps;

public class IsoAutoWallTileTests
{
    [Test]
    public void MirroredWall_WithBottomLeftPivot_AppliesMirrorCompensationTranslation()
    {
        var context = TilemapTestContext.Create();
        var tile = CreateTile(
            CreateSprite(new Vector2(0f, 0f)),
            WallSide.Right,
            Vector2.zero,
            true);

        context.Tilemap.SetTile(Vector3Int.zero, tile);
        context.Tilemap.SetTile(Vector3Int.right, tile);
        context.Tilemap.RefreshAllTiles();

        Matrix4x4 matrix = context.Tilemap.GetTransformMatrix(Vector3Int.zero);

        Assert.That(matrix.m00, Is.EqualTo(-1f).Within(0.0001f));
        Assert.That(matrix.m03, Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(matrix.m13, Is.EqualTo(-0.25f).Within(0.0001f));
    }
}
```

- [ ] **Step 2: Add the compatibility regression for already-offset sprite pivots**

```csharp
[Test]
public void MirroredWall_WithExistingUpperPivot_DoesNotDoubleCompensate()
{
    var context = TilemapTestContext.Create();
    var tile = CreateTile(
        CreateSprite(new Vector2(1f, 0.25f)),
        WallSide.Right,
        Vector2.zero,
        true);

    context.Tilemap.SetTile(Vector3Int.zero, tile);
    context.Tilemap.SetTile(Vector3Int.right, tile);
    context.Tilemap.RefreshAllTiles();

    Matrix4x4 matrix = context.Tilemap.GetTransformMatrix(Vector3Int.zero);

    Assert.That(matrix.m00, Is.EqualTo(-1f).Within(0.0001f));
    Assert.That(matrix.m03, Is.EqualTo(0f).Within(0.0001f));
    Assert.That(matrix.m13, Is.EqualTo(0f).Within(0.0001f));
}
```

- [ ] **Step 3: Add the unmirrored baseline regression**

```csharp
[Test]
public void NonMirroredWall_KeepsIdentityTransform()
{
    var context = TilemapTestContext.Create();
    var tile = CreateTile(
        CreateSprite(new Vector2(0f, 0f)),
        WallSide.Left,
        Vector2.zero,
        true);

    context.Tilemap.SetTile(Vector3Int.zero, tile);
    context.Tilemap.SetTile(Vector3Int.right, tile);
    context.Tilemap.RefreshAllTiles();

    Matrix4x4 matrix = context.Tilemap.GetTransformMatrix(Vector3Int.zero);

    Assert.That(matrix, Is.EqualTo(Matrix4x4.identity));
}
```

- [ ] **Step 4: Add minimal test helpers in the same file**

```csharp
private static IsoAutoWallTile CreateTile(
    Sprite sprite,
    WallSide xAxisWallSide,
    Vector2 compensationPixels,
    bool useCompensation)
{
    var tile = ScriptableObject.CreateInstance<IsoAutoWallTile>();
    SetField(tile, "sprite", sprite);
    SetField(tile, "xAxisWallSide", xAxisWallSide);
    SetField(tile, "useMirrorPivotCompensation", useCompensation);
    SetField(tile, "mirrorPivotCompensationPixels", compensationPixels == Vector2.zero
        ? new Vector2(256f, 128f)
        : compensationPixels);
    return tile;
}

private static Sprite CreateSprite(Vector2 normalizedPivot)
{
    var texture = new Texture2D(256, 512, TextureFormat.RGBA32, false);
    return Sprite.Create(
        texture,
        new Rect(0f, 0f, 256f, 512f),
        normalizedPivot,
        512f);
}

private static void SetField(object target, string fieldName, object value)
{
    FieldInfo field = target.GetType()
        .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
    field.SetValue(target, value);
}
```

- [ ] **Step 5: Run the test file to verify the first regression fails before production code changes**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe' `
  -batchmode `
  -projectPath 'C:\unityproject\Bar_Tycoon' `
  -runTests `
  -testPlatform EditMode `
  -testFilter IsoAutoWallTileTests `
  -testResults 'C:\unityproject\Bar_Tycoon\Temp\IsoAutoWallTileTests.xml' `
  -logFile 'C:\unityproject\Bar_Tycoon\Temp\IsoAutoWallTileTests.log' `
  -quit
```

Expected:
- FAIL
- failure on `MirroredWall_WithBottomLeftPivot_AppliesMirrorCompensationTranslation`
- matrix translation reported as `0, 0` instead of `0.5, -0.25`

- [ ] **Step 6: Commit the red test**

```bash
git add "Assets/00. Work/CheolYee/02. Scripts/TileMaps/Editor/IsoAutoWallTileTests.cs"
git commit -m "test: add IsoAutoWallTile mirror compensation regressions"
```

### Task 2: Replace the raw mirror matrix with compensation-aware transform building

**Files:**
- Modify: `Assets/00. Work/CheolYee/02. Scripts/TileMaps/IsoAutoWallTile.cs`
- Test: `Assets/00. Work/CheolYee/02. Scripts/TileMaps/Editor/IsoAutoWallTileTests.cs`

- [ ] **Step 1: Add serialized settings for compensation without breaking existing assets**

```csharp
[Header("Transform")]
[SerializeField] private bool useMirrorPivotCompensation = true;
[SerializeField] private Vector2 mirrorPivotCompensationPixels = new(256f, 128f);
```

- [ ] **Step 2: Replace the static raw flip-only matrix setup**

```csharp
private static readonly Matrix4x4 NormalMatrix = Matrix4x4.identity;

private static Matrix4x4 BuildMirrorMatrix(Vector2 compensationWorld)
{
    return Matrix4x4.TRS(
        new Vector3(compensationWorld.x, compensationWorld.y, 0f),
        Quaternion.identity,
        new Vector3(-1f, 1f, 1f));
}
```

- [ ] **Step 3: Route `GetTileData` through a focused transform builder**

```csharp
bool mirrorX = resolvedSide == WallSide.Right;
tileData.transform = BuildTransform(mirrorX);
```

```csharp
private Matrix4x4 BuildTransform(bool mirrorX)
{
    if (!mirrorX)
        return NormalMatrix;

    if (!useMirrorPivotCompensation || sprite == null)
        return BuildMirrorMatrix(Vector2.zero);

    Vector2 compensationWorld = GetMirrorCompensationWorld(sprite);
    return BuildMirrorMatrix(compensationWorld);
}
```

- [ ] **Step 4: Compute compensation from current sprite pivot to the nominal mirrored pivot**

```csharp
private Vector2 GetMirrorCompensationWorld(Sprite targetSprite)
{
    float pixelsPerUnit = GetSpritePixelsPerUnitSafe(targetSprite);
    Vector2 actualPivotPixels = targetSprite.pivot;
    Vector2 desiredPivotPixels = mirrorPivotCompensationPixels;

    float x = (desiredPivotPixels.x - actualPivotPixels.x) / pixelsPerUnit;
    float y = (actualPivotPixels.y - desiredPivotPixels.y) / pixelsPerUnit;
    return new Vector2(x, y);
}

private static float GetSpritePixelsPerUnitSafe(Sprite targetSprite)
{
    return targetSprite != null && targetSprite.pixelsPerUnit > 0f
        ? targetSprite.pixelsPerUnit
        : 100f;
}
```

- [ ] **Step 5: Keep side resolution and tint logic unchanged**

```csharp
WallSide resolvedSide = ResolveWallSide(hasXNeighbor, hasYNeighbor);
tileData.color = useTint
    ? (resolvedSide == WallSide.Left ? leftWallColor : rightWallColor)
    : Color.white;
```

- [ ] **Step 6: Run the targeted EditMode test file again**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe' `
  -batchmode `
  -projectPath 'C:\unityproject\Bar_Tycoon' `
  -runTests `
  -testPlatform EditMode `
  -testFilter IsoAutoWallTileTests `
  -testResults 'C:\unityproject\Bar_Tycoon\Temp\IsoAutoWallTileTests.xml' `
  -logFile 'C:\unityproject\Bar_Tycoon\Temp\IsoAutoWallTileTests.log' `
  -quit
```

Expected:
- PASS
- all `IsoAutoWallTileTests` green

- [ ] **Step 7: Commit the production fix**

```bash
git add "Assets/00. Work/CheolYee/02. Scripts/TileMaps/IsoAutoWallTile.cs" "Assets/00. Work/CheolYee/02. Scripts/TileMaps/Editor/IsoAutoWallTileTests.cs"
git commit -m "fix: compensate mirrored isometric wall pivot in tile transform"
```

### Task 3: Verify scene behavior in Unity without repainting content

**Files:**
- Modify: none expected
- Check: `Assets/00. Work/CheolYee/01. Scene/Main.unity`
- Check: `Assets/00. Work/CheolYee/08. Prefabs/Grid.prefab`

- [ ] **Step 1: Open or keep the `Main` scene active and inspect `Grid/BackWalls`**

Inspector targets:

```text
Scene: Assets/00. Work/CheolYee/01. Scene/Main.unity
GameObject: Grid/BackWalls
Checks: Tilemap anchor remains (0.5, 0.5, 0), painted cells unchanged
```

- [ ] **Step 2: Visually confirm all four wall runs align with no sprite pivot swapping**

Checklist:

```text
- lower-left to lower-right run still aligns
- upper-left to upper-right run now aligns
- left vertical run and right vertical run still match tint/facing rules
- no cell repainting needed
```

- [ ] **Step 3: Re-check an asset that currently carries a non-zero sprite pivot**

Targets:

```text
- Assets/00. Work/CheolYee/03. Sprites/Tiles/Walls/cafe_inside_wall_basic_01.png
- Assets/00. Work/CheolYee/03. Sprites/Tiles/Walls/InnerWalls/InsideWall_Default.png
```

Expected:

```text
- scene stays aligned even if old assets still carry historical pivot offsets
- future assets can be normalized to (0, 0) without changing the tile script again
```

- [ ] **Step 4: Capture evidence with Unity MCP scene-view screenshot**

Expected:

```text
- four-sided room wall remains continuous
- no obvious top-edge shift between mirrored and non-mirrored sides
```

- [ ] **Step 5: Commit only if no scene or prefab relink was required**

```bash
git status --short
```

Expected:

```text
- only script/test/plan/spec changes from this task
- if scene or prefab changed unexpectedly, inspect before committing
```
