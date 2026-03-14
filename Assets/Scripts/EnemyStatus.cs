using System;
using UnityEngine;

public class EnemyStatus : MonoBehaviour
{
    [Header("Life Settings")]
    public float maxHP = 1000f;
    [NonSerialized] public float currentHP;

    [Header("Marker Settings")]
    // マーカーが点滅を開始するHPのしきい値 [cite: 244]
    public float flickerLifeThreshold = 200f;

    void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(float damage)
    {
        if (currentHP <= 0) return;
        currentHP -= damage;

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    void Die()
    {
        // 敵が消える際、マーカー側は Update の null チェックで自動的に非アクティブになります
        Debug.Log("Enemy Defeated");
        Destroy(gameObject);
    }
}