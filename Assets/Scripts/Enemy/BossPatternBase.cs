using UnityEngine;
using System.Collections;

public abstract class BossPatternBase : MonoBehaviour
{
    protected BossController controller;
    protected EnemyStatus status;

    protected readonly float CENTER_X = -2.0f;
    protected readonly float CENTER_Y = 0.0f;

    protected Vector2 moveAreaMin = new Vector2(-4.5f, 1.5f);
    protected Vector2 moveAreaMax = new Vector2(0.5f, 3.5f);

    protected Collider2D parentCollider;
    protected SpriteRenderer bossRenderer;

    protected virtual void Awake()
    {
        controller = GetComponentInParent<BossController>();
        status = GetComponentInParent<EnemyStatus>();
    }

    // --- 弾生成関数（DanmakuFunctions を使用するように変更） ---

    /// <summary>
    /// 単発弾を生成する (CreateShotA1 を使用)
    /// </summary>
    protected GameObject CreateShot(BulletData data, Vector3 position, float speed, float angle, float delay = 0)
    {
        if (data == null) return null;
        // DanmakuFunctions の A1 形式で生成
        return DanmakuFunctions.CreateShotA1(data.bulletPrefab, data, position, speed, angle, delay);
    }

    /// <summary>
    /// 全方位弾（リング）を生成する
    /// </summary>
    protected void CreateRoundShot(BulletData data, Vector3 position, int count, float speed, float startAngle, float delay = 0)
    {
        if (data == null) return;
        // DanmakuFunctions の全方位弾関数を呼び出す
        DanmakuFunctions.RoundShot01(data.bulletPrefab, data, position, count, speed, startAngle, delay);
    }

    /// <summary>
    /// 全方位に加速・回転する弾を発射する (RoundShot02)
    /// </summary>
    protected void CreateRoundShot02(BulletData data, Vector3 position, int count, float speed, float accel, float maxSpeed, float angVel, float startAngle, float delay = 0)
    {
        if (data == null) return;
        DanmakuFunctions.RoundShot02(data.bulletPrefab, data, position, count, speed, accel, maxSpeed, angVel, startAngle, delay);
    }

    // --- 移動・ステルス関連のメソッド（既存のものを維持） ---

    public IEnumerator SetMovePosition03(float tx, float ty, float weight)
    {
        if (controller != null) controller.SetMoving(true);
        Vector3 startPos = transform.parent.position;
        Vector3 targetPos = new Vector3(tx, ty, 0);
        float duration = Mathf.Max(0.01f, weight / 60.0f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.parent.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.parent.position = targetPos;
        if (controller != null) controller.SetMoving(false);
    }

    public IEnumerator SetMovePositionRand03(float minX, float maxX, float minY, float maxY, float weight)
    {
        Vector3 currentPos = transform.parent.position;
        float moveY = Random.Range(minY, maxY);
        float centerY = (moveAreaMin.y + moveAreaMax.y) * 0.5f;
        float targetY = (currentPos.y > centerY) ? currentPos.y - moveY : currentPos.y + moveY;

        float targetX;
        if (PlayerMove.Instance != null)
        {
            float playerX = PlayerMove.Instance.transform.position.x;
            float moveX = Random.Range(minX, maxX);
            targetX = (playerX > currentPos.x) ? currentPos.x + moveX : currentPos.x - moveX;
        }
        else
        {
            targetX = currentPos.x + Random.Range(-maxX, maxX);
        }

        targetX = Mathf.Clamp(targetX, moveAreaMin.x, moveAreaMax.x);
        targetY = Mathf.Clamp(targetY, moveAreaMin.y, moveAreaMax.y);

        yield return StartCoroutine(SetMovePosition03(targetX, targetY, weight));
    }

    protected IEnumerator FadeToStealth(float fadeDuration, float targetAlpha = 0f)
    {
        if (parentCollider == null) parentCollider = GetComponentInParent<Collider2D>();
        if (bossRenderer == null) bossRenderer = transform.parent.GetComponentInChildren<SpriteRenderer>();

        if (parentCollider != null) parentCollider.enabled = false;

        if (bossRenderer != null)
        {
            float elapsed = 0f;
            Color startColor = bossRenderer.color;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(startColor.a, targetAlpha, elapsed / fadeDuration);
                bossRenderer.color = new Color(startColor.r, startColor.g, startColor.b, a);
                yield return null;
            }
        }
    }

    protected virtual void OnDestroy()
    {
        if (parentCollider != null) parentCollider.enabled = true;
        if (bossRenderer != null)
        {
            bossRenderer.color = new Color(bossRenderer.color.r, bossRenderer.color.g, bossRenderer.color.b, 1f);
        }
    }
}