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

    // 復帰用に元の値を保持する変数
    private float originalShotRate;
    private float originalBombRate;
    private Collider2D parentCollider;
    private SpriteRenderer bossRenderer;
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

    /// <summary>
    /// ステルス状態（透明化＋当たり判定消失）に移行する
    /// </summary>
    /// <param name="fadeDuration">透明化にかかる時間</param>
    /// <param name="targetAlpha">最終的な透明度 (0で完全に消える)</param>
    protected IEnumerator FadeToStealth(float fadeDuration, float targetAlpha = 0f)
    {
        if (status == null) yield break;

        // 初回実行時にコンポーネント等を取得
        if (parentCollider == null) parentCollider = GetComponentInParent<Collider2D>();
        if (bossRenderer == null) bossRenderer = transform.parent.GetComponentInChildren<SpriteRenderer>();

        // 1. 当たり判定（ショット・ボム・自機衝突）を完全に消す
        if (parentCollider != null) parentCollider.enabled = false;

        // 2. ショットが当たった際の音などを出さないよう、ダメージレートを0にする
        // (※現在のフェーズのレートを一時的に書き換える)
        // originalShotRate = status.phases[status.currentPhaseIndex].shotDamageRate; // 構造体の場合は直接書き換えに注意が必要なため、以下のロジックが安全です

        // 3. 透明化アニメーション
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

    /// <summary>
    /// ステルス状態から復帰する
    /// </summary>
    protected IEnumerator FadeToVisible(float fadeDuration)
    {
        if (bossRenderer != null)
        {
            float elapsed = 0f;
            Color startColor = bossRenderer.color;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(startColor.a, 1f, elapsed / fadeDuration);
                bossRenderer.color = new Color(startColor.r, startColor.g, startColor.b, a);
                yield return null;
            }
        }

        // 当たり判定を戻す
        if (parentCollider != null) parentCollider.enabled = true;
    }

    // パターンオブジェクトが破棄（フェーズ終了）されるときに自動で復帰させる
    protected virtual void OnDestroy()
    {
        // 強制的に元の状態に戻す
        if (parentCollider != null) parentCollider.enabled = true;
        if (bossRenderer != null)
        {
            bossRenderer.color = new Color(bossRenderer.color.r, bossRenderer.color.g, bossRenderer.color.b, 1f);
        }
    }
}