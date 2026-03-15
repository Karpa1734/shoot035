using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BossLifeCountUI : MonoBehaviour
{
    public static BossLifeCountUI Instance;

    [Header("References")]
    public TextMeshProUGUI nameText;
    public Transform starParent;
    public GameObject starPrefab;
    public CanvasGroup canvasGroup;

    [Header("Position Settings")]
    public float worldCenterX = -2.0f;
    public float leftOffset = -180f; // 中心からの左への距離
    public float topOffset = 20f;

    private EnemyStatus targetStatus;
    private RectTransform rectTransform;
    private Camera mainCamera;
    private List<GameObject> activeStars = new List<GameObject>();

    void Awake()
    {
        Instance = this;
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;
        canvasGroup.alpha = 0f;

        // アンカーを左上に設定
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f); // 左上基点
    }

    public void SetTarget(EnemyStatus status)
    {
        targetStatus = status;
        nameText.text = status.bossName;
        UpdateStars();
    }

    void Update()
    {
        if (targetStatus == null)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, Time.deltaTime * 3f);
            return;
        }

        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.deltaTime * 3f);

        // 位置を x = -2.0 の左上に同期
        Vector3 screenPos = mainCamera.WorldToScreenPoint(new Vector3(worldCenterX, 0, 0));
        float uiPosX = (screenPos.x - Screen.width / 2f) + leftOffset;
        rectTransform.anchoredPosition = new Vector2(uiPosX, -topOffset);

        // 星の数を監視（フェーズ移行時などに更新）
        UpdateStars();
    }

    void UpdateStars()
    {
        if (targetStatus == null) return;

        int lifeCount = targetStatus.GetRemainingLifeCount();

        // 描画されている星の数と実際の数が違う場合のみ更新
        if (activeStars.Count != lifeCount)
        {
            foreach (var star in activeStars) Destroy(star);
            activeStars.Clear();

            for (int i = 0; i < lifeCount; i++)
            {
                GameObject s = Instantiate(starPrefab, starParent);
                activeStars.Add(s);
            }
        }
    }
}