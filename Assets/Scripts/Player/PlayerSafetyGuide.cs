using UnityEngine;

public class PlayerSafetyGuide : MonoBehaviour
{
    [Header("AI Logic Settings")]
    public float searchRadius = 3.0f;     // 弾を検知する範囲
    public float repulsionPower = 5.0f;   // 斥力の強さ

    [Header("Visual Settings")]
    public LineRenderer guideLine;        // 安全方向を示すライン
    public float lineLength = 0.8f;       // ガイド線の長さ
    public Color safeColor = new Color(0f, 1f, 1f, 0.6f);
    [Header("Visual Smoothing")]
    public float smoothSpeed = 10f; // 値が小さいほどゆっくり動く（5～15推奨）

    private Vector3 currentVelocity; // SmoothDamp用（オプション）
    private Vector3 smoothedDir;    // 現在表示中の滑らかなベクトル
    private PlayerTimeSlowAI slowAI;

    void Start()
    {
        slowAI = GetComponent<PlayerTimeSlowAI>();
        if (guideLine != null)
        {
            guideLine.positionCount = 2;
            guideLine.startColor = guideLine.endColor = safeColor;
            guideLine.enabled = false;
        }
    }

    void Update()
    {
        if (Time.timeScale < 0.98f && !Input.GetKey(KeyCode.Z))
        {
            Vector2 targetDir = CalculateSafeDirection();
            if (targetDir != Vector2.zero)
            {
                // --- ★修正：急激な回転を防ぐために補間を入れる ---
                // 現在の向きから目標の向きへ、smoothSpeedの速さで回転させる
                smoothedDir = Vector3.Slerp(smoothedDir, targetDir, Time.unscaledDeltaTime * smoothSpeed);

                UpdateGuideVisual(smoothedDir);
            }
        }
        else
        {
            if (guideLine != null) guideLine.enabled = false;
            // ガイドが消えている間も、次に備えてsmoothedDirを初期化しておく
            smoothedDir = Vector2.up;
        }
    }

    Vector2 CalculateSafeDirection()
    {
        Vector2 totalRepulsion = Vector2.zero;
        Collider2D[] bullets = Physics2D.OverlapCircleAll(transform.position, searchRadius);

        foreach (var col in bullets)
        {
            if (col.CompareTag("EnemyBullet") || col.CompareTag("Laser"))
            {
                Vector2 diff = (Vector2)transform.position - (Vector2)col.ClosestPoint(transform.position);
                float dist = diff.magnitude;
                float force = repulsionPower / Mathf.Max(dist * dist, 0.01f);
                totalRepulsion += diff.normalized * force;
            }
        }

        totalRepulsion += CalculateScreenBoundaryForce();

        // --- ★追加：画面外へのベクトルをクリッピング ---
        if (PlayerMove.Instance != null)
        {
            // 下端にいて、ベクトルが下を向いているなら、Y成分を0にする
            if (transform.position.y <= PlayerMove.Instance.minY + 0.1f && totalRepulsion.y < 0)
                totalRepulsion.y = 0;

            // 上端にいて、ベクトルが上を向いているなら、Y成分を0にする
            if (transform.position.y >= PlayerMove.Instance.maxY - 0.1f && totalRepulsion.y > 0)
                totalRepulsion.y = 0;

            // 左端にいて、ベクトルが左を向いているなら、X成分を0にする
            if (transform.position.x <= PlayerMove.Instance.minX + 0.1f && totalRepulsion.x < 0)
                totalRepulsion.x = 0;

            // 右端にいて、ベクトルが右を向いているなら、X成分を0にする
            if (transform.position.x >= PlayerMove.Instance.maxX - 0.1f && totalRepulsion.x > 0)
                totalRepulsion.x = 0;
        }

        // クリッピングした後に正規化することで、壁に沿った方向（横など）が強調される
        return totalRepulsion.normalized;
    }

    Vector2 CalculateScreenBoundaryForce()
    {
        Vector2 force = Vector2.zero;
        float margin = 1.0f;           // 壁の影響を感じ始める距離（少し広めに設定）
        float boundaryWeight = 10.0f;    // 壁の最大反発力（repulsionPower 5.0 に対して 3.0 程度でバランスを取る）

        if (PlayerMove.Instance == null) return Vector2.zero;

        float minX = PlayerMove.Instance.minX;
        float maxX = PlayerMove.Instance.maxX;
        float minY = PlayerMove.Instance.minY;
        float maxY = PlayerMove.Instance.maxY;

        // --- 左端 ---
        if (transform.position.x < minX + margin)
        {
            // 0.0 (マージン端) ～ 1.0 (壁ギリギリ) の比率を計算
            float t = 1.0f - Mathf.Clamp01((transform.position.x - minX) / margin);
            force.x += t * boundaryWeight;
        }
        // --- 右端 ---
        else if (transform.position.x > maxX - margin)
        {
            float t = 1.0f - Mathf.Clamp01((maxX - transform.position.x) / margin);
            force.x -= t * boundaryWeight;
        }

        // --- 下端 ---
        if (transform.position.y < minY + margin)
        {
            float t = 1.0f - Mathf.Clamp01((transform.position.y - minY) / margin);
            force.y += t * boundaryWeight;
        }
        // --- 上端 ---
        else if (transform.position.y > maxY - margin)
        {
            float t = 1.0f - Mathf.Clamp01((maxY - transform.position.y) / margin);
            force.y -= t * boundaryWeight;
        }

        return force;
    }
    void UpdateGuideVisual(Vector2 direction)
    {
        if (guideLine == null) return;

        guideLine.enabled = true;
        guideLine.SetPosition(0, transform.position);
        guideLine.SetPosition(1, transform.position + (Vector3)direction * lineLength);
    }
}