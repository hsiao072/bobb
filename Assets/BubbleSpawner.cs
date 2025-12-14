using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleSpawner : MonoBehaviour
{
    public GameObject bubblePrefab;
    private BubbleGrowth currentBubble;
    private bool hasBlown = false;  // 是否已經吹過一次

    void Update()
    {
        // 只能吹一次（hasBlown 為 false 才能生成）
        if (!hasBlown)
        {
            // 第一次按下滑鼠 → 生成泡泡
            if (Input.GetMouseButtonDown(0))
            {
                SpawnBubble();
                hasBlown = true;    // 標記：只能吹一次
            }
        }

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

    void SpawnBubble()
    {
        Vector3 spawnPos = new Vector3(0, 1, 2);
        GameObject bubbleObj = Instantiate(bubblePrefab, spawnPos, Quaternion.identity);
        currentBubble = bubbleObj.GetComponent<BubbleGrowth>();

        //⭐️讓攝影機追蹤新生成的泡泡
        Camera.main.GetComponent<CameraFollowBubble>().SetTarget(bubbleObj.transform);
    }
}