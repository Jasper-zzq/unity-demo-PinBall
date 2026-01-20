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
}
