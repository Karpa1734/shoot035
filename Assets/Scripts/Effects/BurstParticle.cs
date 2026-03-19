using UnityEngine;

public class BurstParticle : MonoBehaviour
{
    [Header("動きの設定")]
    public Vector3 velocity;
    [Tooltip("回転速度の範囲")]
    public float rotationSpeed = 360f;
    [Tooltip("生存時間")]
    public float lifespan = 1.0f;

    private float timer = 0f;
    private SpriteRenderer spriteRenderer;
    private Vector3 currentRotation;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // 爆発の破片っぽさを出すため、ランダムな軸で回転させる
        currentRotation = new Vector3(
            Random.Range(-rotationSpeed, rotationSpeed),
            Random.Range(-rotationSpeed, rotationSpeed),
            Random.Range(-rotationSpeed, rotationSpeed)
        );
    }

    void Update()
    {
        // 1. 移動 (Space.Worldで直進させる)
        transform.position += velocity * Time.deltaTime;

        // 2. 回転
        transform.Rotate(currentRotation * Time.deltaTime);

        // 3. 生存時間とフェードアウト
        timer += Time.deltaTime;
        if (spriteRenderer != null)
        {
            // 残り時間に応じてアルファ値を下げる
            float alpha = 1.0f - (timer / lifespan);
            Color newColor = spriteRenderer.color;
            newColor.a = Mathf.Max(0f, alpha);
            spriteRenderer.color = newColor;
        }

        // 4. 削除
        if (timer >= lifespan)
        {
            Destroy(gameObject);
        }
    }
}