using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float highSpeed = 4.5f;
    [SerializeField] private float lowSpeed = 2.0f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 inputVec;

    [Header("Movement Bounds")]
    public float minX = -4.0f;
    public float maxX = 4.0f;
    public float minY = -4.5f;
    public float maxY = 4.5f;

    [Header("Status Timers")]
    private float invincibleTimer = 0f;
    private float deathBombTimer = 0f;

    [Header("Ghost Effect Settings")]
    public GameObject ghostPrefab;      // BulletGhostプレハブを割り当て
    public float ghostDistance = 0.2f; // どのくらい動いたら残像を出すか
    public float ghostDuration = 0.4f; // 残像が消えるまでの時間
    private Vector3 lastGhostPos;       // 最後に残像を出した位置

    public bool IsInvincible => invincibleTimer > 0;
    public bool IsInDeathBombWindow => deathBombTimer > 0;

    public static PlayerMove Instance { get; private set; }

    void Awake()
    {
        Time.timeScale = 1f;
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        // 残像の初期位置を同期
        lastGhostPos = transform.position;
    }

    void Update()
    {
        inputVec.x = Input.GetAxisRaw("Horizontal");
        inputVec.y = Input.GetAxisRaw("Vertical");

        if (invincibleTimer > 0) invincibleTimer -= Time.deltaTime;
        if (deathBombTimer > 0) deathBombTimer -= Time.deltaTime;
    }

    void LateUpdate()
    {
        // 1. 無敵時の色更新（既存のロジックを保持）
        if (IsInvincible)
        {
            UpdateInvincibleVisual();
        }
        else
        {
            if (sr != null && sr.color != Color.white)
            {
                ResetVisual();
            }
        }

        // 2. ★追加：残像生成ロジック
        // スロー中かつショットを撃っていない時のみ動作
        if (Time.timeScale < 0.95f && !Input.GetKey(KeyCode.Z))
        {
            float dist = Vector3.Distance(transform.position, lastGhostPos);
            if (dist > ghostDistance)
            {
                SpawnGhost();
                lastGhostPos = transform.position;
            }
        }
        else
        {
            // スロー中でない時は、位置だけ更新し続けて生成をスキップ
            lastGhostPos = transform.position;
        }
    }

    void FixedUpdate()
    {
        float speed = Input.GetKey(KeyCode.LeftShift) ? lowSpeed : highSpeed;
        Vector2 velocity = inputVec.normalized * speed;
        Vector2 nextPosition = rb.position + velocity * Time.fixedDeltaTime;
        nextPosition.x = Mathf.Clamp(nextPosition.x, minX, maxX);
        nextPosition.y = Mathf.Clamp(nextPosition.y, minY, maxY);
        rb.MovePosition(nextPosition);
    }

    // --- ★追加：残像生成メソッド ---
    private void SpawnGhost()
    {
        if (ghostPrefab == null || sr == null) return;

        // 残像を生成
        GameObject g = Instantiate(ghostPrefab, transform.position, transform.rotation);
        BulletGhost ghostScript = g.GetComponent<BulletGhost>();

        if (ghostScript != null)
        {
            // 現在の自機のスプライト、色、描画順を渡す
            // 無敵中の青い色もそのまま残像に反映される
            ghostScript.Initialize(sr.sprite, sr.color, ghostDuration, sr.sortingOrder);
        }
    }

    public void SetInvincible(float duration)
    {
        invincibleTimer = duration;
        deathBombTimer = 0f;
    }

    public void StartDeathBombWindow(float duration)
    {
        if (!IsInvincible) deathBombTimer = duration;
    }

    private void UpdateInvincibleVisual()
    {
        if (sr == null) return;
        float pingPong = Mathf.PingPong(Time.time * 20f, 1f);
        float alpha = 0.3f + pingPong * 0.7f;
        sr.color = Color.Lerp(new Color(0.4f, 0.4f, 1f, alpha), new Color(1f, 1f, 1f, alpha), pingPong);
    }

    private void ResetVisual()
    {
        if (sr == null) return;
        sr.color = Color.white;
    }
}