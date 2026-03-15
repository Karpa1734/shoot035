using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BossCircularHealth : MonoBehaviour
{
    // ピンの情報を管理するための内部構造体
    private struct MarkerInfo
    {
        public GameObject obj;
        public float threshold;
    }
    private List<MarkerInfo> activeMarkers = new List<MarkerInfo>(); // 管理用リスト
    [Header("UI References")]
    public Image healthFillImage;
    public RectTransform markerParent;
    public GameObject markerPrefab;
    public CanvasGroup canvasGroup;

    [Header("Target Settings")]
    private EnemyStatus targetEnemy;
    private RectTransform rectTransform;
    private Camera mainCamera;

    [Header("Appearance Settings")]
    public float radius = 60f;
    public Vector3 offset = Vector3.zero;
    public float appearDuration = 1.0f;

    [Header("Marker Settings")]
    public Vector3 markerScale = new Vector3(0.5f, 0.5f, 1f);

    private bool isAppearing = true;



    // 座標同期を共通化
    private void SyncPosition()
    {
        if (targetEnemy == null || mainCamera == null) return;

        // ワールド座標をスクリーン座標に変換して代入
        Vector3 screenPos = mainCamera.WorldToScreenPoint(targetEnemy.transform.position + offset);
        rectTransform.position = screenPos;
    }

    public void Initialize(EnemyStatus enemy, List<float> thresholds)
    {
        targetEnemy = enemy;
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;

        // 生成された瞬間に必ず透明＆空にする
        if (healthFillImage != null) healthFillImage.fillAmount = 0f;
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        SyncPosition();
        InitializeMarkers(thresholds);
        markerParent.gameObject.SetActive(false);

        // ステルス状態で座標が確定してから表示
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        StartCoroutine(AppearRoutine());
    }

    IEnumerator AppearRoutine()
    {
        isAppearing = true;
        float elapsed = 0f;

        // 溜まりきる目標値 (通常は1.0)
        float finalRatio = targetEnemy.maxHP > 0 ? targetEnemy.currentHP / targetEnemy.maxHP : 1f;

        while (elapsed < appearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / appearDuration;
            // イージングをかけるとより東方らしくなります (例: t * t)
            healthFillImage.fillAmount = Mathf.Lerp(0f, finalRatio, t);
            yield return null;
        }

        healthFillImage.fillAmount = finalRatio;
        markerParent.gameObject.SetActive(true);
        isAppearing = false;
    }
    void Update()
    {
        if (targetEnemy == null)
        {
            Destroy(gameObject);
            return;
        }

        if (!isAppearing)
        {
            // --- 修正ポイント：バー全体の比率を取得 ---
            float barMax = targetEnemy.GetBarTotalMaxHP();
            float barCurrent = targetEnemy.GetBarCurrentHP();

            float ratio = barMax > 0 ? barCurrent / barMax : 0;
            healthFillImage.fillAmount = ratio;
            CheckMarkers(ratio);
        }
        SyncPosition();
    }
    // 現在の比率としきい値を比較してピンを消す
    void CheckMarkers(float currentRatio)
    {
        // 逆順でループ（削除操作を行うため）
        for (int i = activeMarkers.Count - 1; i >= 0; i--)
        {
            // 体力比率がピンの位置（しきい値）を下回ったら削除
            if (currentRatio <= activeMarkers[i].threshold)
            {
                // 単に消すだけでなく、ここでエフェクトを出しても良い
                Destroy(activeMarkers[i].obj);
                activeMarkers.RemoveAt(i);
            }
        }
    }
    void InitializeMarkers(List<float> thresholds)
    {
        // 既存のピンをクリア
        foreach (Transform child in markerParent) Destroy(child.gameObject);
        activeMarkers.Clear();

        foreach (float threshold in thresholds)
        {
            GameObject marker = Instantiate(markerPrefab, markerParent);
            RectTransform markerRect = marker.GetComponent<RectTransform>();

            // 位置計算ロジック
            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 1f); // 上端基準
            float angle = threshold * 360f;
            float rad = (90f + angle) * Mathf.Deg2Rad;
            float x = Mathf.Cos(rad) * radius;
            float y = Mathf.Sin(rad) * radius;

            markerRect.anchoredPosition = new Vector2(x, y);
            markerRect.localRotation = Quaternion.Euler(0, 0, angle);
            markerRect.localScale = markerScale;

            // リストに追加して管理対象にする
            activeMarkers.Add(new MarkerInfo { obj = marker, threshold = threshold });
        }
    }
}