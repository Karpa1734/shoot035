using UnityEngine;
using System.Collections;

public class EnemyBullet : MonoBehaviour
{
    public GameObject originPrefab;
    public GameObject effectPrefab;
    public Sprite delaySprite;

    private SpriteRenderer sr;
    private CircleCollider2D col;
    private BulletData currentData;
    private GameObject activeDelayEffect;

    private float speed, angle, accel, maxSpeed, angularVelocity;
    private bool isInitialized = false;
    private bool isFiring = false;

    // --- 計算方法の変更ポイント：フレーム単位の待機 ---
    private int delayFrameCount = 0;
    private int currentSortingOrder; // 自分の描画順を記憶
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<CircleCollider2D>();
    }

    public void Initialize(float speed, float angle, float accel, float maxSpeed, float angVel, float delay, BulletData data)
    {
        currentData = data;
        this.speed = speed;
        this.angle = angle;
        this.accel = accel;
        this.maxSpeed = maxSpeed;
        this.angularVelocity = angVel;

        sr.sprite = data.bulletSprite;
        sr.color = Color.white;
        if (data.material != null) sr.material = data.material;
        col.radius = data.radius;

        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        // 秒数ではなく「フレーム数」として保存（60fps換算）
        this.delayFrameCount = Mathf.RoundToInt(delay);

        // 遅延演出の開始（見た目だけコルーチンで制御）
        StartCoroutine(DelayEffectRoutine(delay, data));

        // --- 修正ポイント：描画順の決定 ---
        currentSortingOrder = BulletSortingManager.GetNextOrder(data.sizeType);
        sr.sortingOrder = currentSortingOrder;

        sr.sprite = data.bulletSprite;
        sr.color = Color.white;
        if (data.material != null) sr.material = data.material;
        col.radius = data.radius;

        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        this.delayFrameCount = Mathf.RoundToInt(delay);

        StartCoroutine(DelayEffectRoutine(delay, data));
        isInitialized = true;
        isFiring = false;
    }



    // --- 重要：移動処理を FixedUpdate に集約 ---
    void FixedUpdate()
    {
        if (!isInitialized) return;

        // ディレイカウントダウン
        if (delayFrameCount > 0)
        {
            delayFrameCount--;
            return; // まだ動き出さない
        }

        // 動き出しの瞬間
        if (!isFiring)
        {
            isFiring = true;
            sr.enabled = true;
            col.enabled = true;
            if (activeDelayEffect != null) Destroy(activeDelayEffect);
        }

        // 物理的に一定のステップで移動 (1フレーム = 1/60秒固定)
        float dt = 1f / 60f;

        angle += angularVelocity * dt * 60f;
        speed += accel * dt * 60f;

        if (accel > 0 && speed > maxSpeed) speed = maxSpeed;
        if (accel < 0 && speed < maxSpeed) speed = maxSpeed;

        float rad = angle * Mathf.Deg2Rad;
        transform.position += new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * speed * dt;

        // 画面外判定
        if (Mathf.Abs(transform.position.x) > 7f || Mathf.Abs(transform.position.y) > 8f)
            Deactivate(false);
    }

    IEnumerator DelayEffectRoutine(float delayFrames, BulletData data)
    {
        sr.enabled = false;
        col.enabled = false;

        if (delayFrames > 0 && effectPrefab != null && data.delaySprite != null)
        {
            activeDelayEffect = Instantiate(effectPrefab, transform.position, Quaternion.identity);

            // エフェクトを弾のすぐ前面に表示
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
        if (activeDelayEffect != null) Destroy(activeDelayEffect);

        if (playBreakEffect && effectPrefab != null && currentData != null)
        {
            GameObject eff = Instantiate(effectPrefab, transform.position, Quaternion.identity);

            // 消滅エフェクトも弾のすぐ前面に表示
            SpriteRenderer effSr = eff.GetComponent<SpriteRenderer>();
            if (effSr != null) effSr.sortingOrder = currentSortingOrder + 1;

            ShotEffect logic = eff.GetComponent<ShotEffect>();
            if (logic != null)
                logic.StartCoroutine(logic.PlayBreakAnimation(currentData.breakColor, transform.localScale.x));
        }
        isInitialized = false;
        isFiring = false;
        BulletPool.Instance.Release(originPrefab, gameObject);
    }
}