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
        Tilemap tilemap = CreateTilemap();
        IsoWallTile tile = CreateTile(xAxisWallSide: WallSide.Left);
        SetField(tile, "leftWallOffsetPixels", Vector2.zero);
        SetField(tile, "rightWallOffsetPixels", Vector2.zero);

        tilemap.SetTile(Vector3Int.zero, tile);
        tilemap.SetTile(Vector3Int.right, tile);
        tilemap.RefreshAllTiles();

        Matrix4x4 m = tilemap.GetTransformMatrix(Vector3Int.zero);
        Assert.That(m.m00, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void RightWall_XNeighbor_IsMirrored()
    {
        Tilemap tilemap = CreateTilemap();
        IsoWallTile tile = CreateTile(xAxisWallSide: WallSide.Right);
        SetField(tile, "leftWallOffsetPixels", Vector2.zero);
        SetField(tile, "rightWallOffsetPixels", Vector2.zero);

        tilemap.SetTile(Vector3Int.zero, tile);
        tilemap.SetTile(Vector3Int.right, tile);
        tilemap.RefreshAllTiles();

        Matrix4x4 m = tilemap.GetTransformMatrix(Vector3Int.zero);
        Assert.That(m.m00, Is.EqualTo(-1f).Within(0.001f));
    }

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

        Matrix4x4 m = tilemap.GetTransformMatrix(Vector3Int.zero);
        Assert.That(m.m03, Is.EqualTo(0f).Within(0.001f));
        Assert.That(m.m13, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void TileAtDeltaX1_WithLeftOffsetX512_HasExpectedWorldOffset()
    {
        Tilemap tilemap = CreateTilemap();
        IsoWallTile tile = CreateTileWithOffset(
            center: Vector3Int.zero,
            leftOffset: new Vector2(512f, 0f));

        tilemap.SetTile(new Vector3Int(1, 0, 0), tile);
        tilemap.SetTile(new Vector3Int(2, 0, 0), tile);
        tilemap.RefreshAllTiles();

        Matrix4x4 m = tilemap.GetTransformMatrix(new Vector3Int(1, 0, 0));
        Assert.That(m.m03, Is.EqualTo(Mathf.Cos(26.565f * Mathf.Deg2Rad)).Within(0.005f));
        Assert.That(m.m13, Is.EqualTo(-Mathf.Sin(26.565f * Mathf.Deg2Rad)).Within(0.005f));
    }

    [Test]
    public void TileAtDeltaY1_WithRightOffsetY512_HasExpectedWorldOffset()
    {
        // Y-only neighbor → resolves to Right wall (Opposite of Left)
        // rightOffsetY=512px, delta=(0,1)
        // oy = 1.0, ox = 0
        // worldX = delta.y * oy * (-cosA) = 1 * 1.0 * (-cos(26.565°)) ≈ -0.894
        // worldY = delta.y * oy * (-sinA) = 1 * 1.0 * (-sin(26.565°)) ≈ -0.447
        Tilemap tilemap = CreateTilemap();
        IsoWallTile tile = CreateTile(WallSide.Left); // xAxisWallSide=Left → Y-neighbor → Right wall
        SetField(tile, "centerTilePosition", Vector3Int.zero);
        SetField(tile, "leftWallOffsetPixels", Vector2.zero);
        SetField(tile, "rightWallOffsetPixels", new Vector2(0f, 512f)); // Y offset on Right wall

        tilemap.SetTile(new Vector3Int(0, 1, 0), tile);
        tilemap.SetTile(new Vector3Int(0, 2, 0), tile);
        tilemap.RefreshAllTiles();

        Matrix4x4 m = tilemap.GetTransformMatrix(new Vector3Int(0, 1, 0));
        // Right wall → mirror → m00 = -1
        Assert.That(m.m00, Is.EqualTo(-1f).Within(0.001f));
        // worldX = 1 * (512/512) * (-cos(26.565°)) = -cos(26.565°)
        Assert.That(m.m03, Is.EqualTo(-Mathf.Cos(26.565f * Mathf.Deg2Rad)).Within(0.005f));
        // worldY = 1 * (512/512) * (-sin(26.565°)) = -sin(26.565°)
        Assert.That(m.m13, Is.EqualTo(-Mathf.Sin(26.565f * Mathf.Deg2Rad)).Within(0.005f));
    }

    [Test]
    public void NegativeOffset_ClampedAtCenter_DoesNotCrossCenter()
    {
        float naturalStep = Mathf.Sqrt(0.5f * 0.5f + 0.25f * 0.25f);
        float minPx = -naturalStep * 512f;

        Tilemap tilemap = CreateTilemap();
        IsoWallTile tile = CreateTileWithOffset(
            center: Vector3Int.zero,
            leftOffset: new Vector2(minPx, 0f));

        tilemap.SetTile(new Vector3Int(1, 0, 0), tile);
        tilemap.SetTile(new Vector3Int(2, 0, 0), tile);
        tilemap.RefreshAllTiles();

        Matrix4x4 m = tilemap.GetTransformMatrix(new Vector3Int(1, 0, 0));
        float expectedX = -naturalStep * Mathf.Cos(26.565f * Mathf.Deg2Rad);
        Assert.That(m.m03, Is.EqualTo(expectedX).Within(0.005f));
    }

    [Test]
    public void OffsetBeyondMin_ClampedToMin()
    {
        float naturalStep = Mathf.Sqrt(0.5f * 0.5f + 0.25f * 0.25f);
        float minPx = -naturalStep * 512f;
        float beyondMin = minPx - 100f;

        Tilemap tilemap = CreateTilemap();
        IsoWallTile tile = CreateTileWithOffset(center: Vector3Int.zero, leftOffset: new Vector2(beyondMin, 0f));
        IsoWallTile tileSameMin = CreateTileWithOffset(center: Vector3Int.zero, leftOffset: new Vector2(minPx, 0f));

        tilemap.SetTile(new Vector3Int(1, 0, 0), tile);
        tilemap.SetTile(new Vector3Int(2, 0, 0), tile);
        tilemap.RefreshAllTiles();

        GameObject go2 = new("G2") { hideFlags = HideFlags.HideAndDontSave };
        _created.Add(go2);
        Grid grid2 = go2.AddComponent<Grid>();
        grid2.cellLayout = GridLayout.CellLayout.Isometric;
        grid2.cellSize = new Vector3(1f, 0.5f, 1f);
        GameObject to2 = new("T2") { hideFlags = HideFlags.HideAndDontSave };
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
        Assert.That(m.m00, Is.EqualTo(-1f).Within(0.001f));
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
        SetField(tile, "useMirrorPivotCompensation", false);
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
