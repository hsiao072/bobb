using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class BubbleSpawner : MonoBehaviour
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

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // 取得右手控制器
        rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!rightHand.isValid) return;

        // 讀取 trigger
        rightHand.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue);
        bool triggerPressed = triggerValue > 0.8f;

        // ===== 剛按下 Trigger =====
        if (triggerPressed && !lastTriggerState)
        {
            SpawnBubble();
        }

        // ===== 按住 Trigger：成長 + 持續震動 =====
        if (triggerPressed && currentBubble != null)
        {
            currentBubble.GrowBubble();
            SendHaptics(); // ⭐ 持續震動
        }

        // ===== 放開 Trigger：塑形 + 停止震動 =====
        if (!triggerPressed && lastTriggerState && currentBubble != null)
        {
            
            currentBubble.StartShaping();
            audioSource.PlayOneShot(triggerReleaseSound);
            AudioSource.PlayClipAtPoint(
                triggerReleaseSound,
                transform.position,
                1f
            );
            StopHaptics();
            currentBubble = null;
        }

        lastTriggerState = triggerPressed;
    }

    // =========【生成泡泡】=========
    void SpawnBubble()
    {
        GameObject prefabToSpawn =
            (mainBubble == null) ? mainBubblePrefab : subBubblePrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogError("Prefab 尚未設定！");
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
