using UnityEngine;
using System.Collections;

public class SpellManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject sealPrefab;
    public GameObject shockwavePrefab;

    [Header("Single Sprites")]
    public Sprite sealSprite;
    public Sprite shockwaveSprite;

    public EnemyStatus bossStatus;
    private bool isOnSpell = false;

    void Update()
    {
        // Xキーでボム発動（Updateで入力を拾うのが重要）
        if (Input.GetKeyDown(KeyCode.X) && !isOnSpell)
        {
            StartCoroutine(ExecuteFantasySeal());
        }
    }

    IEnumerator ExecuteFantasySeal()
    {
        // 【重要】発動した瞬間に無敵を設定！これによって食らいボムが成立する
        // 285フレーム ≒ 4.75秒
        PlayerMove.Instance.SetInvincible(320f / 60f);

        isOnSpell = true;

        // 衝撃波の生成 
        if (shockwavePrefab != null)
        {
            GameObject shock = Instantiate(shockwavePrefab, transform.position, Quaternion.identity);
            Shockwave logic = shock.GetComponent<Shockwave>();
            if (logic != null) logic.Initialize(shockwaveSprite);
        }

        // ホーミング順序のランダム化
        int[] homingOrders = { 0, 1, 2, 3, 4, 5, 6, 7 };
        for (int i = homingOrders.Length - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            int tmp = homingOrders[i]; homingOrders[i] = homingOrders[r]; homingOrders[r] = tmp;
        }

        // 8つの封印弾を一斉に生成 
        for (int i = 0; i < 8; i++)
        {
            float startAngle = i * 45f;
            Color c = GetSealColor(i % 4);
            SpawnSealImmediate(startAngle, c, homingOrders[i]);
        }

        yield return new WaitForSeconds(255f / 60f); // loop(255)
        isOnSpell = false;
    }

    void SpawnSealImmediate(float angle, Color color, int order)
    {
        GameObject seal = Instantiate(sealPrefab, transform.position, Quaternion.identity);
        SealOrb logic = seal.GetComponent<SealOrb>();
        if (logic != null)
        {
            logic.Initialize(sealSprite, shockwaveSprite, angle, order, color, bossStatus, shockwavePrefab);
        }
    }

    private Color GetSealColor(int type)
    {
        if (type == 1) return Color.red;
        if (type == 2) return Color.yellow;
        if (type == 3) return Color.blue;
        if (type == 0) return Color.white;
        return Color.white;
    }
}