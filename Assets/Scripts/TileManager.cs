using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    public Tilemap tilemap;

    public TileTypes[] tileTypes;
    [Range(2, 6)] public int tileTypeRange;
    public int conditionA = 4, conditionB = 7, conditionC = 9;
    private Dictionary<TileBase, TileTypes> tileLookup;

    [Range(5, 10)] public int rows = 10;
    [Range(5, 12)] public int cols = 12;

    
    public class Group
    {
        public List<Vector3Int> positions;
        public TileBase originalTile;
        public int count;
    }

    bool deadlockFlag = true;

    void Start()
    {
        InitializeTileLookup();
        GenerateGrid();
        UpdateTileGroups();
    }

    public void InitializeTileLookup()
    {
        tileLookup = new Dictionary<TileBase, TileTypes>();

        for (int i = 0; i < tileTypeRange; i++)
        {
            tileLookup[tileTypes[i].defaultTile] = tileTypes[i];
            for (int j = 0; j < tileTypes[i].conditionalTiles.Length; j++)
            {
                tileLookup[tileTypes[i].conditionalTiles[j]] = tileTypes[i];
            }
        }
    }

    public void GenerateGrid()
    {
        tilemap.ClearAllTiles();

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                int randomTileIndex = UnityEngine.Random.Range(0, tileTypeRange);
                tilemap.SetTile(new Vector3Int(x, y, 0), tileTypes[randomTileIndex].defaultTile);
            }
        }
    }

    public void UpdateTileGroups()
    {
        List<Group> groups = FindAllGroups();

        // If there are not any group, there is a deadlock
        if (deadlockFlag)
        {
            UnityEngine.Debug.LogWarning("DEADLOCK");
            groups = FixDeadlock();
        }

        ApplyGroupUpdates(groups);
    }

    /// <summary>
    /// Change randomly tile color with near tile.
    /// </summary>
    /// <param name="changeTile">Total amount of tile will change.</param>
    /// <returns>Returns new group tiles</returns>
    private List<Group> FixDeadlock(int changeTile = 6)
    {
        for (int i = 0; i < changeTile; i++)
        {
            int randomX = UnityEngine.Random.Range(0, cols);
            int randomY = UnityEngine.Random.Range(0, rows);
            Vector3Int position = new Vector3Int(randomX, randomY, 0);

            TileBase currentTile = tilemap.GetTile(position);
            if (currentTile == null) continue;

            // Get a random neighbor tile
            Vector3Int[] neighbors = new Vector3Int[]
            {
                new Vector3Int(position.x + 1, position.y, 0),
                new Vector3Int(position.x - 1, position.y, 0),
                new Vector3Int(position.x, position.y + 1, 0),
                new Vector3Int(position.x, position.y - 1, 0)
            };

            TileBase newTile = null;
            for (int r = 0; r < neighbors.Length; r++)
            {
                int  rand = UnityEngine.Random.Range(0, neighbors.Length);
                newTile = tilemap.GetTile(neighbors[rand]);
                if (newTile != null)
                    break;
            }

            
            tilemap.SetTile(position, newTile);            
        }

        return FindAllGroups();
    }

    /// <summary>
    /// Finds the given square area. It must start with max rows and cols number when the game start.
    /// When the tile map has been changed. It should search only changed area.
    /// </summary>
    /// <returns>Returns all the same color groups</returns>
    private List<Group> FindAllGroups()
    {
        // Assume there is a deadlock
        deadlockFlag = true;

        bool[,] visited = new bool[cols, rows];
        List<Group> groups = new List<Group>();

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (!visited[x, y])
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    TileBase tile = tilemap.GetTile(pos);
                    if (tile == null) continue;

                    Group group = new Group
                    {
                        positions = new List<Vector3Int>(),
                        originalTile = tile,
                        count = 0
                    };

                    Queue<Vector3Int> queue = new Queue<Vector3Int>();
                    queue.Enqueue(pos);
                    visited[x, y] = true;

                    // Iterate for all neighbor tiles and add position values to group
                    while (queue.Count > 0)
                    {
                        Vector3Int current = queue.Dequeue();
                        group.positions.Add(current);
                        group.count++;

                        CheckNeighbor(current.x + 1, current.y, tile, visited, queue);
                        CheckNeighbor(current.x - 1, current.y, tile, visited, queue);
                        CheckNeighbor(current.x, current.y + 1, tile, visited, queue);
                        CheckNeighbor(current.x, current.y - 1, tile, visited, queue);
                    }

                    // Check if there is a group that has count more than 1 than there is no deadlock
                    if (group.count > 1) deadlockFlag = false;

                    groups.Add(group);
                }
            }
        }

        return groups;
    }

    private void CheckNeighbor(int x, int y, TileBase targetTile, bool[,] visited, Queue<Vector3Int> queue)
    {
        if (x >= 0 && x < cols && y >= 0 && y < rows && !visited[x, y])
        {
            Vector3Int neighborPos = new Vector3Int(x, y, 0);
            TileBase neighborTile = tilemap.GetTile(neighborPos);
            if (AreTilesSameType(targetTile, neighborTile))
            {
                visited[x, y] = true;
                queue.Enqueue(neighborPos);
            }
        }
    }

    bool AreTilesSameType(TileBase tile1, TileBase tile2)
    {
        if (tile1 == null || tile2 == null) return false;

        return tileLookup.ContainsKey(tile1) && tileLookup.ContainsKey(tile2) &&
               tileLookup[tile1] == tileLookup[tile2];
    }

    private void ApplyGroupUpdates(List<Group> groups)
    {
        List<Vector3Int> positions = new List<Vector3Int>();
        List<TileBase> tiles = new List<TileBase>();

        foreach (Group group in groups)
        {
            //if(group.count <= 2) continue; // Doesn't need to change tile if the group count is lower than 2.

            TileBase newTile = DetermineTileBasedOnCount(group.originalTile, group.count);
            foreach (Vector3Int pos in group.positions)
            {
                positions.Add(pos);
                tiles.Add(newTile);
            }
        }

        tilemap.SetTiles(positions.ToArray(), tiles.ToArray());
    }

    private TileBase DetermineTileBasedOnCount(TileBase originalTile, int count)
    {
        foreach (TileTypes tileType in tileTypes)
        {
            if (tileType.defaultTile == originalTile || tileType.conditionalTiles.Contains(originalTile))
            {
                if (count >= conditionC) return tileType.conditionalTiles[0];
                if (count >= conditionB) return tileType.conditionalTiles[1];
                if (count >= conditionA) return tileType.conditionalTiles[2];
                return tileType.defaultTile;
            }
        }

        return originalTile; // Fallback
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPosition = tilemap.WorldToCell(worldPoint);

            if (tilemap.HasTile(cellPosition))
            {
                UnityEngine.Debug.Log(tilemap.GetTile(cellPosition));
                RemoveMatchingTiles(cellPosition);
            }
        }
    }
    void RemoveMatchingTiles(Vector3Int startPosition)
    {
        TileBase startTile = tilemap.GetTile(startPosition);
        if (startTile == null) return;

        Queue<Vector3Int> toCheck = new Queue<Vector3Int>();
        HashSet<Vector3Int> toRemove = new HashSet<Vector3Int>();

        // Initialize with clicked tile
        toCheck.Enqueue(startPosition);

        while (toCheck.Count > 0)
        {
            // TO DO ONLY REMOVE AT LEAST TWO SAME COLOR
            Vector3Int current = toCheck.Dequeue();
            if (toRemove.Contains(current)) continue;

            TileBase tile = tilemap.GetTile(current);

            if (tile == startTile)
            {
                toRemove.Add(current);
                toCheck.Enqueue(current + Vector3Int.right);
                toCheck.Enqueue(current + Vector3Int.left);
                toCheck.Enqueue(current + Vector3Int.up);
                toCheck.Enqueue(current + Vector3Int.down);
            }
        }

        // Only remove groups of 2+ tiles
        if (toRemove.Count < 2) return;

        Vector3Int[] positions = new Vector3Int[toRemove.Count];
        TileBase[] emptyTiles = new TileBase[toRemove.Count];
        int i = 0;
        foreach (Vector3Int pos in toRemove)
        {
            positions[i] = pos;
            emptyTiles[i] = null;
            i++;
        }
        tilemap.SetTiles(positions, emptyTiles);

        // Apply Gravity and Spawn New Tiles
        ApplyGravity(toRemove);
        SpawnNewTiles(toRemove);

        UpdateTileGroups();
    }

    void ApplyGravity(HashSet<Vector3Int> removedTiles)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start(); // Start timing

        // Check only affected columns and apply gravity
        foreach (Vector3Int rTiles in removedTiles)
        {
            for (int y = rTiles.y; y < rows; y++)
            {
                Vector3Int currentPos = new Vector3Int(rTiles.x, y, 0);

                if (tilemap.GetTile(currentPos) == null)
                {
                    // Start with upper tile
                    for (int aboveY = y + 1; aboveY < rows; aboveY++)
                    {
                        Vector3Int abovePos = new Vector3Int(rTiles.x, aboveY, 0);
                        TileBase aboveTile = tilemap.GetTile(abovePos);

                        if (aboveTile != null)
                        {
                            // Move tile down
                            tilemap.SetTile(currentPos, aboveTile);
                            tilemap.SetTile(abovePos, null);
                            break;
                        }
                    }
                }
            }
        }

        stopwatch.Stop(); // End timing
        UnityEngine.Debug.Log($"ApplyGravity Execution Ticks: {stopwatch.ElapsedTicks}");
    }

    void SpawnNewTiles(HashSet<Vector3Int> removedTiles)
    {
        foreach (Vector3Int rTile in removedTiles)
        {
            for (int y = rTile.y; y < rows; y++)
            {
                Vector3Int position = new Vector3Int(rTile.x, y, 0);

                if (tilemap.GetTile(position) == null) // If empty space exists
                {
                    int randomTileIndex = UnityEngine.Random.Range(0, tileTypeRange);
                    tilemap.SetTile(position, tileTypes[randomTileIndex].defaultTile); // Spawn a new tile
                }
            }
        }
    }

}


