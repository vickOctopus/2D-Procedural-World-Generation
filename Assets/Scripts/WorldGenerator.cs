using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;
using System.Collections.Generic;

public class WorldGenerator : MonoBehaviour
{
    [Header("World Settings")]
    [SerializeField] private int worldWidth = 100;
    [SerializeField] private int worldHeight = 100;

    
    [Header("Generation Settings")]
    [SerializeField] private int seed = 0;
    [SerializeField] private int outpostCount = 3;  // 据点数量
    [SerializeField, Range(0.01f, 0.1f)] private float noiseScale = 0.05f;
    [SerializeField, Range(0.3f, 0.7f)] private float fillPercentage = 0.45f;
    [SerializeField, Range(1, 10)] private int smoothIterations = 5;
    
    [Header("References")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase wallTile;
    [SerializeField] private GameObject outpostPrefab;
    
    private int[,] tiles; // 0 = 空气, 1 = 墙壁
    private AreaTemplate spawnAreaTemplate;
    private AreaTemplate outpostAreaTemplate;
    private bool[,] protectedTiles; // 标记必须保持的区域
    private List<Vector2> outpostPositions = new List<Vector2>();  // 记录所有据点位置（包括出生点）
    
    private float minOutpostDistance;  // 缓存计算结果
    
    private void Start()
    {
        LoadSpawnAreaTemplate();
        InitializeMinDistance();
        GenerateWorld();
    }
    
    private void InitializeMinDistance()
    {
        float worldDiagonal = Mathf.Sqrt(worldWidth * worldWidth + worldHeight * worldHeight);
        float idealSpacing = worldDiagonal / Mathf.Sqrt(outpostCount + 1);
        minOutpostDistance = idealSpacing * 0.5f;
    }
    
    private void LoadSpawnAreaTemplate()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Templates/spawn_area");
        TextAsset outpostFile = Resources.Load<TextAsset>("Templates/outpost_area");
        
        if (jsonFile != null)
        {
            spawnAreaTemplate = JsonUtility.FromJson<AreaTemplate>(jsonFile.text);
        }
        
        if (outpostFile != null)
        {
            outpostAreaTemplate = JsonUtility.FromJson<AreaTemplate>(outpostFile.text);
        }
    }
    
    private void GenerateWorld()
    {
        tiles = new int[worldWidth, worldHeight];
        protectedTiles = new bool[worldWidth, worldHeight];
        outpostPositions.Clear();
        
        GenerateBaseNoise();    // 基础噪声
        GenerateAreas();        // 特殊区域
        SmoothTerrain();        // 平滑地形
        DrawTilemap();          // 绘制地图
    }
    
    private void GenerateAreas()
    {
        System.Random prng = new System.Random(seed);
        
        // 生成出生点
        GenerateRandomArea(spawnAreaTemplate, prng);
        
        // 生成据点
        for (int i = 0; i < outpostCount; i++)
        {
            GenerateRandomArea(outpostAreaTemplate, prng);
        }
    }
    
    private void GenerateRandomArea(AreaTemplate template, System.Random prng)
    {
        int width = template.layout[0].Length;
        int height = template.layout.Length;
        int attempts = 0;
        int maxAttempts = 100;
        
        while (attempts < maxAttempts)
        {
            int startX = prng.Next(width, worldWidth - width);
            int startY = prng.Next(height, worldHeight - height);
            Vector2 newPos = new Vector2(startX + width/2, startY + height/2);
            
            // 检查与其他区域的距离
            bool tooClose = false;
            foreach (var pos in outpostPositions)
            {
                if (Vector2.Distance(newPos, pos) < minOutpostDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            
            if (!tooClose)
            {
                ApplyAreaTemplate(template, startX, startY);  // 直接调用ApplyAreaTemplate
                outpostPositions.Add(newPos);
                break;
            }
            
            attempts++;
        }
    }
    
    private void ApplyAreaTemplate(AreaTemplate template, int startX, int startY)
    {
        for (int y = 0; y < template.layout.Length; y++)
        {
            string row = template.layout[template.layout.Length - 1 - y];
            for (int x = 0; x < row.Length; x++)
            {
                int worldX = startX + x;
                int worldY = startY + y;
                
                if (IsOutOfBounds(worldX, worldY)) continue;
                
                char tile = row[x];
                Vector3 position = new Vector3(
                    worldX - worldWidth/2,
                    worldY - worldHeight/2,
                    0
                );
                
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
                        PlayerController.instance.transform.position = position;
                        break;
                    case 'O':
                        tiles[worldX, worldY] = 0; // 空气
                        protectedTiles[worldX, worldY] = true;
                        Instantiate(outpostPrefab, position, Quaternion.identity);
                        break;
                }
            }
        }
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
} 