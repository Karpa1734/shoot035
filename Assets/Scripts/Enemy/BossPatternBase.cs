using UnityEngine;
using System.Collections;

public abstract class BossPatternBase : MonoBehaviour
{
    protected BossController controller;
    protected EnemyStatus status;

    // 画面左側（戦場）の中心座標
    protected readonly float CENTER_X = -2.0f;
    protected readonly float CENTER_Y = 3.0f;

    // 移動可能範囲（CENTER_X を基準に設定）
    protected Vector2 moveAreaMin = new Vector2(-5.5f, 1.0f);
    protected Vector2 moveAreaMax = new Vector2(1.5f, 4.5f);

    protected virtual void Awake()
    {
        controller = GetComponentInParent<BossController>();
        status = GetComponentInParent<EnemyStatus>();
    }

    // --- 弾生成関数 ---
    protected void CreateShot(BulletData data, Vector3 position, float speed, float angle)
    {
        if (data == null || BulletPool.Instance == null) return;
        GameObject bullet = BulletPool.Instance.Get(data.bulletPrefab, position, Quaternion.identity);
        EnemyBullet eb = bullet.GetComponent<EnemyBullet>();
        if (eb != null) eb.Initialize(speed, angle, 0, speed, 0, 0, data);
    }

    // --- 旧 BossController から移植：減速移動 ---
    public IEnumerator SetMovePosition03(float tx, float ty, float weight, float maxSpeed)
    {
        if (controller != null) controller.SetMoving(true);

        Vector3 target = new Vector3(tx, ty, 0);
        float distance = Vector3.Distance(transform.parent.position, target);

        while (distance > 0.01f)
        {
            Vector3 diff = target - transform.parent.position;
            // 物理的に一定のステップで移動 (1/60秒固定)
            float moveSpeed = Mathf.Min(maxSpeed, diff.magnitude / Mathf.Max(0.1f, weight / 60f));
            transform.parent.position += diff.normalized * moveSpeed * Time.deltaTime;

            yield return null;
            distance = Vector3.Distance(transform.parent.position, target);
        }
        transform.parent.position = target;

        if (controller != null) controller.SetMoving(false);
    }

    // --- 旧 BossController から移植：ランダム移動先計算 ---
    public IEnumerator SetMovePositionRand03(float minX, float maxX, float minY, float maxY, float weight, float maxSpeed)
    {
        Vector3 currentPos = transform.parent.position;
        float targetX, targetY;

        // Y軸の移動
        float moveY = Random.Range(minY, maxY);
        targetY = (Random.value > 0.5f) ? currentPos.y + moveY : currentPos.y - moveY;

        // X軸の移動（プレイヤーの位置を考慮）
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

        // 範囲内にクランプ
        targetX = Mathf.Clamp(targetX, moveAreaMin.x, moveAreaMax.x);
        targetY = Mathf.Clamp(targetY, moveAreaMin.y, moveAreaMax.y);

        yield return StartCoroutine(SetMovePosition03(targetX, targetY, weight, maxSpeed));
    }
}