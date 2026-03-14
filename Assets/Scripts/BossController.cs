using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    [Header("References")]
    public GameObject bulletPrefab;
    public EnemyStatus status;

    // 子オブジェクトにあるSpriteRendererをここに割り当てる
    public SpriteRenderer bossSpriteRenderer;

    [Header("Sprite Settings")]
    public Sprite idleSprite;
    public Sprite movingSprite;
    public bool faceLeftByDefault = true;

    [Header("Bullet Patterns Data")]
    public BulletData aimedShotData;
    public BulletData spiralShotData;

    [Header("Movement Settings")]
    public Vector2 moveAreaMin = new Vector2(-3.5f, 1.0f);
    public Vector2 moveAreaMax = new Vector2(3.5f, 4.5f);
    public float defaultWeight = 20f;
    public float defaultMaxSpeed = 5.0f;

    private bool isMoving = false;
    private Vector3 lastPosition;

    void Start()
    {
        if (status == null) status = GetComponent<EnemyStatus>();

        // --- 修正：自分にない場合は「子オブジェクト」から探す ---
        if (bossSpriteRenderer == null)
        {
            bossSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        lastPosition = transform.position;
        StartCoroutine(MainPatternRoutine());
    }

    void Update()
    {
        if (bossSpriteRenderer == null) return;

        // 1. スプライトの切り替え
        if (isMoving)
        {
            if (movingSprite != null) bossSpriteRenderer.sprite = movingSprite;
        }
        else
        {
            if (idleSprite != null) bossSpriteRenderer.sprite = idleSprite;
        }

        // 2. 左右反転の処理
        Vector3 movementDelta = transform.position - lastPosition;
        if (movementDelta.x > 0.001f)
            bossSpriteRenderer.flipX = faceLeftByDefault;
        else if (movementDelta.x < -0.001f)
            bossSpriteRenderer.flipX = !faceLeftByDefault;

        lastPosition = transform.position;
    }

    IEnumerator MainPatternRoutine()
    {
        while (status != null && status.currentHP > 0)
        {
            yield return StartCoroutine(AimedNWay(3, 30f, 5f, aimedShotData));
            yield return new WaitForSeconds(0.8f);

            // 移動
            yield return StartCoroutine(SetMovePositionRand03(1.0f, 2.0f, 0.3f, 0.6f, 15f, 6.0f));
            yield return new WaitForSeconds(0.8f);
        }
    }

    // --- 減速移動（フラグ管理） ---
    public IEnumerator SetMovePosition03(float tx, float ty, float weight, float maxSpeed)
    {
        isMoving = true; // 移動開始

        Vector3 target = new Vector3(tx, ty, 0);
        float distance = Vector3.Distance(transform.position, target);

        while (distance > 0.01f)
        {
            Vector3 diff = target - transform.position;
            distance = diff.magnitude;
            float moveSpeed = Mathf.Min(maxSpeed, distance / Mathf.Max(0.1f, weight / 60f));
            transform.position += diff.normalized * moveSpeed * Time.deltaTime;
            yield return null;
            distance = Vector3.Distance(transform.position, target);
        }
        transform.position = target;

        isMoving = false; // 移動終了
    }

    // --- 以下、以前のロジック ---
    public IEnumerator SetMovePositionRand03(float minX, float maxX, float minY, float maxY, float weight, float maxSpeed)
    {
        Vector3 currentPos = transform.position;
        float targetX, targetY;
        float moveY = Random.Range(minY, maxY);
        if (Random.value > 0.5f)
        {
            targetY = currentPos.y + moveY;
            if (targetY > moveAreaMax.y) targetY = currentPos.y - moveY;
        }
        else
        {
            targetY = currentPos.y - moveY;
            if (targetY < moveAreaMin.y) targetY = currentPos.y + moveY;
        }
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
        yield return StartCoroutine(SetMovePosition03(targetX, targetY, weight, maxSpeed));
    }

    IEnumerator AimedNWay(int count, float angleRange, float speed, BulletData data)
    {
        if (PlayerMove.Instance == null) yield break;
        Vector3 dir = PlayerMove.Instance.transform.position - transform.position;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float startAngle = baseAngle - (angleRange / 2f);
        float step = (count > 1) ? angleRange / (count - 1) : 0;
        for (int i = 0; i < count; i++)
        {
            DanmakuFunctions.CreateShotA1(bulletPrefab, data, transform.position, speed, startAngle + (step * i), 15);
        }
    }
}