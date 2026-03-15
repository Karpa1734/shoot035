using KanKikuchi.AudioManager;
using System.Collections;
using TMPro;
using UnityEngine;

public class EnemySpellCardUI : MonoBehaviour
{
    public static EnemySpellCardUI Instance;

    [Header("UI Components")]
    public CanvasGroup canvasGroup;
    public RectTransform rectTransform;
    public TextMeshProUGUI spellNameText;
    public TextMeshProUGUI bonusText;
    public TextMeshProUGUI historyText;

    [Header("Position Settings")]
    // 右下の出現位置（例: x=400, y=-450）
    public Vector2 startPos = new Vector2(400, -450);
    // 右上の待機位置（例: x=400, y=400）
    public Vector2 targetPos = new Vector2(400, 400);
    // --- 修正：水色のカラーコードを定義（必要に応じて調整してください） ---
    private readonly string cyanColorTag = "<color=#00FFFF>";
    private readonly string colorEndTag = "</color>";
    private Coroutine currentAnimation;

    void Awake()
    {
        Instance = this;
        canvasGroup.alpha = 0f;
    }

    // スペルカード開始時に呼ばれる
    public void DisplaySpell(string spellName, int getCount, int challengeCount)
    {

        SEManager.Instance.Play(SEPath.CARDCALL, 0.5f);
        // 1. 自分をアクティブにする
        gameObject.SetActive(true);

        // 2. 表示・アニメーション開始
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(SpellInRoutine(spellName, getCount, challengeCount));
    }

    // スペルカード終了時に呼ばれる
    public void HideSpell()
    {
        if (gameObject.activeInHierarchy) // 表示中のときだけ実行
        {
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(SpellOutRoutine());
        }
    }

    IEnumerator SpellOutRoutine()
    {
        float elapsed = 0f;
        Vector2 exitPos = targetPos + new Vector2(600f, 0f);

        while (elapsed < 0.33f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.33f;
            rectTransform.anchoredPosition = Vector2.Lerp(targetPos, exitPos, t);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;

        // --- 修正：アニメーションが終わったら自分を非アクティブにする ---
        gameObject.SetActive(false);
    }

    IEnumerator SpellInRoutine(string name, int get, int challenge)
    {
        spellNameText.text = name;
        // --- 修正：リッチテキストタグを使用して "History" と "Bonus" の色を変更 ---
        historyText.text = $"{cyanColorTag}History{colorEndTag}  {get:D3}/{challenge:D3}";

        // ボーナス値は更新ロジックができるまで仮で 0 固定や計算式を
        // bonusText.text = "Bonus  " + "00000000";
        bonusText.text = $"{cyanColorTag}Bonus{colorEndTag}  00000000";

        // 1. 右下に出現（縮小フェードイン）
        rectTransform.anchoredPosition = startPos;
        rectTransform.localScale = Vector3.one * 1.5f; // 最初は少し大きく
        canvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < 0.33f) // 約20フレーム
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.33f;
            // 縮小しながらフェードイン
            rectTransform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, t);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        // 2. 右上（待機場所）へ移動
        yield return new WaitForSeconds(0.6f);
        elapsed = 0f;
        while (elapsed < 0.67f) // 約40フレーム
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.67f;
            // Sin曲線で滑らかに移動
            float easedT = Mathf.Sin(t * Mathf.PI * 0.5f);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, easedT);
            yield return null;
        }
    }


}