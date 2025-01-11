using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class OreCluster
{
    public OreType oreType;
    public TileBase oreTile;
    
    // 矿脉配置
    public int veinCount = 2;        // 矿脉数量
    public float veinDistance = 10f;  // 矿脉之间的最小距离
    
 // 矿脉大小配置
    public int veinSize = 10;        // 矿脉大小（基准矿石数量）
    
    // 获取实际的矿脉大小范围
    public int GetMinVeinSize() => Mathf.RoundToInt(veinSize * 0.7f);
    public int GetMaxVeinSize() => Mathf.RoundToInt(veinSize * 1.3f);
    public float GetVeinRadius() => Mathf.Sqrt(veinSize) * 1.5f;  // 根据矿脉大小动态计算合适的扩散半径
} 