using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldBoundaryGenerator
{
    private readonly int worldWidth;
    private readonly int worldHeight;
    private readonly int boundaryWidth;
    private readonly Tilemap tilemap;
    private readonly TileBase boundaryTile;
    
    public WorldBoundaryGenerator(
        int worldWidth,
        int worldHeight,
        int boundaryWidth,
        Tilemap tilemap,
        TileBase boundaryTile)
    {
        this.worldWidth = worldWidth;
        this.worldHeight = worldHeight;
        this.boundaryWidth = boundaryWidth;
        this.tilemap = tilemap;
        this.boundaryTile = boundaryTile;
    }
    
    public void GenerateBoundary()
    {
        // 生成上下边界（在世界范围外）
        for (int x = -boundaryWidth; x < worldWidth + boundaryWidth; x++)
        {
            // 底部边界
            for (int y = -boundaryWidth; y < 0; y++)
            {
                Vector3Int position = new Vector3Int(x - worldWidth/2, y - worldHeight/2, 0);
                tilemap.SetTile(position, boundaryTile);
            }
            
            // 顶部边界
            for (int y = worldHeight; y < worldHeight + boundaryWidth; y++)
            {
                Vector3Int position = new Vector3Int(x - worldWidth/2, y - worldHeight/2, 0);
                tilemap.SetTile(position, boundaryTile);
            }
        }
        
        // 生成左右边界（在世界范围外）
        for (int y = 0; y < worldHeight; y++)
        {
            // 左边界
            for (int x = -boundaryWidth; x < 0; x++)
            {
                Vector3Int position = new Vector3Int(x - worldWidth/2, y - worldHeight/2, 0);
                tilemap.SetTile(position, boundaryTile);
            }
            
            // 右边界
            for (int x = worldWidth; x < worldWidth + boundaryWidth; x++)
            {
                Vector3Int position = new Vector3Int(x - worldWidth/2, y - worldHeight/2, 0);
                tilemap.SetTile(position, boundaryTile);
            }
        }
    }
} 