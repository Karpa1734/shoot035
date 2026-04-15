using UnityEngine;
using System.Collections.Generic;

public class PlayerAISupport : MonoBehaviour
{
    [Header("AI Settings")]
    public float detectionRadius = 2.0f; // ’e‚ğŒŸ’m‚·‚é”ÍˆÍ
    public float repulsionStrength = 5.0f; // ‰ñ”ğ‚Ì‹­‚³
    public float safetyWeight = 0.5f;   // ƒvƒŒƒCƒ„[“ü—Í‚É‘Î‚·‚éAI‚Ì‰î“ü“x

    private PlayerMove playerMove;

    void Start()
    {
        playerMove = GetComponent<PlayerMove>();
    }

    // PlayerMove‚ÌFixedUpdate‚Ì’¼‘O‚ÉŒvZ‚ğs‚¤
    public Vector2 GetAIAdjustment()
    {
        Vector2 totalRepulsion = Vector2.zero;

        // 1. ü•Ó‚Ì’e‚ğŒŸ’miƒŒƒCƒ„[İ’è‚ğ„§j
        Collider2D[] bullets = Physics2D.OverlapCircleAll(transform.position, detectionRadius);

        foreach (var col in bullets)
        {
            if (col.CompareTag("EnemyBullet")) // ’e‚Ì”»’è
            {
                Vector2 diff = (Vector2)transform.position - (Vector2)col.transform.position;
                float distance = diff.magnitude;

                if (distance < 0.1f) distance = 0.1f; // ƒ[ƒœZ–h~

                // 2. ‹——£‚Ì‹t”‚ÉŠî‚Ã‚­Ë—Í‚ğŒvZ
                // ‹——£‚ª‹ß‚¢‚Ù‚Ç‹}Œƒ‚É‘å‚«‚È—Í‚ª“­‚­
                totalRepulsion += diff.normalized * (repulsionStrength / Mathf.Pow(distance, 2));
            }
        }

        return totalRepulsion * safetyWeight;
    }
}