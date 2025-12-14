using UnityEngine;

public class CameraFollowBubble : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 0.5f, -2f);
    public float followSpeed = 3f;
    public float rotationSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        // 1️⃣ 平滑跟隨位置
        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);

        // 2️⃣ 平滑旋轉（看向泡泡，不抖動）
        Vector3 dir = target.position - transform.position;
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
