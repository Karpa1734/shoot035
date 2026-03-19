using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using KanKikuchi.AudioManager;

public class TitleMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI[] menuTexts;

    [Header("Selection Settings")]
    public bool[] menuSelectable;
    [Range(0f, 1f)] public float disabledAlpha = 0.3f;

    [Header("Color Settings")]
    public Color selectedColor = Color.white;
    public Color unselectedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Scene Settings")]
    public string gameSceneName = "Shoot"; // ゲーム本編のシーン名

    private int selectedIndex = 0;

    // --- TitleMenuManager.cs の修正 ---

    void Start()
    {
        // menuTextsが未設定なら何もしない
        if (menuTexts == null || menuTexts.Length == 0) return;

        if (menuSelectable == null || menuSelectable.Length != menuTexts.Length)
        {
            System.Array.Resize(ref menuSelectable, menuTexts.Length);
            for (int i = 0; i < menuSelectable.Length; i++)
            {
                menuSelectable[i] = (i == 0 || i == menuTexts.Length - 1);
            }
        }

        selectedIndex = FindNextSelectableIndex(-1, 1);
        UpdateMenuVisuals();
    }

    void UpdateMenuVisuals()
    {
        for (int i = 0; i < menuTexts.Length; i++)
        {
            // 追加：テキスト自体がアサインされていない場合はスキップ
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

    void Update()
    {
        HandleMenuNavigation();
    }

    void HandleMenuNavigation()
    {
        int prevIndex = selectedIndex;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex = FindNextSelectableIndex(selectedIndex, -1);
            if (prevIndex != selectedIndex)
            {
                UpdateMenuVisuals();
                SEManager.Instance.Play(SEPath.MENUSELECT, 0.5f);
            }
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex = FindNextSelectableIndex(selectedIndex, 1);
            if (prevIndex != selectedIndex)
            {
                UpdateMenuVisuals();
                SEManager.Instance.Play(SEPath.MENUSELECT, 0.5f);
            }
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (menuSelectable[selectedIndex])
            {
                ExecuteSelection();
            }
        }
    }

    int FindNextSelectableIndex(int current, int direction)
    {
        int count = menuTexts.Length;
        int next = current;
        for (int i = 0; i < count; i++)
        {
            next = (next + direction + count) % count;
            if (menuSelectable[next]) return next;
        }
        return (current == -1) ? 0 : current;
    }


    void ExecuteSelection()
    {
        SEManager.Instance.Play(SEPath.MENUDECIDE, 0.5f);

        switch (selectedIndex)
        {
            case 0: // Game Start
                SceneManager.LoadScene(gameSceneName);
                break;

            case 9: // Exit (リストの一番下)
                Debug.Log("Quit Game");
                Application.Quit();
                break;
        }
    }
}