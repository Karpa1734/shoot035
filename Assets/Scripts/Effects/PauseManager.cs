using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using KanKikuchi.AudioManager;

public class PauseManager : MonoBehaviour
{

    [Header("UI Elements")]
    public GameObject pauseCanvas;
    public TextMeshProUGUI[] menuTexts;

    [Header("Confirmation UI")]
    public GameObject confirmPanel; // 確認ダイアログの親オブジェクト
    public TextMeshProUGUI confirmYesText;
    public TextMeshProUGUI confirmNoText;

    [Header("Selection Settings")]
    public bool[] menuSelectable;
    [Range(0f, 1f)] public float disabledAlpha = 0.3f;

    [Header("Color Settings")]
    public Color selectedColor = Color.white;
    public Color unselectedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private bool isPaused = false;
    private int selectedIndex = 0;
    private bool isGameOverMode = false;

    // --- 追加：状態管理用 ---
    private enum PauseState { Main, ConfirmExit, ConfirmRestart }
    private PauseState currentState = PauseState.Main;
    private int confirmIndex = 1; // 0: Yes, 1: No (初期位置をNoに)

    void Start()
    {
        if (pauseCanvas != null) pauseCanvas.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);

        if (menuSelectable == null || menuSelectable.Length != menuTexts.Length)
        {
            System.Array.Resize(ref menuSelectable, menuTexts.Length);
            for (int i = 0; i < menuSelectable.Length; i++) menuSelectable[i] = true;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                if (currentState != PauseState.Main) CancelConfirmation();
                else ResumeGame();
            }
            else PauseGame();
        }

        if (isPaused)
        {
            if (currentState == PauseState.Main) HandleMenuNavigation();
            else HandleConfirmNavigation();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        pauseCanvas.SetActive(true);
        confirmPanel.SetActive(false);
        currentState = PauseState.Main;
        Time.timeScale = 0f;
        selectedIndex = FindNextSelectableIndex(-1, 1);
        UpdateMenuVisuals();
        SEManager.Instance.Play(SEPath.PAUSE, 0.5f);
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseCanvas.SetActive(false);
        confirmPanel.SetActive(false);
        Time.timeScale = 1f;
        SEManager.Instance.Play(SEPath.MENUCANCEL, 0.5f);
    }

    void HandleMenuNavigation()
    {
        int prevIndex = selectedIndex;
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex = FindNextSelectableIndex(selectedIndex, -1);
            if (prevIndex != selectedIndex) { UpdateMenuVisuals(); SEManager.Instance.Play(SEPath.MENUSELECT, 0.5f); }
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex = FindNextSelectableIndex(selectedIndex, 1);
            if (prevIndex != selectedIndex) { UpdateMenuVisuals(); SEManager.Instance.Play(SEPath.MENUSELECT, 0.5f); }
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (menuSelectable[selectedIndex]) ExecuteSelection();
        }
    }

    // --- 追加：確認画面のナビゲーション ---
    void HandleConfirmNavigation()
    {
        int prev = confirmIndex;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) ||
            Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            confirmIndex = (confirmIndex == 0) ? 1 : 0;
            if (prev != confirmIndex) { UpdateConfirmVisuals(); SEManager.Instance.Play(SEPath.MENUSELECT, 0.5f); }
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (confirmIndex == 0) ExecuteConfirmedAction(); // Yes
            else CancelConfirmation(); // No
        }

        if (Input.GetKeyDown(KeyCode.X)) CancelConfirmation();
    }

    void ExecuteSelection()
    {
        switch (selectedIndex)
        {
            case 0: // 再開
                SEManager.Instance.Play(SEPath.MENUDECIDE, 0.5f);
                if (isGameOverMode) PlayerStatusManager.Instance.PerformContinue();
                isGameOverMode = false;
                ResumeGame();
                break;
            case 1: // タイトルへ（確認へ）
                OpenConfirmation(PauseState.ConfirmExit);
                break;
            case 4: // 最初から（確認へ）
                OpenConfirmation(PauseState.ConfirmRestart);
                break;
        }
    }

    void OpenConfirmation(PauseState state)
    {
        SEManager.Instance.Play(SEPath.MENUDECIDE, 0.5f);
        currentState = state;
        confirmIndex = 1; // 初期位置を No に設定
        confirmPanel.SetActive(true);
        UpdateConfirmVisuals();
    }

    void CancelConfirmation()
    {
        SEManager.Instance.Play(SEPath.MENUCANCEL, 0.5f);
        currentState = PauseState.Main;
        confirmPanel.SetActive(false);
    }

    void ExecuteConfirmedAction()
    {
        SEManager.Instance.Play(SEPath.MENUDECIDE, 0.5f);

        // --- 追加：現在のハイスコアを保存 ---
        // ScoreManager に実装した SaveHighScore メソッドを呼び出します
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SaveHighScore();
        }

        PlayerStatusManager.Instance.ResetContinueCount();
        Time.timeScale = 1f;

        if (currentState == PauseState.ConfirmExit)
        {
            SceneManager.LoadScene("Title");
        }
        else if (currentState == PauseState.ConfirmRestart)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    void UpdateConfirmVisuals()
    {
        confirmYesText.color = (confirmIndex == 0) ? selectedColor : unselectedColor;
        confirmNoText.color = (confirmIndex == 1) ? selectedColor : unselectedColor;
    }


    // --- 追加：次に選択可能なインデックスを探索する ---
    int FindNextSelectableIndex(int current, int direction)
    {
        int count = menuTexts.Length;
        if (count == 0) return 0;

        int next = current;
        for (int i = 0; i < count; i++)
        {
            next = (next + direction + count) % count;
            if (menuSelectable[next]) return next;
        }
        return (current == -1) ? 0 : current;
    }

    void UpdateMenuVisuals()
    {
        if (menuTexts == null) return;

        for (int i = 0; i < menuTexts.Length; i++)
        {
            // 追加：インスペクターで None が混ざっている場合の対策
            if (menuTexts[i] == null) continue;

            if (!menuSelectable[i])
            {
                Color c = unselectedColor;
                c.a = disabledAlpha;
                menuTexts[i].color = c;
            }
            else
            {
                menuTexts[i].color = (i == selectedIndex) ? selectedColor : unselectedColor;
            }
        }
    }

    // 外部からモードを切り替えるメソッド
    public void SetGameOverMode(bool active)
    {
        isGameOverMode = active;
        if (active)
        {
            menuTexts[0].text = "コンティニューする"; // 文言を変更
            menuSelectable[0] = true; // ゲームオーバー時は選べるようにする
        }
        else
        {
            menuTexts[0].text = "一時停止を解除";
        }
    }
}