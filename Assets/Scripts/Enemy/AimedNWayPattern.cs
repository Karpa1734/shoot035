using UnityEngine;

public class AimedNWayPattern : MonoBehaviour
{
    [Header("Bullet Settings")]
    public BulletData bulletData;   // GameObject‚Ì‘ã‚í‚è‚ÉBulletData‚ðŽQÆ
    public int nWay = 5;
    public float spreadAngle = 30f;
    public float bulletSpeed = 4f;
    public float fireInterval = 1.5f;

    private Transform player;
    private float timer = 0f;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null || bulletData == null) return;

        timer += Time.deltaTime;
        if (timer >= fireInterval)
        {
            FireAimedNWay();
            timer = 0f;
        }
    }

    void FireAimedNWay()
    {
        Vector2 diff = player.position - transform.position;
        float baseAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        float startAngle = baseAngle - (spreadAngle / 2f);
        float angleStep = (nWay > 1) ? spreadAngle / (nWay - 1) : 0;

        for (int i = 0; i < nWay; i++)
        {
            float currentAngle = startAngle + (angleStep * i);

            // --- C³Fˆø”‚ð3‚Â“n‚· (prefab, position, rotation) ---
            GameObject bullet = BulletPool.Instance.Get(
                bulletData.bulletPrefab,
                transform.position,
                Quaternion.identity
            );

            EnemyBullet script = bullet.GetComponent<EnemyBullet>();
            if (script != null)
            {
                script.Initialize(bulletSpeed, currentAngle, 0, bulletSpeed, 0, 0, bulletData);
            }
        }
    }
}