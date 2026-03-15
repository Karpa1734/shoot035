using UnityEngine;
using TMPro;
using System.Collections;

public class BossTimerUI : MonoBehaviour
{
    public static BossTimerUI Instance;
    [Header("References")]
    public EnemyStatus targetStatus;
    public TextMeshProUGUI timerText;
    public CanvasGroup canvasGroup;

    [Header("Color Settings")]
    private Color normalColor = Color.white;
    private Color warningColor = new Color(1f, 128f / 255f, 128f / 255f);
    private Color dangerColor = new Color(1f, 64f / 255f, 64f / 255f);

    [Header("Position Settings")]
    public float worldCenterX = -2.0f;

    // --- 修正ポイント：位置の切り替え ---
    public float normalTopOffset = 50f;   // 通常時
    public float spellTopOffset = 100f;  // スペルカード時（少し下げる）
    private float currentTargetOffset;   // 現在目標としているオフセット

    [Header("Animation (th13 Slide)")]
    private float t_count = 0;
    private RectTransform rectTransform;
    private Camera mainCamera;

    private int lastIntSecond = -1;
    private Vector3 originalScale;

    void Awake()
    {
        if (Instance == null) Instance = this;
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;
        canvasGroup.alpha = 0f;
        originalScale = rectTransform.localScale;
        currentTargetOffset = normalTopOffset; // 初期値

        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    // --- 外部（EnemyStatus）からフェーズの種類を教えてもらう ---
    public void SetPhaseType(PhaseType type)
    {
        if (type == PhaseType.SpellCard || type == PhaseType.Endurance)
            currentTargetOffset = spellTopOffset;
        else
            currentTargetOffset = normalTopOffset;
    }

    void Update()
    {
        // ターゲットが完全にいなくなった時（ボス撃破時）のみ消える
        if (targetStatus == null)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, Time.deltaTime * 2f);
            t_count = 0;
            lastIntSecond = -1;
            return;
        }

        // --- 修正ポイント：移行中（値が一時的に0）でも、ターゲットがいれば消さない ---
        // 初回出現時のみ、フェードインを開始する
        if (canvasGroup.alpha < 1f && targetStatus.currentTimer > 0f)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.deltaTime * 3f);
        }

        // 座標計算
        Vector3 worldPos = new Vector3(worldCenterX, 0, 0);
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        float uiPosX = screenPos.x - (Screen.width / 2f);

        // 1. 出現アニメーション
        if (t_count < 90f) t_count += Time.deltaTime * 120f;
        float py = -40f * Mathf.Sin(t_count * Mathf.Deg2Rad) + 40f;

        // --- 修正ポイント：currentTargetOffset に向かって滑らかに移動させる ---
        float smoothedOffset = Mathf.Lerp(rectTransform.anchoredPosition.y, -currentTargetOffset + py, Time.deltaTime * 3f);
        rectTransform.anchoredPosition = new Vector2(uiPosX, smoothedOffset);
        // 2. UIの更新
        UpdateUI(targetStatus.currentTimer);

        // 3. 10秒以下の特殊演出
        int currentIntSecond = Mathf.FloorToInt(targetStatus.currentTimer);
        if (targetStatus.currentTimer <= 10.5f && currentIntSecond != lastIntSecond && targetStatus.currentTimer > 0)
        {
            if (currentIntSecond < 10)
            {
                StartCoroutine(PopRoutine());
                // SE再生（省略）
            }
            lastIntSecond = currentIntSecond;
        }
    }

    IEnumerator PopRoutine()
    {
        float duration = 0.15f;
        float elapsed = 0f;
        Vector3 popScale = originalScale * 1.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rectTransform.localScale = Vector3.Lerp(originalScale, popScale, elapsed / duration);
            yield return null;
        }
        rectTransform.localScale = originalScale;
    }
    void UpdateUI(float time)
    {
        if (time > 99f) time = 99.99f;
        int sec = Mathf.FloorToInt(time);
        int ms = Mathf.FloorToInt((time * 100f) % 100f);
        timerText.text = string.Format("{0:00}<size=70%>.{1:00}</size>", sec, ms);

        if (time < 5f) timerText.color = dangerColor;
        else if (time < 10f) timerText.color = warningColor;
        else timerText.color = normalColor;
    }
}