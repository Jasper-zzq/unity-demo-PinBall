using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // 珠子类型枚举
    public enum PearlType
    {
        钢珠,
        Pokeball
    }

    [Header("生成设置")]
    [Tooltip("玩家生成点X坐标")]
    public float spawnX = 0f;

    [Tooltip("玩家生成点Y坐标")]
    public float spawnY = 0f;

    [Tooltip("玩家生成点Z坐标")]
    public float spawnZ = 0f;

    [Tooltip("珠子类型")]
    public PearlType pearlType = PearlType.钢珠;

    [Header("预制体引用")]
    [Tooltip("钢珠预制体")]
    public GameObject steelBallPrefab;

    [Tooltip("Pokeball预制体")]
    public GameObject pokeballPrefab;

    // 存储生成的玩家对象引用
    private GameObject currentPlayer;

    void Update()
    {
        // 检测R键输入（仅使用新 Input System）
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.rKey.wasPressedThisFrame)
        {
            InitPlayer();
        }

        // 检查玩家是否掉出地图
        if (currentPlayer != null && currentPlayer.transform.position.y < 1f)
        {
            Destroy(currentPlayer);
            currentPlayer = null;
            Debug.Log("玩家掉出地图，已销毁");
        }
    }

    /// <summary>
    /// 初始化玩家函数
    /// 根据当前参数生成预制体并设置tag为Player
    /// </summary>
    public void InitPlayer()
    {
        // 如果已有玩家对象，先销毁 
        // if (currentPlayer != null)
        // {
        //     Destroy(currentPlayer);
        // }

        // 根据珠子类型选择预制体
        GameObject prefab = null;
        switch (pearlType)
        {
            case PearlType.钢珠:
                prefab = steelBallPrefab;
                break;
            case PearlType.Pokeball:
                prefab = pokeballPrefab;
                break;
        }

        // 检查预制体是否已赋值
        if (prefab == null)
        {
            Debug.LogError($"预制体未赋值: {pearlType}");
            return;
        }

        // 创建生成点位置
        Vector3 spawnPosition = new Vector3(spawnX, spawnY, spawnZ);

        // 实例化预制体
        currentPlayer = Instantiate(prefab, spawnPosition, Quaternion.identity);

        // 设置tag为Player
        currentPlayer.tag = "Player";

        // 添加Rigidbody组件（如果没有的话）
        if (currentPlayer.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = currentPlayer.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        Debug.Log($"玩家已生成: 类型={pearlType}, 位置=({spawnX}, {spawnY}, {spawnZ})");
    }

    /// <summary>
    /// 获取当前玩家的位置
    /// </summary>
    public Vector3 GetPlayerPosition()
    {
        if (currentPlayer != null)
        {
            return currentPlayer.transform.position;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// 检查玩家是否已生成
    /// </summary>
    public bool IsPlayerSpawned()
    {
        return currentPlayer != null;
    }

    /// <summary>
    /// 销毁当前玩家
    /// </summary>
    public void DestroyPlayer()
    {
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
            currentPlayer = null;
        }
    }
}
