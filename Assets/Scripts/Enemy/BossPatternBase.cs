using UnityEngine;
using System.Collections;

public abstract class BossPatternBase : MonoBehaviour
{
    protected BossController controller;
    protected EnemyStatus status;

    // 画面左側（戦場）の中心座標
    protected readonly float CENTER_X = -2.0f;
    protected readonly float CENTER_Y = 0.0f;

    // 移動可能範囲（CENTER_X を基準に設定）
    protected Vector2 moveAreaMin = new Vector2(-4.5f, 1.5f);
    protected Vector2 moveAreaMax = new Vector2(0.5f, 3.5f);

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
    // --- BossPatternBase.cs ---

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

            // --- 修正ポイント：イージング（Sin関数）の適用 ---
            // これを入れることで、移動の終わりにかけて滑らかに減速します。
            // (Sine Out と呼ばれる、東方Project等の弾幕STGでよく使われる手法です)
            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            // 補間された t を使って座標を移動
            transform.parent.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        transform.parent.position = targetPos;
        if (controller != null) controller.SetMoving(false);
    }
    public IEnumerator SetMovePositionRand03(float minX, float maxX, float minY, float maxY, float weight)
    {
        Vector3 currentPos = transform.parent.position;
        float targetX, targetY;

        // --- 修正ポイント：Y軸の移動ロジック ---
        // 移動量（moveY）を先に決め、現在の高さが移動可能範囲の中央より上か下かで、進む方向を強制的に変えます
        float moveY = Random.Range(minY, maxY);
        float centerY = (moveAreaMin.y + moveAreaMax.y) * 0.5f;

        // 中央より高い位置にいれば「下」へ、低い位置にいれば「上」へ動かす
        targetY = (currentPos.y > centerY) ? currentPos.y - moveY : currentPos.y + moveY;

        // X軸の移動（自機の位置を見て、近づく方向に移動）
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

        // 3. 上限下限のクランプ（移動可能範囲を越えないようにする）
        targetX = Mathf.Clamp(targetX, moveAreaMin.x, moveAreaMax.x);
        targetY = Mathf.Clamp(targetY, moveAreaMin.y, moveAreaMax.y);

        yield return StartCoroutine(SetMovePosition03(targetX, targetY, weight));
    }
}