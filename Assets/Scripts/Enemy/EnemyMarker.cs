using UnityEngine;

public class EnemyMarker : MonoBehaviour
{
    [Header("References")]
    public EnemyStatus targetStatus; // 追跡対象（ボスなど）
    public SpriteRenderer sr;

    [Header("Display Settings")]
    public float bottomY = -4.5f;    // 画面下端のY座標

    private float flickerTimer = 0f;

    void Start()
    {
        // 1. スプライトレンダラーの自動取得
        if (sr == null) sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // ターゲットがいない、または非アクティブなら何もしない
        if (targetStatus == null || !targetStatus.gameObject.activeInHierarchy)
        {
            if (sr != null) sr.enabled = false;
            return;
        }

        // 2. 位置の更新 (敵のX座標に追従、Yは固定)
        float targetX = targetStatus.transform.position.x;
        transform.position = new Vector3(targetX, bottomY, 0);

        // 3. 画面外判定：左右のプレイエリア外なら表示を消す
        if (targetX < -6f || targetX > 2f)
        {
            sr.enabled = false;
            return; // 画面外なら計算不要
        }
        else
        {
            sr.enabled = true;
        }

        // 4. 透明度の計算：自機との水平距離で変化
        float alpha = 0.6f; // デフォルト値
        if (PlayerMove.Instance != null)
        {
            float distToPlayer = Mathf.Abs(targetX - PlayerMove.Instance.transform.position.x);
            // 弾幕風の式：近いと薄く、遠いと濃く (144 ~ 255 の範囲を 0.0 ~ 1.0 に変換)
            alpha = (144f + distToPlayer * 10f) / 255f;
        }
        alpha = Mathf.Clamp(alpha, 0.4f, 1.0f);

        // 5. ライフに応じた点滅処理
        Color finalColor = Color.white;
        if (targetStatus.currentHP < targetStatus.flickerLifeThreshold)
        {
            // 残りライフが少ないほど点滅を速くする
            float flickerSpeed = 5f + (1f - targetStatus.currentHP / targetStatus.flickerLifeThreshold) * 15f;
            flickerTimer += Time.deltaTime * flickerSpeed;

            // 周期的に白と黒を切り替える
            if (Mathf.Sin(flickerTimer * Mathf.PI) < 0)
                finalColor = Color.black;
        }

        // 最終的な色と透明度を適用
        sr.color = new Color(finalColor.r, finalColor.g, finalColor.b, alpha);
    }
}