using UnityEngine;

public class Shockwave : MonoBehaviour
{
    private SpriteRenderer sr;
    private CircleCollider2D col;
    private float expandSpeed;
    private int damage = 20; // ÕŒ‚”g‚Ìƒ_ƒ[ƒW

    [Header("Screen Shake Settings")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 0.15f;

    // --- C³Fˆø”‚ğ4‚Â (Color shockColor ‚ğ’Ç‰Á) ‚É•ÏX ---
    public void InitializeWithCustomScale(Sprite sprite, Color shockColor, float startScale, float speed ,bool isShake = false)
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<CircleCollider2D>();

        sr.sprite = sprite;
        sr.color = shockColor; // “n‚³‚ê‚½’e‚ÌF‚ğ“K—p
        transform.localScale = Vector3.one * startScale;
        expandSpeed = speed;

        if (col != null) col.isTrigger = true;

        // ‰ÁZ‡¬
        sr.material = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));

        // ‰æ–Ê‚ğ—h‚ç‚·
        if (CameraShake.Instance != null && isShake)
        {
            CameraShake.Instance.Shake(shakeDuration, shakeMagnitude);
        }

        Destroy(gameObject, 1.0f);
    }

    void Update()
    {
        float currentScale = transform.localScale.x + expandSpeed * Time.deltaTime * 60f;
        transform.localScale = Vector3.one * currentScale;

        Color c = sr.color;
        c.a -= 0.02f * Time.deltaTime * 60f;
        sr.color = c;

        if (c.a <= 0) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ‰½‚©‚É“–‚½‚Á‚½‚±‚Æ©‘Ì‚ğŠm”F
        Debug.Log("ÕŒ‚”g‚ª‰½‚©‚ÉÚG: " + collision.gameObject.name);

        if (collision.CompareTag("EnemyBullet"))
        {
            Debug.Log("’e‚Ìƒ^ƒO‚ğŒŸ’mI");
            EnemyBullet bullet = collision.GetComponent<EnemyBullet>();
            if (bullet != null)
            {
                Debug.Log("’e‚ÌÁ–Åˆ—‚ğÀs‚µ‚Ü‚·");
                bullet.Deactivate(true); //
            }
        }

        EnemyStatus enemy = collision.GetComponent<EnemyStatus>();
        if (enemy != null) enemy.TakeDamage(damage, true); //
    }
}