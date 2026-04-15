using UnityEngine;

public class BulletGhost : MonoBehaviour
{
    private SpriteRenderer sr;
    private float lifetime;
    private float timer;
    private Color startColor;

    public void Initialize(Sprite sprite, Color color, float duration, int sortingOrder)
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder - 1; // 弾本体より背面に表示
        startColor = color;
        startColor.a = 0.5f; // 半透明からスタート
        sr.color = startColor;

        lifetime = duration;
        timer = 0;
    }

    void Update()
    {
        timer += Time.unscaledDeltaTime; // スロー中でも一定速度で消えるように
        float progress = timer / lifetime;

        Color c = sr.color;
        c.a = Mathf.Lerp(startColor.a, 0f, progress);
        sr.color = c;

        if (progress >= 1f)
        {
            // ここも本来は専用のプールに戻すのが理想
            Destroy(gameObject);
        }
    }
}