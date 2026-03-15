using KanKikuchi.AudioManager;
using System.Collections;
using UnityEngine;

public class AimedNWayPattern : BossPatternBase
{
    [Header("Bullet Settings")]
    public BulletData bulletData;
    public int nWay = 5;
    public float spreadAngle = 30f;
    public float bulletSpeed = 4f;
    public float fireInterval = 1.2f; // 間隔を少し短く

    [Header("Movement Settings (Kibikibi!)")]
    public float moveMinX = 0.5f;
    public float moveMaxX = 1.5f;
    public float moveMinY = 0.5f;
    public float moveMaxY = 0.5f;

    // --- 重要：ここを調整 ---
    public float moveWeight = 8.0f;

    // 最高速度を思い切って上げます。
    public float moveMaxSpeed = 12.0f;

    private float timer = 0f;
    private bool isMoving = false;

    protected override void Awake()
    {
        base.Awake(); //
    }

    void Update()
    {
        if (status == null || bulletData == null) return; //

        timer += Time.deltaTime;
        if (timer >= fireInterval)
        {
            FireAimedNWay();
            timer = 0f;

            if (!isMoving)
            {
                StartCoroutine(MoveRoutine());
            }
        }
    }

    void FireAimedNWay()
    {
        if (PlayerMove.Instance == null) return; //

        Vector2 diff = PlayerMove.Instance.transform.position - transform.position;
        float baseAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        float startAngle = baseAngle - (spreadAngle / 2f);
        float angleStep = (nWay > 1) ? spreadAngle / (nWay - 1) : 0;

        SEManager.Instance.Play(SEPath.SHOT1, 0.5f);
        for (int i = 0; i < nWay; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            CreateShot(bulletData, transform.position, bulletSpeed, currentAngle); //
        }
    }

    IEnumerator MoveRoutine()
    {
        isMoving = true;

        // 第6引数（maxSpeed）は不要になったので、引数を整理して呼び出し
        yield return StartCoroutine(SetMovePositionRand03(
            moveMinX, moveMaxX,
            moveMinY, moveMaxY,
            moveWeight
        ));

        isMoving = false;
    }
}