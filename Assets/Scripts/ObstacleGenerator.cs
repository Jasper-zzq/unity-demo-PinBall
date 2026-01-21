using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleGenerator : MonoBehaviour
{
    [Header("生成设置")]
    [Tooltip("障碍物预制体")]
    public GameObject obstaclePrefab;

    [Tooltip("障碍物之间的最小距离")]
    [Range(0.1f, 5f)]
    public float minDistance = 1f;

    [Tooltip("生成密度 (0-1)")]
    [Range(0f, 1f)]
    public float density = 0.3f;

    [Tooltip("边缘边距，不在边距范围内生成障碍物")]
    [Range(0f, 10f)]
    public float margin = 1f;

    [Tooltip("随机种子，用于重现相同的生成结果")]
    public int randomSeed = 0;

    // 生成的障碍物列表
    private List<GameObject> generatedObstacles = new List<GameObject>();

    /// <summary>
    /// 生成障碍物
    /// </summary>
    public void GenerateObstacles()
    {
        ClearObstacles();

        if (obstaclePrefab == null)
        {
            Debug.LogError("障碍物预制体未设置！");
            return;
        }

        // 获取当前对象的边界
        Bounds bounds = GetComponent<Renderer>()?.bounds ?? GetComponent<Collider>()?.bounds ?? new Bounds(transform.position, Vector3.one);

        // 考虑边距后的有效区域
        Vector3 effectiveSize = bounds.size - new Vector3(margin * 2, 0, margin * 2);
        Vector3 effectiveMin = bounds.min + new Vector3(margin, bounds.min.y, margin);
        Vector3 effectiveMax = bounds.max - new Vector3(margin, 0, margin);

        // 确保有效区域是正的
        if (effectiveSize.x <= 0 || effectiveSize.z <= 0)
        {
            Debug.LogWarning("边距太大，有效生成区域为负数！");
            return;
        }

        // 设置随机种子
        Random.InitState(randomSeed);

        // 计算大约需要生成的障碍物数量
        float area = effectiveSize.x * effectiveSize.z;
        float obstacleArea = Mathf.PI * (minDistance / 2f) * (minDistance / 2f); // 假设圆形区域
        int estimatedCount = Mathf.RoundToInt(area / obstacleArea * density);

        // 使用泊松盘采样生成位置
        List<Vector3> positions = GeneratePoissonDiskSampling(effectiveMin, effectiveMax, minDistance, estimatedCount);

        // 生成障碍物
        foreach (Vector3 position in positions)
        {
            GameObject obstacle = Instantiate(obstaclePrefab, position, Quaternion.identity, transform);
            generatedObstacles.Add(obstacle);
        }

        Debug.Log($"生成了 {generatedObstacles.Count} 个障碍物");
    }

    /// <summary>
    /// 清除所有生成的障碍物
    /// </summary>
    public void ClearObstacles()
    {
        foreach (GameObject obstacle in generatedObstacles)
        {
            if (obstacle != null)
            {
                DestroyImmediate(obstacle);
            }
        }
        generatedObstacles.Clear();
    }

    /// <summary>
    /// 使用泊松盘采样生成位置
    /// </summary>
    private List<Vector3> GeneratePoissonDiskSampling(Vector3 min, Vector3 max, float minDist, int maxAttempts = 30)
    {
        List<Vector3> points = new List<Vector3>();
        List<Vector3> activePoints = new List<Vector3>();

        // 第一个点随机放置
        Vector3 firstPoint = new Vector3(
            Random.Range(min.x, max.x),
            min.y, // Y坐标保持在表面
            Random.Range(min.z, max.z)
        );
        points.Add(firstPoint);
        activePoints.Add(firstPoint);

        while (activePoints.Count > 0 && points.Count < 1000) // 防止无限循环
        {
            int randomIndex = Random.Range(0, activePoints.Count);
            Vector3 point = activePoints[randomIndex];
            bool found = false;

            // 在当前点周围尝试生成新点
            for (int i = 0; i < maxAttempts; i++)
            {
                // 在minDistance到2*minDistance之间随机选择距离
                float angle = Random.Range(0f, 2f * Mathf.PI);
                float distance = Random.Range(minDist, 2f * minDist);

                Vector3 newPoint = point + new Vector3(
                    Mathf.Cos(angle) * distance,
                    0,
                    Mathf.Sin(angle) * distance
                );

                // 检查新点是否在有效范围内
                if (newPoint.x >= min.x && newPoint.x <= max.x &&
                    newPoint.z >= min.z && newPoint.z <= max.z)
                {
                    // 检查与所有现有点的距离
                    bool tooClose = false;
                    foreach (Vector3 existingPoint in points)
                    {
                        if (Vector3.Distance(new Vector3(newPoint.x, 0, newPoint.z),
                                           new Vector3(existingPoint.x, 0, existingPoint.z)) < minDist)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (!tooClose)
                    {
                        points.Add(newPoint);
                        activePoints.Add(newPoint);
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                activePoints.RemoveAt(randomIndex);
            }
        }

        return points;
    }

    /// <summary>
    /// 在编辑器中可视化生成区域
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!enabled) return;

        // 获取边界
        Bounds bounds = GetComponent<Renderer>()?.bounds ?? GetComponent<Collider>()?.bounds ?? new Bounds(transform.position, Vector3.one);

        // 绘制原始边界
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        // 绘制有效生成区域
        Vector3 effectiveSize = bounds.size - new Vector3(margin * 2, 0, margin * 2);
        Vector3 effectiveCenter = bounds.center;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(effectiveCenter, effectiveSize);

        // 绘制生成的障碍物位置
        Gizmos.color = Color.red;
        foreach (GameObject obstacle in generatedObstacles)
        {
            if (obstacle != null)
            {
                Gizmos.DrawSphere(obstacle.transform.position, 0.1f);
            }
        }
    }
}
