using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using KanKikuchi.AudioManager;

public class PauseManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject pauseCanvas;
    public TextMeshProUGUI[] menuTexts;

    [Header("Color Settings")]
    public Color selectedColor = Color.white;
    public Color unselectedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private bool isPaused = false;
    private int selectedIndex = 0;

    void Start()
    {
        if (pauseCanvas != null) pauseCanvas.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }

        if (isPaused)
        {
            HandleMenuNavigation();
        }
    }

    void PauseGame()
    {
        isPaused = true;
        pauseCanvas.SetActive(true);
        Time.timeScale = 0f;
        selectedIndex = 0;
        UpdateMenuVisuals();

        // --- 追加：ポーズ開始音 ---
        SEManager.Instance.Play(SEPath.PAUSE, 0.5f);
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseCanvas.SetActive(false);
        Time.timeScale = 1f;

        // --- 追加：キャンセル（戻る）音 ---
        SEManager.Instance.Play(SEPath.MENUCANCEL, 0.5f);
    }

    void HandleMenuNavigation()
    {
        // 上下入力時に選択音を鳴らす
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex = (selectedIndex - 1 + menuTexts.Length) % menuTexts.Length;
            UpdateMenuVisuals();
            SEManager.Instance.Play(SEPath.MENUSELECT, 0.5f); // --- 追加：選択（カーソル移動）音 ---
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex = (selectedIndex + 1) % menuTexts.Length;
            UpdateMenuVisuals();
            SEManager.Instance.Play(SEPath.MENUSELECT, 0.5f); // --- 追加：選択（カーソル移動）音 ---
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            ExecuteSelection();
        }
    }

    void UpdateMenuVisuals()
    {
        for (int i = 0; i < menuTexts.Length; i++)
        {
            menuTexts[i].color = (i == selectedIndex) ? selectedColor : unselectedColor;
        }
    }

    void ExecuteSelection()
    {
        // --- 追加：決定音 ---
        SEManager.Instance.Play(SEPath.MENUDECIDE, 0.5f);

        switch (selectedIndex)
        {
            case 0: // 一時停止を解除
                // ResumeGame内でキャンセル音が鳴るため、決定音と重なるのが嫌な場合は
                // Time.timeScale等の処理をここに直接書くか、音を調整してください
                ResumeGame();
                break;

            case 1: // 最初からやり直す
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;
        }
    }
}