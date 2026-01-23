using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CollisionZoneGenerator : MonoBehaviour
{
    [Header("区域设置")]
    [Tooltip("碰撞区域的数量")]
    [Range(1, 20)]
    public int zoneCount = 5;

    [Tooltip("得分区域的数量（从起始位置开始）")]
    [Range(0, 20)]
    public int scoringZoneCount = 2;

    [Tooltip("点光源的颜色（用于得分区域）")]
    public Color pointLightColor = Color.yellow;

    [Header("区域属性")]
    [Tooltip("区域高度")]
    [Range(0.1f, 10f)]
    public float zoneHeight = 2f;

    [Tooltip("区域厚度")]
    [Range(0.01f, 1f)]
    public float zoneThickness = 0.1f;

    [Tooltip("点光源强度")]
    [Range(0f, 10f)]
    public float lightIntensity = 2f;

    [Tooltip("点光源范围")]
    [Range(0f, 20f)]
    public float lightRange = 5f;

    // 生成的区域对象列表
    private List<GameObject> generatedZones = new List<GameObject>();

    void Start()
    {
        GenerateCollisionZones();
    }

    void Update()
    {
        // 检测R键输入，按R键重新生成区域
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            Debug.Log("检测到R键按下，重新生成碰撞区域...");
            GenerateCollisionZones();
        }
    }

    /// <summary>
    /// 生成碰撞区域
    /// </summary>
    public void GenerateCollisionZones()
    {
        ClearZones();

        // 获取当前对象的边界
        Bounds bounds = GetComponent<Renderer>()?.bounds ?? GetComponent<Collider>()?.bounds ?? new Bounds(transform.position, Vector3.one);

        // 计算Z轴长度
        float zLength = bounds.size.z;
        float zoneWidth = zLength / zoneCount;

        Debug.Log($"生成碰撞区域: Z轴长度={zLength:F2}, 区域数量={zoneCount}, 每个区域宽度={zoneWidth:F2}");

        // 生成每个区域
        for (int i = 0; i < zoneCount; i++)
        {
            // 计算区域的起始和结束Z位置
            float startZ = bounds.min.z + i * zoneWidth;
            float endZ = bounds.min.z + (i + 1) * zoneWidth;

            // 创建区域对象
            GameObject zoneObject = new GameObject($"CollisionZone_{i + 1}");
            zoneObject.transform.SetParent(transform);
            zoneObject.transform.localPosition = Vector3.zero;
            zoneObject.transform.localRotation = Quaternion.identity;

            // 计算区域中心位置
            Vector3 zoneCenter = new Vector3(
                bounds.center.x,
                bounds.center.y,
                startZ + zoneWidth / 2f
            );

            // 创建Box Collider
            BoxCollider collider = zoneObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(bounds.size.x, zoneHeight, zoneWidth - zoneThickness * 2);
            collider.center = transform.InverseTransformPoint(zoneCenter);

            // 设置为触发器
            collider.isTrigger = true;

            // 添加区域标识脚本
            ZoneIdentifier zoneId = zoneObject.AddComponent<ZoneIdentifier>();
            zoneId.zoneIndex = i;
            zoneId.isScoringZone = i < scoringZoneCount;

            // 为得分区域添加点光源
            if (i < scoringZoneCount)
            {
                Light pointLight = zoneObject.AddComponent<Light>();
                pointLight.type = LightType.Point;
                pointLight.color = pointLightColor;
                pointLight.intensity = lightIntensity;
                pointLight.range = lightRange;

                // 设置光源位置（稍微高于区域中心）
                pointLight.transform.localPosition = collider.center + Vector3.up * (zoneHeight / 2f + 0.5f);

                Debug.Log($"得分区域 {i + 1}: 添加点光源，颜色={pointLightColor}");
            }

            generatedZones.Add(zoneObject);

            Debug.Log($"生成区域 {i + 1}: 位置范围 Z=[{startZ:F2}, {endZ:F2}], 中心={zoneCenter}, 得分区域={i < scoringZoneCount}");
        }
    }

    /// <summary>
    /// 清除所有生成的区域
    /// </summary>
    public void ClearZones()
    {
        foreach (GameObject zone in generatedZones)
        {
            if (zone != null)
            {
                DestroyImmediate(zone);
            }
        }
        generatedZones.Clear();
        Debug.Log("清除了所有碰撞区域");
    }

    /// <summary>
    /// 获取生成的区域数量
    /// </summary>
    public int GetZoneCount()
    {
        return generatedZones.Count;
    }

    /// <summary>
    /// 获取得分区域的数量
    /// </summary>
    public int GetScoringZoneCount()
    {
        return Mathf.Min(scoringZoneCount, zoneCount);
    }

    /// <summary>
    /// 在编辑器中可视化区域
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!enabled) return;

        // 获取边界
        Bounds bounds = GetComponent<Renderer>()?.bounds ?? GetComponent<Collider>()?.bounds ?? new Bounds(transform.position, Vector3.one);

        float zLength = bounds.size.z;
        float zoneWidth = zLength / zoneCount;

        // 绘制区域划分线
        Gizmos.color = Color.blue;
        for (int i = 1; i < zoneCount; i++)
        {
            float z = bounds.min.z + i * zoneWidth;
            Vector3 start = new Vector3(bounds.min.x, bounds.center.y, z);
            Vector3 end = new Vector3(bounds.max.x, bounds.center.y, z);
            Gizmos.DrawLine(start, end);
        }

        // 绘制得分区域
        Gizmos.color = Color.yellow;
        for (int i = 0; i < Mathf.Min(scoringZoneCount, zoneCount); i++)
        {
            float startZ = bounds.min.z + i * zoneWidth;
            float endZ = bounds.min.z + (i + 1) * zoneWidth;

            Vector3 center = new Vector3(bounds.center.x, bounds.center.y, startZ + zoneWidth / 2f);
            Vector3 size = new Vector3(bounds.size.x, zoneHeight, zoneWidth);

            Gizmos.DrawWireCube(center, size);
        }

        // 绘制普通区域
        Gizmos.color = Color.gray;
        for (int i = scoringZoneCount; i < zoneCount; i++)
        {
            float startZ = bounds.min.z + i * zoneWidth;
            float endZ = bounds.min.z + (i + 1) * zoneWidth;

            Vector3 center = new Vector3(bounds.center.x, bounds.center.y, startZ + zoneWidth / 2f);
            Vector3 size = new Vector3(bounds.size.x, zoneHeight, zoneWidth);

            Gizmos.DrawWireCube(center, size);
        }
    }
}

/// <summary>
/// 区域标识脚本，用于区分不同区域
/// </summary>
public class ZoneIdentifier : MonoBehaviour
{
    public int zoneIndex;
    public bool isScoringZone;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            string zoneType = isScoringZone ? "得分区域" : "普通区域";
            Debug.Log($"玩家进入{zoneType} {zoneIndex + 1}");

            // 这里可以添加得分逻辑或其他区域特定行为
            if (isScoringZone)
            {
                // 得分区域的特殊处理
                Debug.Log($"玩家进入得分区域 {zoneIndex + 1}，获得分数！");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            string zoneType = isScoringZone ? "得分区域" : "普通区域";
            Debug.Log($"玩家离开{zoneType} {zoneIndex + 1}");
        }
    }
}
