using UnityEngine;

public class BubbleGrowth : MonoBehaviour
{
    // =========【成長屬性】=========
    public float growthSpeed = 1.0f;
    public float maxSize = 1.5f;

    // =========【塑形屬性】=========
    public float shapingTime = 1.5f;
    private float shapingTimer = 0f;

    public float dragSensitivity = 0.005f;
    public float waveAmplitude = 0.15f;
    public float waveFrequency = 8f;
    public float damping = 3f;
    public float maxScale = 3f;
    public float minScale = 0.3f;

    private bool isDragging = false;
    private Vector3 lastMousePosition;
    private Vector3 targetScale;
    private Vector3 originalScale;
    private float timeSinceRelease = 0f;
    private Renderer rend;

    // =========【狀態機】=========
    private enum BubbleState { Growing, Shaping, Floating }
    private BubbleState state = BubbleState.Growing;

    private Rigidbody rb;
    private SphereCollider col;
    private bool isFloating = false;

    // =========【漂浮屬性】=========
    public float upwardForce = 0.3f;

    // 新增：上下飄動
    public float verticalDriftAmplitude = 0.2f;
    public float verticalDriftSpeed = 1.5f;

    // 新增：高度限制（VR 頭高 1.9m → 泡泡限制 2.0m）
    public float maxFloatHeight = 2.0f;  // 你可以調整（如 1.8f 或 2.2f）

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        rend = GetComponent<Renderer>();
        isFloating = false;
    }

    void Update()
    {
        switch (state)
        {
            case BubbleState.Growing:
                break;

            case BubbleState.Shaping:
                HandleShaping();
                break;

            case BubbleState.Floating:
                FloatAround();
                break;
        }
    }

    // =========【成長函式】=========
    public void GrowBubble()
    {
        if (state != BubbleState.Growing) return;

        Vector3 next = transform.localScale + Vector3.one * growthSpeed * Time.deltaTime;

        if (next.x < maxSize)
        {
            transform.localScale = next;
            originalScale = transform.localScale; // 記住長大後的大小
        }
    }

    // =========【進入塑形階段】=========
    public void StartShaping()
    {
        state = BubbleState.Shaping;
        shapingTimer = shapingTime;

        // 移除物理
        if (TryGetComponent<Rigidbody>(out Rigidbody r)) Destroy(r);
        if (TryGetComponent<SphereCollider>(out SphereCollider c)) Destroy(c);

        originalScale = transform.localScale;
    }

    // =========【塑形階段邏輯】=========
    void HandleShaping()
    {
        shapingTimer -= Time.deltaTime;

        // 滑鼠按下 → 檢查是否按到泡泡
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == transform)
                {
                    isDragging = true;
                    lastMousePosition = Input.mousePosition;
                    rend.material.color = Color.Lerp(Color.white, Color.cyan, 0.5f);
                }
            }
        }

        // 滑鼠放開 → 結束捏造型
        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                isDragging = false;
                timeSinceRelease = 0f;
            }
        }

        // ==========【拖曳中：捏造型】==========
        if (isDragging)
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

            float changeX = mouseDelta.x * dragSensitivity;
            float changeY = mouseDelta.y * dragSensitivity;

            targetScale = transform.localScale;
            targetScale.x = Mathf.Clamp(targetScale.x + changeX, minScale, maxScale);
            targetScale.y = Mathf.Clamp(targetScale.y + changeY, minScale, maxScale);

            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 0.5f);

            lastMousePosition = Input.mousePosition;
        }
        else
        {
            // ==========【放開後：果凍彈動】==========
            timeSinceRelease += Time.deltaTime;

            float wave = Mathf.Sin(timeSinceRelease * waveFrequency) *
                         Mathf.Exp(-damping * timeSinceRelease) *
                         waveAmplitude;

            transform.localScale = originalScale + new Vector3(wave, -wave, wave);
        }

        // 塑形時間結束 → 開始漂浮
        if (shapingTimer <= 0)
        {
            StartFloating();
        }
    }

    // =========【開始漂浮】=========
    public void StartFloating()
    {
        state = BubbleState.Floating;

        col = gameObject.AddComponent<SphereCollider>();
        col.radius = 0.5f;

        PhysicsMaterial bubbleMat = new PhysicsMaterial();
        bubbleMat.bounciness = 1f;
        bubbleMat.bounceCombine = PhysicsMaterialCombine.Maximum;
        col.material = bubbleMat;

        rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.mass = 0.05f;
        rb.linearDamping = 0.5f;
    }

    // =========【漂浮邏輯】=========
    void FloatAround()
    {
        isFloating = true;
        if (rb == null) return;

        // *** 新增：上下波動 ***
        float verticalWave = Mathf.Sin(Time.time * verticalDriftSpeed) * verticalDriftAmplitude;
        rb.AddForce(new Vector3(0, verticalWave * 0.5f, 0), ForceMode.Acceleration);

        // *** 左右前後漂浮 ***
        Vector3 drift = new Vector3(
            Mathf.Sin(Time.time * 1.3f) * 0.3f,
            0,
            Mathf.Cos(Time.time * 1.1f) * 0.3f
        );
        rb.AddForce(drift * 0.2f);
    }

    void FixedUpdate()
    {
        if (isFloating && rb != null)
        {
            float height = transform.position.y;

            // =========【高度限制】=========
            if (height >= maxFloatHeight)
            {
                // 超過高度 → 施加向下力量
                rb.AddForce(Vector3.down * 1.2f, ForceMode.Acceleration);
            }
            else
            {
                // 未超過 → 正常浮力
                rb.AddForce(Vector3.up * upwardForce, ForceMode.Acceleration);
            }
        }
    }
}
