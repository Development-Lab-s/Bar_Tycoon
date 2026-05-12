using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
using _00._Work.CheolYee._02._Scripts.TileMaps;

public class IsoAutoWallTileTests
{
    private readonly List<Object> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            Object obj = createdObjects[i];
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void MirroredWall_WithBottomLeftPivot_AppliesMirrorCompensationTranslation()
    {
        Tilemap tilemap = CreateTilemap();
        IsoAutoWallTile tile = CreateTile(CreateSprite(new Vector2(0f, 0f)), WallSide.Right);

        tilemap.SetTile(Vector3Int.zero, tile);
        tilemap.SetTile(Vector3Int.right, tile);
        tilemap.RefreshAllTiles();

        Matrix4x4 matrix = tilemap.GetTransformMatrix(Vector3Int.zero);

        Assert.That(matrix.m00, Is.EqualTo(-1f).Within(0.0001f));
        Assert.That(matrix.m03, Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(matrix.m13, Is.EqualTo(-0.25f).Within(0.0001f));
    }

    [Test]
    public void MirroredWall_WithExistingUpperPivot_DoesNotDoubleCompensate()
    {
        Tilemap tilemap = CreateTilemap();
        IsoAutoWallTile tile = CreateTile(CreateSprite(new Vector2(1f, 0.25f)), WallSide.Right);

        tilemap.SetTile(Vector3Int.zero, tile);
        tilemap.SetTile(Vector3Int.right, tile);
        tilemap.RefreshAllTiles();

        Matrix4x4 matrix = tilemap.GetTransformMatrix(Vector3Int.zero);

        Assert.That(matrix.m00, Is.EqualTo(-1f).Within(0.0001f));
        Assert.That(matrix.m03, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(matrix.m13, Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void NonMirroredWall_KeepsIdentityTransform()
    {
        Tilemap tilemap = CreateTilemap();
        IsoAutoWallTile tile = CreateTile(
            CreateSprite(new Vector2(0f, 0f)),
            WallSide.Left,
            Vector2.zero,
            new Vector2(256f, 128f));

        tilemap.SetTile(Vector3Int.zero, tile);
        tilemap.SetTile(Vector3Int.right, tile);
        tilemap.RefreshAllTiles();

        Matrix4x4 matrix = tilemap.GetTransformMatrix(Vector3Int.zero);

        Assert.That(matrix.m00, Is.EqualTo(1f).Within(0.0001f));
        Assert.That(matrix.m11, Is.EqualTo(1f).Within(0.0001f));
        Assert.That(matrix.m03, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(matrix.m13, Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void NonMirroredWall_WithPlacementPivotOffset_AppliesBaseTranslation()
    {
        Tilemap tilemap = CreateTilemap();
        IsoAutoWallTile tile = CreateTile(
            CreateSprite(new Vector2(0f, 0f)),
            WallSide.Left,
            new Vector2(256f, 128f),
            new Vector2(256f, 128f));

        tilemap.SetTile(Vector3Int.zero, tile);
        tilemap.SetTile(Vector3Int.right, tile);
        tilemap.RefreshAllTiles();

        Matrix4x4 matrix = tilemap.GetTransformMatrix(Vector3Int.zero);

        Assert.That(matrix.m00, Is.EqualTo(1f).Within(0.0001f));
        Assert.That(matrix.m03, Is.EqualTo(-0.5f).Within(0.0001f));
        Assert.That(matrix.m13, Is.EqualTo(-0.25f).Within(0.0001f));
    }

    private Tilemap CreateTilemap()
    {
        GameObject gridObject = new("TestGrid");
        gridObject.hideFlags = HideFlags.HideAndDontSave;
        Register(gridObject);

        Grid grid = gridObject.AddComponent<Grid>();
        grid.cellLayout = GridLayout.CellLayout.Isometric;
        grid.cellSize = new Vector3(1f, 0.5f, 1f);

        GameObject tilemapObject = new("TestTilemap");
        tilemapObject.hideFlags = HideFlags.HideAndDontSave;
        Register(tilemapObject);
        tilemapObject.transform.SetParent(gridObject.transform, false);

        Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
        tilemapObject.AddComponent<TilemapRenderer>();
        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
        return tilemap;
    }

    private IsoAutoWallTile CreateTile(
        Sprite sprite,
        WallSide xAxisWallSide,
        Vector2 defaultPlacementPivotPixels = default,
        Vector2 mirroredPlacementPivotPixels = default)
    {
        IsoAutoWallTile tile = ScriptableObject.CreateInstance<IsoAutoWallTile>();
        tile.hideFlags = HideFlags.HideAndDontSave;
        Register(tile);

        SetRequiredField(tile, "sprite", sprite);
        SetRequiredField(tile, "xAxisWallSide", xAxisWallSide);
        SetOptionalField(tile, "defaultPlacementPivotPixels", defaultPlacementPivotPixels);
        SetOptionalField(tile, "mirroredPlacementPivotPixels", mirroredPlacementPivotPixels == default
            ? new Vector2(256f, 128f)
            : mirroredPlacementPivotPixels);
        SetOptionalField(tile, "useMirrorPivotCompensation", true);
        SetOptionalField(tile, "mirrorPivotCompensationPixels", mirroredPlacementPivotPixels == default
            ? new Vector2(256f, 128f)
            : mirroredPlacementPivotPixels);
        return tile;
    }

    private Sprite CreateSprite(Vector2 normalizedPivot)
    {
        Texture2D texture = new(256, 512, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        Register(texture);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 256f, 512f),
            normalizedPivot,
            512f,
            0u,
            SpriteMeshType.FullRect);

        sprite.hideFlags = HideFlags.HideAndDontSave;
        Register(sprite);
        return sprite;
    }

    private void SetRequiredField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' to exist.");
        field.SetValue(target, value);
    }

    private void SetOptionalField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }

    private void Register(Object obj)
    {
        createdObjects.Add(obj);
    }
}
