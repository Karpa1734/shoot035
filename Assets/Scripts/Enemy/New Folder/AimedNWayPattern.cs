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

    private bool isMoving = false;
    private Coroutine fireRoutine;
    protected override void Awake()
    {
        base.Awake(); //
    }
    void OnEnable()
    {
        // オブジェクトが有効になったら発射開始
        if (fireRoutine != null) StopCoroutine(fireRoutine);
        fireRoutine = StartCoroutine(FireRoutine());
    }

    IEnumerator FireRoutine()
    {
        // 最初の待機時間が必要ならここに入れる
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            FireAimedNWay();

            // 移動処理もリズムに合わせて制御可能
            if (!isMoving) StartCoroutine(MoveRoutine());

            // 指定した秒数待機
            yield return new WaitForSeconds(fireInterval);
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
            CreateShot(bulletData, transform.position, bulletSpeed, currentAngle,10); //
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