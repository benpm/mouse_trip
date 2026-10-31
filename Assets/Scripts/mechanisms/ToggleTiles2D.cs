using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Swaps a set of cells between an outline tile (no collider) and a filled tile (collider).
public class ToggleTiles2D : MonoBehaviour, IActivationReceiver
{
    [Header("Tiles")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase outlineTile;
    [SerializeField] private TileBase filledTile;
    [Tooltip("Empty = every cell on the tilemap holding either tile")]
    [SerializeField] private Vector3Int[] cells;

    [Header("Mode")]
    [Tooltip("Off: flip on each activation. On: mirror the activator's state")]
    [SerializeField] private bool followActivation;

    private TilemapCollider2D tilemapCollider;
    private bool[] startFilled;
    private bool initialized;

    // True when every cell is inverted from its initial tile.
    public bool Toggled { get; private set; }

    private void Start() => Init();

    public void SetActivated(bool active)
    {
        if (followActivation) Set(active);
        else if (active) Toggle();
    }

    public void Toggle() => Set(!Toggled);

    public void Set(bool toggled)
    {
        Init();
        var tiles = new TileBase[cells.Length];
        for (int i = 0; i < cells.Length; i++)
            tiles[i] = startFilled[i] ^ toggled ? filledTile : outlineTile;
        tilemap.SetTiles(cells, tiles);
        if (tilemapCollider) tilemapCollider.ProcessTilemapChanges();
        Toggled = toggled;
    }

    private void Init()
    {
        if (initialized) return;
        initialized = true;
        if (!tilemap) tilemap = GetComponent<Tilemap>();
        tilemapCollider = tilemap.GetComponent<TilemapCollider2D>();
        if (cells == null || cells.Length == 0) cells = Scan();
        startFilled = new bool[cells.Length];
        for (int i = 0; i < cells.Length; i++) startFilled[i] = tilemap.GetTile(cells[i]) == filledTile;
    }

    private Vector3Int[] Scan()
    {
        var found = new List<Vector3Int>();
        foreach (var p in tilemap.cellBounds.allPositionsWithin)
        {
            var t = tilemap.GetTile(p);
            if (t == outlineTile || t == filledTile) found.Add(p);
        }
        return found.ToArray();
    }
}
