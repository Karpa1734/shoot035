using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerStatusUI : MonoBehaviour
{
    public Sprite filledSprite; // 中身あり（ピンクのハート / 緑の六角形）
    public Sprite emptySprite;  // 中身なし
    public List<Image> icons;   // 8個のImageを左から順にセット

    public void SetCount(int count)
    {
        for (int i = 0; i < icons.Count; i++)
        {
            if (icons[i] == null) continue;
            // 数値より下のインデックスは「あり」、それ以降は「なし」
            icons[i].sprite = (i < count) ? filledSprite : emptySprite;
        }
    }
}