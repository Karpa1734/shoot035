using UnityEngine;

public class BossMagicCircle : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer sr;

    [Header("Rotation Settings")]
    public float spinSpeed = 0.8f;

    [Header("TH14 Lean Settings")]
    public float lean = 28f;

    private float anglez = 0f;
    private float scale2 = 0f; // 輝針城仕様の進行度変数
    private bool isRunning = false;

    void Start()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            // Built-inで光らせるための設定
            Shader additiveShader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (additiveShader == null) additiveShader = Shader.Find("Sprites/Default");
            sr.material = new Material(additiveShader);
            sr.color = new Color(1f, 1f, 1f, 144f / 255f);

            // 最初は描画をオフにして、初期化によるチラつきを防ぐ
            sr.enabled = false;
        }

        transform.localScale = Vector3.zero;
        StartAppearance();
    }

    void Update()
    {
        if (!isRunning || Mathf.Approximately(Time.timeScale, 0f)) return;

        // 1. 回転計算 (th14仕様の3D傾き)
        anglez -= spinSpeed;
        float anglex = lean - lean * Mathf.Cos(anglez * Mathf.Deg2Rad);
        float angley = lean - lean * Mathf.Sin(anglez * Mathf.Deg2Rad);
        transform.localRotation = Quaternion.Euler(anglex, angley, anglez);

        // 2. スケール処理 (th14の数式を完全再現)
        UpdateScale();
    }

    void UpdateScale()
    {
        float scale3, scale1;

        // 出現から60フレームまで
        if (scale2 < 90f)
        {
            // 毎フレーム 1.5度ずつ増加 (90/60)
            scale2 += 90f / 60f;

            // 拡大中の計算式
            scale3 = 0.90f * Mathf.Sin(scale2 * Mathf.Deg2Rad);
            scale1 = 0.10f * Mathf.Sin(scale2 * Mathf.Deg2Rad);
        }
        else
        {
            // 待機状態 (脈動)
            // 毎フレーム 3度ずつ増加 (360/120)
            scale2 += 360f / 120f;

            // 拡大しきった後の計算式
            scale3 = 0.90f; // 0.9で固定
            scale1 = 0.10f * Mathf.Sin(scale2 * Mathf.Deg2Rad); // 0.1の幅で揺れる
        }

        // 合計スケールを適用 (これで 60フレーム目に 0.9+0.1=1.0 になり、そのまま滑らかに脈動へ移る)
        float finalScale = scale3 + scale1;
        transform.localScale = new Vector3(finalScale, finalScale, 1.0f);

        // 初めて計算されたら表示を開始
        if (!sr.enabled) sr.enabled = true;
    }

    public void StartAppearance()
    {
        scale2 = 0f;
        isRunning = true;
    }
}