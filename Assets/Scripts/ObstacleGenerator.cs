using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Text;

[System.Serializable]
public struct ObstacleType
{
    [Tooltip("障碍物预制体")]
    public GameObject prefab;

    [Tooltip("生成权重 (决定该类型出现的概率)")]
    [Range(0f, 10f)]
    public float weight;

    [Tooltip("该类型的最大生成数量 (0表示无限制)")]
    public int maxCount;
}

public class ObstacleGenerator : MonoBehaviour
{
    [Header("生成设置")]
    [Tooltip("障碍物类型列表")]
    public ObstacleType[] obstacleTypes;

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

    [Header("日志设置")]
    [Tooltip("是否启用详细日志")]
    public bool enableDetailedLogging = true;

    [Tooltip("是否显示生成统计信息")]
    public bool showGenerationStats = true;

    // 生成的障碍物列表
    private List<GameObject> generatedObstacles = new List<GameObject>();

    // 生成统计信息
    private Dictionary<string, int> generationStats = new Dictionary<string, int>();

    // 生成日志
    private StringBuilder generationLog = new StringBuilder();

    void Start()
    {
        // 游戏开始时自动生成障碍物
        GenerateObstacles();
    }

    void Update()
    {
        // 检测T键输入，按T键重新生成障碍物（使用新 Input System）
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.tKey.wasPressedThisFrame)
        {
            randomSeed = Random.Range(1, int.MaxValue);

            Debug.Log("检测到T键按下，重新生成障碍物...");
            GenerateObstacles();
        }
    }

    /// <summary>
    /// 生成障碍物
    /// </summary>
    public void GenerateObstacles()
    {
        float startTime = Time.realtimeSinceStartup;
        ClearObstacles();
        generationLog.Clear();
        generationStats.Clear();

        LogMessage("开始生成障碍物...");
        LogMessage($"生成时间: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        // 验证障碍物类型设置
        if (obstacleTypes == null || obstacleTypes.Length == 0)
        {
            LogError("障碍物类型未设置！");
            return;
        }

        // 验证每个类型都有有效的预制体
        for (int i = 0; i < obstacleTypes.Length; i++)
        {
            if (obstacleTypes[i].prefab == null)
            {
                LogError($"障碍物类型 {i} 的预制体未设置！");
                return;
            }
            generationStats[obstacleTypes[i].prefab.name] = 0;
        }

        // 获取当前对象的边界
        Bounds bounds = GetComponent<Renderer>()?.bounds ?? GetComponent<Collider>()?.bounds ?? new Bounds(transform.position, Vector3.one);
        LogMessage($"生成区域: 中心={bounds.center}, 大小={bounds.size}");

        // 考虑边距后的有效区域
        Vector3 effectiveSize = bounds.size - new Vector3(margin * 2, 0, margin * 2);
        // 修复: 使用父物体的 world Y 作为生成平面的 Y 值（保证相对父物体 local Y == 0）
        Vector3 effectiveMin = bounds.min + new Vector3(margin, 0f, margin);
        Vector3 effectiveMax = bounds.max - new Vector3(margin, 0f, margin);

        // 使用父物体的 world Y 作为生成高度
        float parentY = transform.position.y;
        effectiveMin.y = parentY;
        effectiveMax.y = parentY;

        // 确保有效区域是正的
        if (effectiveSize.x <= 0 || effectiveSize.z <= 0)
        {
            LogWarning("边距太大，有效生成区域为负数！");
            return;
        }

        LogMessage($"有效生成区域: 大小={effectiveSize}, 边距={margin}");

        // 设置随机种子
        Random.InitState(randomSeed);
        LogMessage($"随机种子: {randomSeed}");

        // 计算大约需要生成的障碍物数量
        float area = effectiveSize.x * effectiveSize.z;
        float obstacleArea = Mathf.PI * (minDistance / 2f) * (minDistance / 2f); // 假设圆形区域
        int estimatedCount = Mathf.RoundToInt(area / obstacleArea * density);

        LogMessage($"生成参数: 面积={area:F2}, 密度={density}, 预估数量={estimatedCount}");

        // 使用泊松盘采样生成位置
        List<Vector3> positions = GeneratePoissonDiskSampling(effectiveMin, effectiveMax, minDistance, estimatedCount);

        LogMessage($"泊松盘采样生成 {positions.Count} 个位置");

        // 计算权重总和用于随机选择类型
        float totalWeight = 0f;
        foreach (var type in obstacleTypes)
        {
            totalWeight += type.weight;
        }

        // 生成障碍物
        int generatedCount = 0;
        Dictionary<string, int> typeCounts = new Dictionary<string, int>();

        foreach (Vector3 position in positions)
        {
            // 根据权重随机选择障碍物类型
            GameObject selectedPrefab = SelectObstacleType(totalWeight);

            if (selectedPrefab != null)
            {
                // 检查该类型的最大数量限制
                string prefabName = selectedPrefab.name;
                if (!typeCounts.ContainsKey(prefabName))
                    typeCounts[prefabName] = 0;

                bool canGenerate = true;
                for (int i = 0; i < obstacleTypes.Length; i++)
                {
                    if (obstacleTypes[i].prefab == selectedPrefab &&
                        obstacleTypes[i].maxCount > 0 &&
                        typeCounts[prefabName] >= obstacleTypes[i].maxCount)
                    {
                        canGenerate = false;
                        break;
                    }
                }

                if (canGenerate)
                {
                    // 将世界位置转换为父物体的局部位置，然后以局部坐标创建子物体
                    Vector3 localPos = transform.InverseTransformPoint(position);
                    // 不对 Y 轴进行旋转影响：固定为父物体局部平面高度（例如 0）
                    localPos.y = 0f;

                    GameObject obstacle = Instantiate(selectedPrefab, transform);
                    obstacle.transform.localPosition = localPos;
                    obstacle.transform.localRotation = Quaternion.identity;
                    // 设置预制体缩放值为 (0.05, 10, 0.05)
                    obstacle.transform.localScale = new Vector3(0.05f, 2f, 0.05f);
                    obstacle.tag = "MapObstacle";
                    generatedObstacles.Add(obstacle);
                    typeCounts[prefabName]++;
                    generatedCount++;

                    if (enableDetailedLogging)
                    {
                        LogMessage($"生成障碍物: {prefabName} 在位置 {position}");
                    }
                }
            }
        }

        float generationTime = Time.realtimeSinceStartup - startTime;

        // 显示生成统计信息
        LogMessage($"生成完成! 总共生成了 {generatedCount} 个障碍物");
        LogMessage($"生成耗时: {generationTime:F3} 秒");

        if (showGenerationStats)
        {
            LogMessage("生成统计:");
            foreach (var kvp in typeCounts)
            {
                LogMessage($"  {kvp.Key}: {kvp.Value} 个");
            }
        }

        // 在控制台输出完整日志
        if (enableDetailedLogging)
        {
            Debug.Log(generationLog.ToString());
        }
    }

    /// <summary>
    /// 根据权重随机选择障碍物类型
    /// </summary>
    private GameObject SelectObstacleType(float totalWeight)
    {
        if (totalWeight <= 0) return obstacleTypes[0].prefab;

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var type in obstacleTypes)
        {
            currentWeight += type.weight;
            if (randomValue <= currentWeight)
            {
                return type.prefab;
            }
        }

        return obstacleTypes[0].prefab; // 默认返回第一个
    }

    /// <summary>
    /// 清除所有生成的障碍物
    /// </summary>
    public void ClearObstacles()
    {
        int clearedCount = generatedObstacles.Count;

        foreach (GameObject obstacle in generatedObstacles)
        {
            if (obstacle != null)
            {
                DestroyImmediate(obstacle);
            }
        }
        generatedObstacles.Clear();
        generationStats.Clear();

        if (clearedCount > 0)
        {
            LogMessage($"清除了 {clearedCount} 个障碍物");
            if (enableDetailedLogging)
            {
                Debug.Log(generationLog.ToString());
            }
        }
    }

    /// <summary>
    /// 获取生成的障碍物数量
    /// </summary>
    public int GetObstacleCount()
    {
        return generatedObstacles.Count;
    }

    /// <summary>
    /// 获取指定类型障碍物的数量
    /// </summary>
    public int GetObstacleCountByType(string prefabName)
    {
        return generatedObstacles.FindAll(obj => obj != null && obj.name.StartsWith(prefabName)).Count;
    }

    /// <summary>
    /// 获取生成日志
    /// </summary>
    public string GetGenerationLog()
    {
        return generationLog.ToString();
    }

    /// <summary>
    /// 记录普通消息
    /// </summary>
    private void LogMessage(string message)
    {
        string logEntry = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
        generationLog.AppendLine(logEntry);

        if (!enableDetailedLogging)
        {
            Debug.Log(message);
        }
    }

    /// <summary>
    /// 记录警告消息
    /// </summary>
    private void LogWarning(string message)
    {
        string logEntry = $"[{System.DateTime.Now:HH:mm:ss}] 警告: {message}";
        generationLog.AppendLine(logEntry);
        Debug.LogWarning(message);
    }

    /// <summary>
    /// 记录错误消息
    /// </summary>
    private void LogError(string message)
    {
        string logEntry = $"[{System.DateTime.Now:HH:mm:ss}] 错误: {message}";
        generationLog.AppendLine(logEntry);
        Debug.LogError(message);
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

                    // 直接在 XZ 平面上计算新的 X,Z，并将 Y 强制为 min.y（已由 caller 设置为 0 或期望高度）
                    Vector3 newPoint = new Vector3(
                        point.x + Mathf.Cos(angle) * distance,
                        min.y,
                        point.z + Mathf.Sin(angle) * distance
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
