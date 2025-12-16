using UnityEngine;

public class BubbleGrowth : MonoBehaviour
{
    // ====== 身分 ======
    public bool isMainBubble = false;
    
    // ✨ 新增：主泡泡追蹤
    private static BubbleGrowth mainBubbleInstance;  // 靜態變數儲存主泡泡
    public float attractionForce = 0.8f;              // 被主泡泡吸引的力道
    public float attractionStartDistance = 10f;       // 多遠開始被吸引
    public float attractionMaxDistance = 20f;         // 超過這距離不受影響
    
    // ✨ 新增：重新生成主泡泡
    public GameObject mainBubblePrefab;               // 主泡泡的 Prefab
    public Vector3 respawnOffset = Vector3.zero;      // 重生位置偏移

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

    private Vector3 originalScale;
    private float timeSinceRelease = 0f;

    // =========【狀態機】=========
    private enum BubbleState { Growing, Shaping, Floating }
    private BubbleState state = BubbleState.Growing;

    private Rigidbody rb;
    private SphereCollider col;

    // =========【漂浮】=========
    public float upwardForce = 1.2f;
    public float verticalDriftAmplitude = 1.5f;
    public float verticalDriftSpeed = 1.5f;
    public float maxFloatHeight = 2.0f;
    
    // ✨ 新增：水平漂移參數
    public float horizontalDriftForce = 2.0f;      // 水平漂移力道（增大！）
    public float driftChangeInterval = 3.5f;       // 多久改變一次方向（拉長！）
    private Vector3 currentDriftDirection;         // 當前漂移方向
    private float driftTimer = 0f;
    
    // ✨ 新增：隨機高度參數
    public float minFloatHeight = 0.5f;            // 最低高度
    public float verticalRangeFromStart = 5.0f;    // 垂直活動範圍
    private float targetHeight;                     // 目標高度（每個泡泡隨機）
    private float minTargetHeight;                  // 最低目標高度
    private float maxTargetHeight;                  // 最高目標高度

    void Start()
    {
        originalScale = transform.localScale;
        
        // ✨ 如果是主泡泡，註冊自己
        if (isMainBubble)
        {
            mainBubbleInstance = this;
        }
    }

    void Update()
    {
        // ✨ 右鍵點擊檢測
        if (Input.GetMouseButtonDown(1)) // 1 = 右鍵
        {
            CheckRightClickOnBubble();
        }
        
        if (state == BubbleState.Shaping)
            HandleShaping();
        else if (state == BubbleState.Floating)
            FloatAround();
    }
    
    // ✨ 檢查右鍵是否點到泡泡
    void CheckRightClickOnBubble()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            // 檢查點到的是不是這個泡泡
            if (hit.collider.gameObject == gameObject)
            {
                DestroyAndRespawn();
            }
        }
    }
    
    // ✨ 銷毀並重新生成
    void DestroyAndRespawn()
    {
        if (isMainBubble)
        {
            // 記錄重生位置
            Vector3 respawnPosition = transform.position + respawnOffset;
            
            // 如果有設定 Prefab，就生成新的主泡泡
            if (mainBubblePrefab != null)
            {
                GameObject newMainBubble = Instantiate(mainBubblePrefab, respawnPosition, Quaternion.identity);
                
                // 確保新泡泡也是主泡泡
                BubbleGrowth newBubbleScript = newMainBubble.GetComponent<BubbleGrowth>();
                if (newBubbleScript != null)
                {
                    newBubbleScript.isMainBubble = true;
                    Debug.Log("主泡泡已重新生成！");
                }
            }
            else
            {
                Debug.LogWarning("未設定 mainBubblePrefab！請在 Inspector 中指定主泡泡 Prefab。");
            }
        }
        
        // 銷毀這個泡泡
        Destroy(gameObject);
    }

    // =========【成長】=========
    public void GrowBubble()
    {
        if (state != BubbleState.Growing) return;

        Vector3 next = transform.localScale + Vector3.one * growthSpeed * Time.deltaTime;
        if (next.x <= maxSize)
        {
            transform.localScale = next;
            originalScale = transform.localScale;
        }
    }

    // =========【塑形】=========
    public void StartShaping()
    {
        state = BubbleState.Shaping;
        shapingTimer = shapingTime;

        if (TryGetComponent<Rigidbody>(out Rigidbody r)) Destroy(r);
        if (TryGetComponent<SphereCollider>(out SphereCollider c)) Destroy(c);

        originalScale = transform.localScale;
    }

    void HandleShaping()
    {
        shapingTimer -= Time.deltaTime;

        timeSinceRelease += Time.deltaTime;
        float wave = Mathf.Sin(timeSinceRelease * waveFrequency)
                     * Mathf.Exp(-damping * timeSinceRelease)
                     * waveAmplitude;

        transform.localScale = originalScale + Vector3.one * wave;

        if (shapingTimer <= 0)
            StartFloating();
    }

    // =========【開始漂浮】=========
    public void StartFloating()
    {
        state = BubbleState.Floating;

        col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = false;
        col.radius = 0.5f;

        PhysicsMaterial bubbleMat = new PhysicsMaterial();
        bubbleMat.bounciness = 0.0f;
        bubbleMat.dynamicFriction = 0.0f;
        bubbleMat.staticFriction = 0.0f;

        col.material = bubbleMat;

        rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.mass = 0.01f;
        rb.linearDamping = 1.5f;            // ✨ 降低阻尼，讓泡泡飄更遠
        rb.angularDamping = 1.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // ✨ 設定這個泡泡的垂直活動範圍
        float currentY = transform.position.y;
        minTargetHeight = Mathf.Max(currentY - 2f, minFloatHeight);
        maxTargetHeight = currentY + verticalRangeFromStart;
        
        // 初始目標高度
        targetHeight = Random.Range(minTargetHeight, maxTargetHeight);
        
        RandomizeDriftDirection();
    }

    // ✨ 隨機改變漂移方向
    void RandomizeDriftDirection()
    {
        // 產生隨機的 XZ 平面方向（水平方向）
        float angle = Random.Range(0f, 360f);
        currentDriftDirection = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            0f,
            Mathf.Sin(angle * Mathf.Deg2Rad)
        ).normalized;
    }

    // =========【漂浮邏輯】=========
    void FloatAround()
    {
        if (rb == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Rigidbody 不存在！");
            return;
        }

        // ✨ 如果不是主泡泡，且主泡泡存在，則被吸引過去
        if (!isMainBubble && mainBubbleInstance != null)
        {
            Vector3 toMainBubble = mainBubbleInstance.transform.position - transform.position;
            float distance = toMainBubble.magnitude;
            
            // 在一定距離內才會被吸引
            if (distance < attractionMaxDistance && distance > 0.5f)
            {
                // 距離越近，吸引力越強
                float attractionStrength = attractionForce;
                
                // 在 attractionStartDistance 內吸引力線性增強
                if (distance < attractionStartDistance)
                {
                    attractionStrength *= (attractionStartDistance - distance) / attractionStartDistance + 1f;
                }
                
                Vector3 attractionDirection = toMainBubble.normalized;
                rb.AddForce(attractionDirection * attractionStrength, ForceMode.Acceleration);
            }
        }

        // ✨ 定期改變水平漂移方向
        driftTimer += Time.deltaTime;
        if (driftTimer >= driftChangeInterval)
        {
            RandomizeDriftDirection();
            
            // ✨ 同時也換個新的目標高度，讓泡泡上下飄動
            targetHeight = Random.Range(minTargetHeight, maxTargetHeight);
            
            driftTimer = 0f;
        }

        // ✨ 水平漂移力（XZ平面）
        // 如果不是主泡泡，漂移力會比較小（因為主要是被吸引）
        float driftMultiplier = isMainBubble ? 1f : 0.4f;
        Vector3 horizontalForce = currentDriftDirection * horizontalDriftForce * driftMultiplier;
        rb.AddForce(horizontalForce, ForceMode.Acceleration);

        // 垂直波動
        float verticalWave = Mathf.Sin(Time.time * verticalDriftSpeed) * verticalDriftAmplitude;
        
        // ✨ 根據目標高度調整上升力（改進版）
        float currentHeight = transform.position.y;
        float heightDifference = targetHeight - currentHeight;
        
        // 基礎上升力（總是向上，抵抗重力）
        float baseUpwardForce = upwardForce;
        
        // 根據距離目標的差距微調（但不要讓力變成負的）
        if (currentHeight < targetHeight)
        {
            // 低於目標：增加上升力
            baseUpwardForce += heightDifference * 0.3f;
        }
        else if (currentHeight > targetHeight + 0.5f)
        {
            // 高於目標較多：減少上升力，但保持正值
            baseUpwardForce = Mathf.Max(0.2f, upwardForce - (currentHeight - targetHeight) * 0.3f);
        }
        
        rb.AddForce(Vector3.up * (baseUpwardForce + verticalWave), ForceMode.Acceleration);

        // ✨ 防止飛太高（使用最大高度）
        if (transform.position.y > maxFloatHeight)
            rb.AddForce(Vector3.down * 2.0f, ForceMode.Acceleration);
    }

    // =========【吸收其他泡泡】=========
    void OnCollisionEnter(Collision collision)
    {
        if (!isMainBubble) return;

        BubbleGrowth otherBubble = collision.gameObject.GetComponent<BubbleGrowth>();

        if (otherBubble == null) return;
        if (otherBubble == this) return;
        if (otherBubble.isMainBubble) return;

        Absorb(otherBubble);
    }

    void Absorb(BubbleGrowth other)
    {
        float otherSize = other.transform.localScale.x;

        // 根據被吸收泡泡大小成長
        transform.localScale += Vector3.one * (otherSize * 0.3f);
        originalScale = transform.localScale;

        Destroy(other.gameObject);
    }
    
    // ✨ 清理靜態引用
    void OnDestroy()
    {
        if (isMainBubble && mainBubbleInstance == this)
        {
            mainBubbleInstance = null;
        }
    }
}