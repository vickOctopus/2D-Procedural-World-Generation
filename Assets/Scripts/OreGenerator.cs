using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class OreGenerator
{
    private int worldWidth;
    private int worldHeight;
    private TileType[,] tiles;
    private OreType[,] oreTypes;
    private bool[,] protectedTiles;
    private System.Random random;
    
    public OreGenerator(int width, int height, TileType[,] tiles, OreType[,] oreTypes, bool[,] protectedTiles, int seed)
    {
        this.worldWidth = width;
        this.worldHeight = height;
        this.tiles = tiles;
        this.oreTypes = oreTypes;
        this.protectedTiles = protectedTiles;
        this.random = new System.Random(seed);
    }
    
    public void GenerateOres(List<OreCluster> oreClusters)
    {
        List<Vector2Int> allOrePositions = new List<Vector2Int>();
        
        foreach (var cluster in oreClusters)
        {
            GenerateOreCluster(cluster, allOrePositions);
        }
    }
    
    private void GenerateOreCluster(OreCluster cluster, List<Vector2Int> allOrePositions)
    {
        // 第一阶段：放置种子点
        List<Vector2Int> seeds = PlaceInitialSeeds(cluster, allOrePositions);
        if (seeds.Count == 0) return;
        
        // 第二阶段：为每个种子点生成矿脉
        foreach (var seed in seeds)
        {
            int targetSize = Random.Range(cluster.GetMinVeinSize(), cluster.GetMaxVeinSize() + 1);
            HashSet<Vector2Int> veinOres = new HashSet<Vector2Int> { seed };
            Queue<Vector2Int> expansionQueue = new Queue<Vector2Int>();
            expansionQueue.Enqueue(seed);
            
            while (veinOres.Count < targetSize && expansionQueue.Count > 0)
            {
                Vector2Int currentOre = expansionQueue.Dequeue();
                
                // 检查周围八个方向
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        
                        int newX = currentOre.x + dx;
                        int newY = currentOre.y + dy;
                        Vector2Int newPos = new Vector2Int(newX, newY);
                        
                        // 检查是否在矿脉半径范围内
                        if (Vector2.Distance(new Vector2(seed.x, seed.y), new Vector2(newX, newY)) > cluster.GetVeinRadius())
                            continue;
                            
                        if (!IsValidOrePosition(newX, newY) || veinOres.Contains(newPos))
                            continue;
                            
                        // 检查是否至少有一个相邻矿石
                        if (CountOreNeighbors(newX, newY, cluster.oreType) > 0)
                        {
                            SetOreTile(newX, newY, cluster.oreType);
                            veinOres.Add(newPos);
                            allOrePositions.Add(newPos);
                            expansionQueue.Enqueue(newPos);
                            
                            if (veinOres.Count >= targetSize)
                                break;
                        }
                    }
                    if (veinOres.Count >= targetSize)
                        break;
                }
            }
        }
    }
    
    private List<Vector2Int> PlaceInitialSeeds(OreCluster cluster, List<Vector2Int> allOrePositions)
    {
        List<Vector2Int> seeds = new List<Vector2Int>();
        int attempts = 0;
        int maxAttempts = 100;
        
        while (seeds.Count < cluster.veinCount && attempts < maxAttempts)
        {
            int x = random.Next(0, worldWidth);
            int y = random.Next(0, worldHeight);
            Vector2Int newSeed = new Vector2Int(x, y);
            
            if (IsValidOrePosition(x, y) && !IsTooCloseToOtherSeeds(newSeed, seeds, cluster.veinDistance))
            {
                SetOreTile(x, y, cluster.oreType);
                seeds.Add(newSeed);
                allOrePositions.Add(newSeed);
            }
            
            attempts++;
        }
        
        return seeds;
    }
    
    private bool IsTooCloseToOtherSeeds(Vector2Int pos, List<Vector2Int> seeds, float minDistance)
    {
        foreach (var seed in seeds)
        {
            float distance = Vector2.Distance(new Vector2(pos.x, pos.y), new Vector2(seed.x, seed.y));
            if (distance < minDistance)
                return true;
        }
        return false;
    }
    

    
    private int CountOreNeighbors(int x, int y, OreType oreType)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                
                int checkX = x + dx;
                int checkY = y + dy;
                
                if (IsOutOfBounds(checkX, checkY)) continue;
                
                if (tiles[checkX, checkY] == TileType.Ore && oreTypes[checkX, checkY] == oreType)
                {
                    count++;
                }
            }
        }
        return count;
    }
    
    private bool IsValidOrePosition(int x, int y)
    {
        if (IsOutOfBounds(x, y)) return false;
        if (protectedTiles[x, y]) return false;
        if (tiles[x, y] != TileType.Wall) return false;
        return true;
    }
    
    private bool IsOutOfBounds(int x, int y)
    {
        return x < 0 || x >= worldWidth || y < 0 || y >= worldHeight;
    }
    
    private void SetOreTile(int x, int y, OreType oreType)
    {
        tiles[x, y] = TileType.Ore;
        oreTypes[x, y] = oreType;
    }
} 