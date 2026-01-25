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
    [Tooltip("跑马灯在 marqueeDuration 时间内循环次数")]
    [Range(1, 10)]
    public int marqueeLoops = 3;

    // 生成的区域对象列表
    private List<GameObject> generatedZones = new List<GameObject>();
    // 对应的灯列表（与 generatedZones 对应）
    private List<Light> generatedLights = new List<Light>();

    // 玩家是否已进入任一区域（用于阻止后续区域触发）
    private bool playerHasEntered = false;

    // 启动时是否自动运行灯光序列
    public bool runStartupLightSequence = true;

    void Start()
    {
        if (runStartupLightSequence)
        {
            StartCoroutine(RunStartupSequence());
        }
        else
        {
            GenerateCollisionZones();
            // 打开所有灯（如果存在）
            foreach (Light l in generatedLights)
            {
                if (l != null) l.enabled = true;
            }
        }
    }

    void Update()
    {
        // 检测R键输入，按T键重新生成区域
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.tKey.wasPressedThisFrame)
        {
            Debug.Log("检测到T键按下，重新生成碰撞区域...");
            // 停止现有序列并重新运行完整启动序列
            StopAllCoroutines();
            StartCoroutine(RunStartupSequence());
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

        // 计算实际得分区域数（不超过区域总数）
        int actualScoring = Mathf.Min(scoringZoneCount, zoneCount);
        // 随机选择得分区域索引
        HashSet<int> scoringIndices = new HashSet<int>();
        while (scoringIndices.Count < actualScoring)
        {
            scoringIndices.Add(Random.Range(0, zoneCount));
        }

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
            zoneId.isScoringZone = scoringIndices.Contains(i);

            // 为得分区域添加点光源
            // 为所有区域都创建可控点光源（初始为关闭状态）
            Light zoneLight = zoneObject.AddComponent<Light>();
            zoneLight.type = LightType.Point;
            zoneLight.color = zoneId.isScoringZone ? pointLightColor : Color.white;
            zoneLight.intensity = lightIntensity;
            zoneLight.range = lightRange;
            zoneLight.transform.localPosition = collider.center + Vector3.up * (zoneHeight / 2f + 0.5f);
            // 初始关闭，由启动序列控制
            zoneLight.enabled = false;
            generatedLights.Add(zoneLight);

            if (zoneId.isScoringZone)
            {
                Debug.Log($"得分区域 {i + 1}: 添加点光源，颜色={pointLightColor}");
            }

            generatedZones.Add(zoneObject);
            Debug.Log($"生成区域 {i + 1}: 位置范围 Z=[{startZ:F2}, {endZ:F2}], 中心={zoneCenter}, 得分区域={zoneId.isScoringZone}");
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
        generatedLights.Clear();
        // 重置玩家进入标记
        playerHasEntered = false;
        Debug.Log("清除了所有碰撞区域");
    }

    /// <summary>
    /// 运行完整的启动灯光序列：
    /// 1) 跑马灯（2秒） -> 2) 全部熄灭 -> 3) 得分区闪灯3下（1秒） -> 4) 全部亮起
    /// </summary>
    private IEnumerator RunStartupSequence()
    {
        // 先生成区域与灯
        GenerateCollisionZones();

        // 1) 跑马灯效果（2秒）
        float marqueeDuration = 2f;
        int totalLights = generatedLights.Count;
        if (totalLights > 0)
        {
            // 在相同的 marqueeDuration 内完成多次完整跑马灯循环
            int loops = Mathf.Max(1, marqueeLoops);
            float stepTime = marqueeDuration / (totalLights * loops);
            for (int loop = 0; loop < loops; loop++)
            {
                for (int i = 0; i < totalLights; i++)
                {
                    // 打开当前灯，关闭其他灯
                    for (int j = 0; j < totalLights; j++)
                    {
                        generatedLights[j].enabled = (j == i);
                    }
                    yield return new WaitForSeconds(stepTime);
                }
            }
        }

        // 2) 全部熄灭
        foreach (Light l in generatedLights) if (l != null) l.enabled = false;

        // 3) 得分区域闪灯3下（在1秒内完成）
        int flashCount = 3;
        float totalFlashDuration = 1f;
        float perFlash = totalFlashDuration / flashCount;
        for (int f = 0; f < flashCount; f++)
        {
            // 打开得分区灯
            for (int k = 0; k < generatedLights.Count; k++)
            {
                Light l = generatedLights[k];
                if (l == null) continue;
                // 只有 ZoneIdentifier 标记为得分区的灯闪烁
                ZoneIdentifier zid = generatedZones[k].GetComponent<ZoneIdentifier>();
                if (zid != null && zid.isScoringZone)
                {
                    l.enabled = true;
                }
                else
                {
                    l.enabled = false;
                }
            }
            yield return new WaitForSeconds(perFlash * 0.5f);
            // 关闭所有灯
            foreach (Light l2 in generatedLights) if (l2 != null) l2.enabled = false;
            yield return new WaitForSeconds(perFlash * 0.5f);
        }

        // 4) 全部亮起
        foreach (Light l in generatedLights) if (l != null) l.enabled = true;
    }

    /// <summary>
    /// 外部调用：当玩家进入某个区域时通知生成器
    /// </summary>
    public void NotifyPlayerEnteredZone(ZoneIdentifier zone)
    {
        if (playerHasEntered) return;
        playerHasEntered = true;

        Debug.Log($"玩家优先进入区域 {zone.zoneIndex + 1}，得分区域={zone.isScoringZone}");

        // 关闭其他区域的判断（禁用它们的 ZoneIdentifier 组件）
        for (int i = 0; i < generatedZones.Count; i++)
        {
            GameObject z = generatedZones[i];
            if (z == null) continue;
            ZoneIdentifier zid = z.GetComponent<ZoneIdentifier>();
            if (zid == null) continue;
            if (zid.zoneIndex != zone.zoneIndex)
            {
                zid.enabled = false;
                // 也可以禁用碰撞体以防止额外事件
                Collider c = z.GetComponent<Collider>();
                if (c != null) c.enabled = false;
            }
        }

        // 如果进入的是得分区域，则让该区域的灯闪烁三次
        if (zone.isScoringZone)
        {
            Light l = zone.GetComponent<Light>();
            if (l != null)
            {
                StartCoroutine(FlashLight(l, 3, 1f));
            }
        }
    }

    /// <summary>
    /// 让指定灯闪烁若干次（总时长由 duration 指定）
    /// </summary>
    private IEnumerator FlashLight(Light light, int flashes, float duration)
    {
        if (light == null || flashes <= 0)
            yield break;

        float perFlash = duration / flashes;
        for (int i = 0; i < flashes; i++)
        {
            light.enabled = true;
            yield return new WaitForSeconds(perFlash * 0.5f);
            light.enabled = false;
            yield return new WaitForSeconds(perFlash * 0.5f);
        }

        // 最终保持开启状态
        light.enabled = true;
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
            // 通知生成器（生成器会处理是否这是玩家优先进入的区域并关闭其他区域判定）
            CollisionZoneGenerator gen = GetComponentInParent<CollisionZoneGenerator>();
            if (gen != null)
            {
                gen.NotifyPlayerEnteredZone(this);
            }

            string zoneType = isScoringZone ? "得分区域" : "普通区域";
            Debug.Log($"玩家进入{zoneType} {zoneIndex + 1}");

            if (isScoringZone)
            {
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
