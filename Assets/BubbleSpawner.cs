using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleSpawner : MonoBehaviour
{
    public GameObject bubblePrefab;
    private BubbleGrowth currentBubble;
    //private bool hasBlown = false;  // 是否已經吹過一次
    private BubbleGrowth mainBubble;   // ★ 主要泡泡

    void Update()
    {
        // 只能吹一次（hasBlown 為 false 才能生成）
        /*if (!hasBlown)
        {
            // 第一次按下滑鼠 → 生成泡泡
            if (Input.GetMouseButtonDown(0))
            {
                SpawnBubbleAtMouse();
                hasBlown = true;    // 標記：只能吹一次
            }
        }*/

        if (Input.GetMouseButtonDown(0))
            SpawnBubbleAtMouse();

        // 如果已有泡泡（正被吹）
        if (currentBubble != null)
        {
            // 按住滑鼠 → 成長
            if (Input.GetMouseButton(0))
            {
                currentBubble.GrowBubble();
            }

            // 放開滑鼠 → 停止成長 & 開始漂浮
            if (Input.GetMouseButtonUp(0))
            {
                //currentBubble.StartFloating();
                
                currentBubble.StartShaping();
                currentBubble = null;   // 之後不再操作
            }
        }
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