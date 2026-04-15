using UnityEngine;

public class PlayerTimeSlowAI : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 3.0f;
    public float minSlowRadius = 0.5f;
    public float slowTimeScale = 0.2f;
    public float transitionSpeed = 10f;

    private float targetTimeScale = 1.0f;
    private float defaultFixedDeltaTime;
    private bool wasPausedLastFrame = false;

    // プレイヤーの状態でスローを無効化するために追加
    private PlayerHitHandler hitHandler;

    void Start()
    {
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        // 子オブジェクトからHitHandlerを探す
        hitHandler = GetComponentInChildren<PlayerHitHandler>();
    }

    void Update()
    {
        // 1. ポーズ解除の復帰処理
        if (wasPausedLastFrame && Time.timeScale > 0f)
        {
            Time.timeScale = targetTimeScale;
            wasPausedLastFrame = false;
        }

        if (Time.timeScale <= 0f)
        {
            wasPausedLastFrame = true;
            return;
        }

        // 2. 危険度判定とスローの決定
        DetectDangerGradient();

        // 3. 反映
        ApplyTimeScale();
    }

    void DetectDangerGradient()
    {
        // ★追加：ショット中（Zキー）はスローを適用しない
        if (Input.GetKey(KeyCode.Z))
        {
            targetTimeScale = 1.0f;
            return;
        }

        // ★追加：被弾中（通常状態以外）はスローを適用しない
        if (hitHandler != null && hitHandler.currentState != PlayerHitHandler.PlayerState.Normal)
        {
            targetTimeScale = 1.0f;
            return;
        }

        Collider2D[] bullets = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        if (bullets.Length == 0)
        {
            targetTimeScale = 1.0f;
            return;
        }

        float minDistance = detectionRadius;
        foreach (var col in bullets)
        {
            if (col.CompareTag("EnemyBullet") || col.CompareTag("Laser"))
            {
                float dist = Vector2.Distance(transform.position, col.ClosestPoint(transform.position));
                if (dist < minDistance) minDistance = dist;
            }
        }

        float t = Mathf.InverseLerp(minSlowRadius, detectionRadius, minDistance);
        targetTimeScale = Mathf.Lerp(slowTimeScale, 1.0f, t);
    }

    void ApplyTimeScale()
    {
        // ★修正：ショット中や被弾時は Lerp を待たずに「即座に」速度 1 に戻す
        bool forceInstant = Input.GetKey(KeyCode.Z) ||
                           (hitHandler != null && hitHandler.currentState != PlayerHitHandler.PlayerState.Normal);

        if (forceInstant)
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = defaultFixedDeltaTime;
        }
        else
        {
            float newScale = Mathf.Lerp(Time.timeScale, targetTimeScale, Time.unscaledDeltaTime * transitionSpeed);
            if (targetTimeScale >= 1.0f && newScale > 0.99f) newScale = 1.0f;

            Time.timeScale = newScale;
            Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
        }

        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // スローが深いほど色が濃くなる
            float intensity = Mathf.InverseLerp(1.0f, slowTimeScale, Time.timeScale);
            sr.color = Color.Lerp(Color.white, new Color(0.4f, 0.4f, 1f, 1f), intensity);
        }
    }
}