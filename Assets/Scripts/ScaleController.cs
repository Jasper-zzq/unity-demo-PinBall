using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


public class ReadOnlyAttribute : PropertyAttribute { }

public class ScaleController : MonoBehaviour
{
    // 缩放轴选择枚举
    public enum ScaleAxis
    {
        X,
        Y,
        Z
    }

    [Header("缩放设置")]
    [Tooltip("选择要缩放的轴")]
    public ScaleAxis scaleAxis = ScaleAxis.Z;

    [Tooltip("最大缩放值 (0-1)")]
    [Range(0f, 1f)]
    public float maxScale = 0.6f;

    [Tooltip("回弹时间 (秒)")]
    public float bounceBackTime = 0.3f;

    [Tooltip("作用力倍数")]
    public float playerForceMultiplier = 100f;

    private float currentCompression = 0f;

    private bool isCompressing = false;

    private bool isBouncingBack = false;

    // 原始缩放值
    private Vector3 originalScale;
    // 压缩开始时间
    private float compressionStartTime;
    // 压缩持续时间
    private const float MAX_COMPRESSION_TIME = 5f;

    void Start()
    {
        // 保存原始缩放
        originalScale = transform.localScale;

        // 确保对象有 Rigidbody 组件以支持物理碰撞检测
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation; // 冻结旋转，保持对象朝向
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // 连续碰撞检测，提高准确性
        }
    }

    void Update()
    {
        // 检测空格键输入（仅使用新 Input System）
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            StartCompression();
        }
        else if (keyboard.spaceKey.wasReleasedThisFrame)
        {
            StartBounceBack();
        }
    }

    /// <summary>
    /// 开始压缩过程
    /// </summary>
    private void StartCompression()
    {
        if (isBouncingBack)
        {
            StopCoroutine("BounceBackCoroutine");
            isBouncingBack = false;
        }

        isCompressing = true;
        compressionStartTime = Time.time;
    }

    /// <summary>
    /// 开始回弹过程
    /// </summary>
    private void StartBounceBack()
    {
        if (isCompressing)
        {
            isCompressing = false;
            StartCoroutine(BounceBackCoroutine());
        }
    }

    /// <summary>
    /// 压缩过程更新
    /// </summary>
    private void UpdateCompression()
    {
        if (!isCompressing) return;

        float elapsedTime = Time.time - compressionStartTime;
        float t = Mathf.Clamp01(elapsedTime / MAX_COMPRESSION_TIME);

        // 使用缓动函数使压缩更自然
        float easedT = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic

        currentCompression = Mathf.Lerp(0f, maxScale, easedT);
        ApplyScale();
    }

    /// <summary>
    /// 回弹协程
    /// </summary>
    private IEnumerator BounceBackCoroutine()
    {
        isBouncingBack = true;
        float startCompression = currentCompression;
        float elapsedTime = 0f;

        while (elapsedTime < bounceBackTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / bounceBackTime;

            // 使用弹性缓动函数
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            currentCompression = Mathf.Lerp(startCompression, 0f, easedT);
            ApplyScale();

            yield return null;
        }

        // 确保回到原始状态
        currentCompression = 0f;
        ApplyScale();
        isBouncingBack = false;
    }

    /// <summary>
    /// 应用缩放
    /// </summary>
    private void ApplyScale()
    {
        Vector3 newScale = originalScale;

        // 根据选择的轴应用缩放
        switch (scaleAxis)
        {
            case ScaleAxis.X:
                newScale.x = originalScale.x * (1f - currentCompression);
                break;
            case ScaleAxis.Y:
                newScale.y = originalScale.y * (1f - currentCompression);
                break;
            case ScaleAxis.Z:
                newScale.z = originalScale.z * (1f - currentCompression);
                break;
        }

        transform.localScale = newScale;
    }

    void LateUpdate()
    {
        // 在LateUpdate中更新压缩，确保在所有Update之后执行
        UpdateCompression();
    }

    // 可选：添加一些公用方法
    /// <summary>
    /// 重置到原始缩放
    /// </summary>
    public void ResetScale()
    {
        currentCompression = 0f;
        transform.localScale = originalScale;
        isCompressing = false;
        isBouncingBack = false;
    }

    /// <summary>
    /// 获取当前压缩进度 (0-1)
    /// </summary>
    public float GetCompressionProgress()
    {
        return currentCompression / maxScale;
    }

    /// <summary>
    /// 当检测到碰撞体时的处理方法
    /// </summary>
    /// <param name="colliders">检测到的碰撞体列表</param>
    protected virtual void OnCollisionDetected(Collider[]  colliders)
    {
        // 子类可以重写此方法来自定义碰撞处理逻辑
        foreach (Collider collider in colliders)
        {
            // 如果是Player对象，施加作用力
            if (collider.CompareTag("Player"))
            {
                ApplyForceToPlayer(collider);
            }

            // 默认行为：输出检测到的碰撞体信息
            // Debug.Log($"检测到碰撞体: tag: {collider.gameObject.tag} 名称: {collider.gameObject.name} 在位置 {collider.transform.position}");
        }
    }

    /// <summary>
    /// 给Player对象施加作用力
    /// </summary>
    /// <param name="playerCollider">Player的碰撞体</param>
    private void ApplyForceToPlayer(Collider playerCollider)
    {
        // 获取Player的Rigidbody
        Rigidbody playerRb = playerCollider.GetComponent<Rigidbody>();
        if (playerRb == null)
        {
            Debug.LogWarning($"Player对象 {playerCollider.gameObject.name} 没有Rigidbody组件，无法施加作用力");
            return;
        }

        // 根据缩放轴确定作用力方向
        Vector3 forceDirection = Vector3.zero;
        switch (scaleAxis)
        {
            case ScaleAxis.X:
                forceDirection = transform.right;
                break;
            case ScaleAxis.Y:
                forceDirection = transform.up;
                break;
            case ScaleAxis.Z:
                forceDirection = transform.forward;
                break;
        }

        // 根据当前压缩量计算作用力大小
        // 压缩量越大（currentCompression越大），作用力越大
        float forceMagnitude = currentCompression * playerForceMultiplier;

        // 施加作用力
        Vector3 force = forceDirection * forceMagnitude;
        playerRb.AddForce(force, ForceMode.Impulse);

        Debug.Log($"给Player施加作用力: 方向={forceDirection}, 大小={forceMagnitude}, 总力={force}");
    }

    /// <summary>
    /// 物理碰撞开始时的处理（Rigidbody碰撞事件）
    /// </summary>
    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (isBouncingBack)
        {
            OnCollisionDetected(new Collider[] { collision.collider });
            return;
        }
       
    }

    /// <summary>
    /// 物理碰撞持续时的处理
    /// </summary>
    protected virtual void OnCollisionStay(Collision collision)
    {
        if (isBouncingBack)
        {
            OnCollisionDetected(new Collider[] { collision.collider });
            return;
        }
        // Debug.Log($"物理碰撞持续: {collision.gameObject.name}");
    }

    /// <summary>
    /// 物理碰撞结束时的处理
    /// </summary>
    protected virtual void OnCollisionExit(Collision collision)
    {
        // Debug.Log($"物理碰撞结束: {collision.gameObject.name}");
    }
}
