using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    public Camera targetCamera;
    public bool lockX = false; // 若只想水平跟著轉(不抬頭低頭)可勾
    public bool lockY = false;
    public bool lockZ = false;

    void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null) return;

        // 讓物件面向鏡頭
        Vector3 dir = transform.position - targetCamera.transform.position;

        // 想只做水平朝向：把Y固定成0，避免上下抬頭低頭
        if (lockX || lockY || lockZ)
        {
            // 先算完整旋轉
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
            Vector3 e = rot.eulerAngles;

            if (lockX) e.x = 0;
            if (lockY) e.y = transform.eulerAngles.y; // 保留原本Y
            if (lockZ) e.z = 0;

            transform.rotation = Quaternion.Euler(e);
        }
        else
        {
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }
}
