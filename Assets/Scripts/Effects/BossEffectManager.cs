using UnityEngine;
using System.Collections;

public class BossEffectManager : MonoBehaviour
{
    public static BossEffectManager Instance;

    [Header("Prefabs")]
    public GameObject chargeParticlePrefab;
    public GameObject burstParticlePrefab;

    [Header("Burst Default Settings")]
    public float burstMinSpeed = 3.0f;
    public float burstMaxSpeed = 7.0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // --- 修正：引数に spawnPosition を追加 ---
    public void PlayChargeEffect(float duration, Color color, Vector3 spawnPosition)
    {
        StartCoroutine(ChargeRoutine(duration, color, spawnPosition));
    }

    IEnumerator ChargeRoutine(float duration, Color color, Vector3 spawnPosition)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            SpawnChargeParticle(color, spawnPosition);
            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.05f);
        }
    }

    void SpawnChargeParticle(Color color, Vector3 targetPos)
    {
        if (chargeParticlePrefab == null) return;
        // 指定された座標に生成
        GameObject p = Instantiate(chargeParticlePrefab, targetPos, Quaternion.identity);
        // ChargeParticle 側も Vector3 で座標を受け取れるように Initialize を修正
        p.GetComponent<ChargeParticle>()?.Initialize(targetPos, color);
    }

    // --- 修正：引数に spawnPosition を追加 ---
    public void PlayBurstEffect(Color color, int count, Vector3 spawnPosition)
    {
        if (burstParticlePrefab == null) return;

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(burstMinSpeed, burstMaxSpeed);
            Vector3 velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * speed;

            // 指定された座標に生成
            GameObject p = Instantiate(burstParticlePrefab, spawnPosition, Quaternion.identity);

            BurstParticle logic = p.GetComponent<BurstParticle>();
            if (logic != null)
            {
                logic.velocity = velocity;
                var sr = p.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = color;
            }
        }
    }
}