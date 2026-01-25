using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 附加到玩家实例上，用于记录与其它物体的碰撞/触发事件，便于调试碰撞相关问题。
/// </summary>
public class PlayerCollisionLogger : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[PlayerCollision] Enter: '{collision.gameObject.name}' (tag={collision.gameObject.tag}), contacts={collision.contactCount}");
    }

    private void OnCollisionExit(Collision collision)
    {
        Debug.Log($"[PlayerCollision] Exit: '{collision.gameObject.name}' (tag={collision.gameObject.tag})");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[PlayerCollision] TriggerEnter: '{other.gameObject.name}' (tag={other.gameObject.tag})");
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[PlayerCollision] TriggerExit: '{other.gameObject.name}' (tag={other.gameObject.tag})");
    }
}


