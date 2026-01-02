using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;


public class BubbleSpawner : MonoBehaviour
{
    public GameObject bubblePrefab;
    private BubbleGrowth currentBubble;
    //private bool hasBlown = false;  // 是否已經吹過一次
    private BubbleGrowth mainBubble;   // ★ 主要泡泡
    private bool lastTriggerState = false;

    void Update()
{
    InputDevice rightHand =
        InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

    if (!rightHand.isValid) return;

    rightHand.TryGetFeatureValue(
        CommonUsages.trigger,
        out float triggerValue
    );

    bool triggerPressed = triggerValue > 0.8f;

    // === 剛按下 Trigger ===
    if (triggerPressed && !lastTriggerState)
    {
        SpawnBubbleAtMouse();
    }

    // === 按住 Trigger ===
    if (triggerPressed && currentBubble != null)
    {
        currentBubble.GrowBubble();
    }

    // === 放開 Trigger ===
    if (!triggerPressed && lastTriggerState && currentBubble != null)
    {
        currentBubble.StartShaping();
        currentBubble = null;
    }

    lastTriggerState = triggerPressed;
}


void SpawnBubbleAtMouse()
{
    Debug.Log("Spawn bubble");

    if (bubblePrefab == null || Camera.main == null)
    {
        Debug.LogError("bubblePrefab 或 Camera.main 是 null");
        return;
    }

    // ★ 強制生成在鏡頭正前方 ★
    Vector3 worldPos =
        Camera.main.transform.position +
        Camera.main.transform.forward * 1.5f;

    GameObject bubbleObj = Instantiate(bubblePrefab, worldPos, Quaternion.identity);
    BubbleGrowth bg = bubbleObj.GetComponent<BubbleGrowth>();
    if (bg == null) return;

    Debug.Log("Bubble instantiated at " + worldPos);

    currentBubble = bubbleObj.GetComponent<BubbleGrowth>();

    if (currentBubble == null)
        Debug.LogError("BubbleGrowth 沒有掛在 prefab 上！");

    if (mainBubble == null)
    {
        bg.isMainBubble = true;
        mainBubble = bg;
    }

    currentBubble = bg;
}



    void SpawnBubble()
    {
        Vector3 spawnPos = new Vector3(0, 1, 2);
        GameObject bubbleObj = Instantiate(bubblePrefab, spawnPos, Quaternion.identity);
        currentBubble = bubbleObj.GetComponent<BubbleGrowth>();

    }
}