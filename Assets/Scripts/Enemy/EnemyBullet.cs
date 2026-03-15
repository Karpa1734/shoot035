using UnityEngine;
using System.Collections;

public class EnemyBullet : MonoBehaviour
{
    public GameObject originPrefab;
    public GameObject effectPrefab;

    private SpriteRenderer sr;
    private CircleCollider2D col;
    private BulletData currentData;
    private GameObject activeDelayEffect;

    private float speed, angle, accel, maxSpeed, angularVelocity;
    private bool isInitialized = false;
    private bool isFiring = false;
    private bool isActive = true;

    private int delayFrameCount = 0;
    private int currentSortingOrder;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<CircleCollider2D>();
    }

    // --- シンプルな弾幕スクリプト（SpiralPattern等）から呼ばれる用 ---
    public void SetVelocity(Vector2 v)
    {
        // ベクトルから速度と角度を逆算して、内部パラメータに同期させる
        speed = v.magnitude;
        angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;

        // 進行方向に向ける（画像が上向き前提なら -90f）
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        isInitialized = true;
        isFiring = true; // SetVelocity時は遅延なしで即発射
        isActive = true;
        delayFrameCount = 0;

        if (sr != null) sr.enabled = true;
        if (col != null) col.enabled = true;
    }

    // --- 高度な弾幕生成（Danmakufu風）から呼ばれる用 ---
    public void Initialize(float speed, float angle, float accel, float maxSpeed, float angVel, float delay, BulletData data)
    {
        currentData = data;
        this.speed = speed;
        this.angle = angle;
        this.accel = accel;
        this.maxSpeed = maxSpeed;
        this.angularVelocity = angVel;

        // 見た目と当たり判定の設定（重複を整理）
        sr.sprite = data.bulletSprite;
        sr.color = Color.white;
        if (data.material != null) sr.material = data.material;
        col.radius = data.radius;

        // 描画順の決定
        currentSortingOrder = BulletSortingManager.GetNextOrder(data.sizeType);
        sr.sortingOrder = currentSortingOrder;

        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        // 遅延フレーム設定
        this.delayFrameCount = Mathf.RoundToInt(delay);

        if (delay > 0)
        {
            StartCoroutine(DelayEffectRoutine(delay, data));
            isFiring = false;
        }
        else
        {
            isFiring = true;
            sr.enabled = true;
            col.enabled = true;
        }

        isInitialized = true;
        isActive = true;
    }

    void FixedUpdate()
    {
        if (!isInitialized || !isActive) return;

        // ディレイカウントダウン
        if (delayFrameCount > 0)
        {
            delayFrameCount--;
            return;
        }

        // 動き出しの瞬間
        if (!isFiring)
        {
            isFiring = true;
            sr.enabled = true;
            col.enabled = true;
            if (activeDelayEffect != null) Destroy(activeDelayEffect);
        }

        // 物理計算 (1/60秒固定ステップ)
        float dt = 1f / 60f;

        angle += angularVelocity * dt * 60f;
        speed += accel * dt * 60f;

        // 最高速度制限
        if (accel > 0 && speed > maxSpeed) speed = maxSpeed;
        if (accel < 0 && speed < maxSpeed) speed = maxSpeed;

        float rad = angle * Mathf.Deg2Rad;
        Vector3 moveVec = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * speed * dt;
        transform.position += moveVec;

        // 画面外判定
        if (Mathf.Abs(transform.position.x) > 10f || Mathf.Abs(transform.position.y) > 10f)
            Deactivate(false);
    }

    IEnumerator DelayEffectRoutine(float delayFrames, BulletData data)
    {
        sr.enabled = false;
        col.enabled = false;

        if (delayFrames > 0 && effectPrefab != null && data.delaySprite != null)
        {
            activeDelayEffect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
            SpriteRenderer effSr = activeDelayEffect.GetComponent<SpriteRenderer>();
            if (effSr != null)
            {
                effSr.sortingOrder = currentSortingOrder + 1;
                effSr.sprite = data.delaySprite;
            }

            ShotEffect logic = activeDelayEffect.GetComponent<ShotEffect>();
            if (logic != null)
                logic.StartCoroutine(logic.PlayDelay(delayFrames / 60f, data.delaySprite, transform.localScale.x));
        }
        yield return null;
    }

    public void Deactivate(bool playBreakEffect)
    {
        isActive = false;
        if (activeDelayEffect != null) Destroy(activeDelayEffect);

        if (playBreakEffect && effectPrefab != null && currentData != null)
        {
            GameObject eff = Instantiate(effectPrefab, transform.position, Quaternion.identity);
            SpriteRenderer effSr = eff.GetComponent<SpriteRenderer>();
            if (effSr != null) effSr.sortingOrder = currentSortingOrder + 1;

            ShotEffect logic = eff.GetComponent<ShotEffect>();
            if (logic != null)
                logic.StartCoroutine(logic.PlayBreakAnimation(currentData.breakColor, transform.localScale.x));
        }

        isInitialized = false;
        isFiring = false;

        if (BulletPool.Instance != null && originPrefab != null)
        {
            // 自分のコピー元(originPrefab)を指定してプールに戻す
            BulletPool.Instance.Release(originPrefab, gameObject);
        }
        else
        {
            Destroy(gameObject); // プールがない、またはプレハブ指定がない場合は破壊
        }
    }
}