using UnityEngine;
using System.Collections;

public class Shockwave : MonoBehaviour
{
    private SpriteRenderer sr;

    public void Initialize(Sprite sprite)
    {
        // ボム発動時のデフォルト設定 [cite: 77, 78]
        InitializeWithCustomScale(sprite, 0f, 0.005f);
    }

    public void InitializeWithCustomScale(Sprite sprite, float startScale, float growSpeed)
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.material = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        sr.color = new Color(1f, 0.2f, 0.2f, 0.2f); // [cite: 77]

        StartCoroutine(ShockRoutine(startScale, growSpeed));
    }

    IEnumerator ShockRoutine(float startScale, float growSpeed)
    {
        float scale = startScale;
        float speed = growSpeed;

         for (int i = 0; i < 60; i++) // loop(180) [cite: 78]
        {
            transform.localScale = new Vector3(scale, scale, 1);

            scale += speed; // Scale += ScaleSpeed [cite: 78]
            speed += 0.005f; // ScaleSpeed += 0.0005 [cite: 78]

            // 徐々にフェードアウト
            Color c = sr.color;
            c.a = Mathf.Max(0, c.a - (0.2f / 180f));
            sr.color = c;

            yield return null;
        }
        Destroy(gameObject);
    }
}