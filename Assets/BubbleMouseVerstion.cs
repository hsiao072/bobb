using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class BubbleMouseVersion : MonoBehaviour
{
    // =========【Prefab 設定】=========
    public GameObject mainBubblePrefab;   // 主泡泡
    public GameObject subBubblePrefab;    // 其他泡泡

    // =========【狀態】=========
    private BubbleGrowth currentBubble;
    private BubbleGrowth mainBubble;

    private bool lastTriggerState = false;

    [Header("Controller")]
    public Transform rightHandTransform;

    // =========【Haptics 設定】=========
    [Header("Haptics")]
    public float hapticAmplitude = 0.4f;
    public float hapticDuration = 0.05f;

    [Header("Audio")]
    public AudioClip triggerReleaseSound;
    private AudioSource audioSource;

    private InputDevice rightHand;

    [Header("Desktop Test")]
    public bool useMouseInEditor = true;     // 用滑鼠測試
    public float desktopSpawnDistance = 1.5f; // 生成在鏡頭前方距離

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // ===== 電腦測試：左鍵生成/成長/放開塑形；右鍵戳破 =====
        if (useMouseInEditor)
        {
            // 右鍵：戳破（先做最基本：對著泡泡點就刪掉）
            if (Input.GetMouseButtonDown(1))
            {
                PopBubbleWithMouseRaycast();
            }

            // 左鍵：剛按下 -> 生成
            if (Input.GetMouseButtonDown(0))
            {
                SpawnBubble_Desktop();
            }

            // 左鍵：按住 -> 成長
            if (Input.GetMouseButton(0) && currentBubble != null)
            {
                currentBubble.GrowBubble();
            }

            // 左鍵：放開 -> 塑形 + 播音效
            if (Input.GetMouseButtonUp(0) && currentBubble != null)
            {
                currentBubble.StartShaping();

                if (audioSource != null && triggerReleaseSound != null)
                {
                    audioSource.PlayOneShot(triggerReleaseSound);
                    AudioSource.PlayClipAtPoint(triggerReleaseSound, transform.position, 1f);
                }

                currentBubble = null;
            }

            return; // 用滑鼠時，不跑 VR 流程
        }

        // ===== VR 流程：Trigger 控制 =====
        rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!rightHand.isValid) return;

        rightHand.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue);
        bool triggerPressed = triggerValue > 0.8f;

        // ===== 剛按下 Trigger =====
        if (triggerPressed && !lastTriggerState)
        {
            SpawnBubble_VR();
        }

        // ===== 按住 Trigger：成長 + 持續震動 =====
        if (triggerPressed && currentBubble != null)
        {
            currentBubble.GrowBubble();
            SendHaptics();
        }

        // ===== 放開 Trigger：塑形 + 停止震動 =====
        if (!triggerPressed && lastTriggerState && currentBubble != null)
        {
            currentBubble.StartShaping();

            if (audioSource != null && triggerReleaseSound != null)
            {
                audioSource.PlayOneShot(triggerReleaseSound);
                AudioSource.PlayClipAtPoint(triggerReleaseSound, transform.position, 1f);
            }

            StopHaptics();
            currentBubble = null;
        }

        lastTriggerState = triggerPressed;
    }

    // =========【Desktop：生成泡泡】=========
    void SpawnBubble_Desktop()
    {
        GameObject prefabToSpawn =
            (mainBubble == null) ? mainBubblePrefab : subBubblePrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogError("Prefab 尚未設定！");
            return;
        }

        if (Camera.main == null)
        {
            Debug.LogError("Camera.main 找不到，請確認主攝影機有 MainCamera Tag！");
            return;
        }

        // 生成在鏡頭正前方
        Vector3 spawnPos =
            Camera.main.transform.position +
            Camera.main.transform.forward * desktopSpawnDistance;

        GameObject bubbleObj =
            Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

        BubbleGrowth bg = bubbleObj.GetComponent<BubbleGrowth>();
        if (bg == null)
        {
            Debug.LogError("BubbleGrowth 沒有掛在 prefab 上！");
            Destroy(bubbleObj);
            return;
        }

        if (mainBubble == null)
        {
            bg.isMainBubble = true;
            mainBubble = bg;
        }
        else
        {
            bg.isMainBubble = false;
        }

        currentBubble = bg;
    }

    // =========【VR：生成泡泡】=========
    void SpawnBubble_VR()
    {
        GameObject prefabToSpawn =
            (mainBubble == null) ? mainBubblePrefab : subBubblePrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogError("Prefab 尚未設定！");
            return;
        }

        if (rightHandTransform == null)
        {
            Debug.LogError("rightHandTransform 尚未設定！");
            return;
        }

        Vector3 spawnPos =
            rightHandTransform.position +
            rightHandTransform.forward * 0.15f;

        GameObject bubbleObj =
            Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

        BubbleGrowth bg = bubbleObj.GetComponent<BubbleGrowth>();
        if (bg == null)
        {
            Debug.LogError("BubbleGrowth 沒有掛在 prefab 上！");
            Destroy(bubbleObj);
            return;
        }

        if (mainBubble == null)
        {
            bg.isMainBubble = true;
            mainBubble = bg;
        }
        else
        {
            bg.isMainBubble = false;
        }

        currentBubble = bg;
    }

    // =========【Desktop：右鍵戳破（Raycast 點到泡泡就刪掉）】=========
    void PopBubbleWithMouseRaycast()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            // 允許點到泡泡本體或其子物件
            BubbleGrowth bg = hit.collider.GetComponentInParent<BubbleGrowth>();
            if (bg != null)
            {
                // 如果戳破的是 mainBubble，要清掉主泡泡引用
                if (bg == mainBubble) mainBubble = null;

                // 如果戳破的是正在吹的泡泡，也清掉 currentBubble
                if (bg == currentBubble) currentBubble = null;

                Destroy(bg.gameObject);
            }
        }
    }

    // =========【震動控制】=========
    void SendHaptics()
    {
        if (rightHand.isValid)
        {
            rightHand.SendHapticImpulse(
                0,
                hapticAmplitude,
                hapticDuration
            );
        }
    }

    void StopHaptics()
    {
        if (rightHand.isValid)
        {
            rightHand.StopHaptics();
        }
    }
}
