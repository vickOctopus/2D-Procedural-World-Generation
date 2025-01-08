using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;

public class WorldGenerator : MonoBehaviour
{
    [Header("World Settings")]
    [SerializeField] private int worldWidth = 100;
    [SerializeField] private int worldHeight = 100;

    
    [Header("Generation Settings")]
    [SerializeField] private int seed = 0;
    [SerializeField, Range(0.01f, 0.1f)] private float noiseScale = 0.05f;
    [SerializeField, Range(0.3f, 0.7f)] private float fillPercentage = 0.45f;
    [SerializeField, Range(1, 10)] private int smoothIterations = 5;
    
    [Header("References")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase wallTile;
    
    private int[,] tiles; // 0 = 空气, 1 = 墙壁
    private AreaTemplate spawnAreaTemplate;
    private bool[,] protectedTiles; // 标记必须保持的区域
    
    private void Start()
    {
        LoadSpawnAreaTemplate();
        GenerateWorld();
    }
    
    private void LoadSpawnAreaTemplate()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Templates/spawn_area");
        if (jsonFile != null)
        {
            spawnAreaTemplate = JsonUtility.FromJson<AreaTemplate>(jsonFile.text);
        }
        else
        {
            Debug.LogError("Failed to load spawn area template!");
        }
    }
    
    private void GenerateWorld()
    {
        tiles = new int[worldWidth, worldHeight];
        protectedTiles = new bool[worldWidth, worldHeight];
        
        // 第一步：使用柏林噪声生成基本地形
        GenerateBaseNoise();
        
        // 第二步：生成出生点区域
        GenerateSpawnArea();
        
        // 第三步：使用元胞自动机平滑地形
        SmoothTerrain();
        
        // 第四步：在Tilemap上绘制瓦片
        DrawTilemap();
    }
    
    private void GenerateSpawnArea()
    {
        if (spawnAreaTemplate == null) return;
        
        // 直接从layout获取尺寸
        int width = spawnAreaTemplate.layout[0].Length;
        int height = spawnAreaTemplate.layout.Length;
        
        int startX = worldWidth / 2 - width / 2;
        int startY = (int)(worldHeight * 0.7f);
        
        // 应用模板
        ApplyTemplate(startX, startY);
    }
    
    private void GenerateBaseNoise()
    {
        if (seed == 0)
        {
            seed = Random.Range(1, int.MaxValue);
        }
        
        System.Random prng = new System.Random(seed);
        
        float offsetX = (float)prng.NextDouble() * 1000f;
        float offsetY = (float)prng.NextDouble() * 1000f;
        
        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                float noiseValue = Mathf.PerlinNoise(
                    (x + offsetX) * noiseScale, 
                    (y + offsetY) * noiseScale
                );
                
                tiles[x, y] = noiseValue < fillPercentage ? 1 : 0;
            }
        }
    }
    
    private void SmoothTerrain()
    {
        for (int i = 0; i < smoothIterations; i++)
        {
            int[,] newTiles = new int[worldWidth, worldHeight];
            
            for (int x = 0; x < worldWidth; x++)
            {
                for (int y = 0; y < worldHeight; y++)
                {
                    if (protectedTiles[x, y])
                    {
                        newTiles[x, y] = tiles[x, y]; // 保护必须保持的区域
                        continue;
                    }
                    
                    // 计算周围8个格子的墙壁数量
                    int wallCount = GetSurroundingWallCount(x, y);
                    
                    // 应用元胞自动机规则
                    if (wallCount > 4)
                        newTiles[x, y] = 1;
                    else if (wallCount < 4)
                        newTiles[x, y] = 0;
                    else
                        newTiles[x, y] = tiles[x, y];
                }
            }
            
            tiles = newTiles;
        }
    }
    
    private int GetSurroundingWallCount(int x, int y)
    {
        int wallCount = 0;
        
        for (int neighborX = x - 1; neighborX <= x + 1; neighborX++)
        {
            for (int neighborY = y - 1; neighborY <= y + 1; neighborY++)
            {
                if (neighborX == x && neighborY == y)
                    continue;
                    
                if (IsOutOfBounds(neighborX, neighborY))
                    wallCount++;
                else if (tiles[neighborX, neighborY] == 1)
                    wallCount++;
            }
        }
        
        return wallCount;
    }
    
    private bool IsOutOfBounds(int x, int y)
    {
        return x < 0 || x >= worldWidth || y < 0 || y >= worldHeight;
    }
    
    private void DrawTilemap()
    {
        // 清除现有的瓦片
        tilemap.ClearAllTiles();
        
        // 绘制新的瓦片
        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                if (tiles[x, y] == 1)
                {
                    Vector3Int position = new Vector3Int(x - worldWidth/2, y - worldHeight/2, 0);
                    tilemap.SetTile(position, wallTile);
                }
            }
        }
    }
    
    private void ApplyTemplate(int startX, int startY)
    {
        // 从下到上读取layout
        for (int y = 0; y < spawnAreaTemplate.layout.Length; y++)
        {
            // 反转y轴读取顺序
            string row = spawnAreaTemplate.layout[spawnAreaTemplate.layout.Length - 1 - y];
            for (int x = 0; x < row.Length; x++)
            {
                int worldX = startX + x;
                int worldY = startY + y;
                
                if (IsOutOfBounds(worldX, worldY)) continue;
                
                char tile = row[x];
                switch (tile)
                {
                    case '#':
                        tiles[worldX, worldY] = 1; // 普通墙
                        break;
                    case '=':
                        tiles[worldX, worldY] = 1; // 必须的墙
                        protectedTiles[worldX, worldY] = true;
                        break;
                    case '.':
                        tiles[worldX, worldY] = 0; // 必须的空气
                        protectedTiles[worldX, worldY] = true;
                        break;
                    case 'P':
                        tiles[worldX, worldY] = 0; // 空气
                        protectedTiles[worldX, worldY] = true;
                        Vector3 spawnPosition = new Vector3(
                            worldX - worldWidth/2,
                            worldY - worldHeight/2,
                            0
                        );
                        PlayerController.instance.transform.position = spawnPosition;
                        break;
                }
            }
        }
    }
} 