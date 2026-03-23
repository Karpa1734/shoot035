using UnityEngine;
using KanKikuchi.AudioManager;

public class PlayerStatusManager : MonoBehaviour
{
    public static PlayerStatusManager Instance;

    [Header("Initial Settings")]
    public int initialLife = 2;
    public int initialSpell = 3;
    public int currentLife;
    public int currentSpell;

    [Header("Statistics")]
    public int continueCount = 0; // コンティニュー回数

    [Header("UI References")]
    public PlayerStatusUI lifeUI;
    public PlayerStatusUI spellUI;
    public PauseManager pauseManager;
    // 必要であればコンティニュー回数を表示するテキストをここに追加
    // public TMPro.TextMeshProUGUI continueCountText; 

    void Awake()
    {
        if (Instance == null) Instance = this;
        currentLife = initialLife;
        currentSpell = initialSpell;
    }

    void Start()
    {
        UpdateUI();
    }

    public bool UseSpell()
    {
        if (currentSpell > 0)
        {
            currentSpell--;
            UpdateUI();
            return true;
        }
        return false;
    }

    public bool SubtractLifeAndCheckRebirth()
    {
        if (currentLife > 0)
        {
            currentLife--;
            currentSpell = initialSpell; // 復活時はボム補充
            UpdateUI();
            return true;
        }
        return false;
    }

    // --- 追加：コンティニュー実行処理 ---
    public void PerformContinue()
    {
        continueCount++; // カウントアップ
        currentLife = initialLife; // 初期値で再開
        currentSpell = initialSpell;
        UpdateUI();

        // プレイヤーを復活させる演出を開始
        PlayerHitHandler hitHandler = Object.FindFirstObjectByType<PlayerHitHandler>();
        if (hitHandler != null) hitHandler.StartRebirthFromContinue();
    }

    // --- 追加：コンティニュー回数のリセット ---
    public void ResetContinueCount()
    {
        continueCount = 0; //
    }

    private void UpdateUI()
    {
        if (lifeUI != null) lifeUI.SetCount(currentLife);
        if (spellUI != null) spellUI.SetCount(currentSpell);
        // if (continueCountText != null) continueCountText.text = "Continue: " + continueCount;
    }

    public void TriggerGameOver()
    {
        if (pauseManager == null) return;

        // ゲームオーバーモードでポーズを開く
        pauseManager.SetGameOverMode(true);
        pauseManager.PauseGame();
    }
}