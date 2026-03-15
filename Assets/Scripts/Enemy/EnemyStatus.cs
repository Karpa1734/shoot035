using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// フェーズの種類を定義
public enum PhaseType { Normal, SpellCard, Endurance }

[Serializable]
public struct BossPhaseData
{
    public string phaseName;
    public float maxHP;
    public PhaseType type;
    [Range(0f, 1f)] public float defense;
    public GameObject patternPrefab;
    public bool startsNewBar;

    // --- 新規追加 ---
    [Header("Special Settings")]
    public float timeLimit;           // 制限時間（秒）
    [Range(0f, 1f)] public float bombDamageCut; // ボムのダメージカット率 (1.0で完全無効)
}

public class EnemyStatus : MonoBehaviour
{
    [Header("Boss Profile")]
    public string bossName = "Hayase Yuuka"; // ボスの名前
    [Header("Phase Settings")]
    [SerializeField] private List<BossPhaseData> phases;
    [SerializeField] private GameObject bulletClearPrefab;

    [Header("UI Settings")]
    public GameObject healthBarPrefab;
    public List<float> phaseThresholds = new List<float>();

    // --- 復元：点滅開始のHPしきい値 ---
    [Header("Marker Settings")]
    public float flickerLifeThreshold = 200f;

    [NonSerialized] public float currentHP;
    [NonSerialized] public float maxHP;

    private int currentPhaseIndex = 0;
    private bool isTransitioning = false;
    private GameObject currentPatternObj;
    private BossCircularHealth currentUI;
    [Header("Timer Display")]
    [NonSerialized] public float currentTimer;
    private bool isTimerActive = false;
    void Awake()
    {
        InitializePhase(0);
    }
    // 残りのライフバー本数（星の数）を計算する
    public int GetRemainingLifeCount()
    {
        int count = 0;
        // 現在のフェーズより後にある「startsNewBar」の数を数える
        for (int i = currentPhaseIndex + 1; i < phases.Count; i++)
        {
            if (phases[i].startsNewBar) count++;
        }
        return count;
    }
    void Start()
    {
        SpawnHealthBar();
        // --- 追加：シーン内のタイマーUIを探して自分を登録する ---
        // BossTimerUI がシーンに1つだけ存在することを前提としています
        BossTimerUI timerUI = FindObjectOfType<BossTimerUI>();
        if (timerUI != null)
        {
            timerUI.targetStatus = this;
        }
        // --- 追加：ライフカウントUIに自分を登録 ---
        if (BossLifeCountUI.Instance != null) BossLifeCountUI.Instance.SetTarget(this);
    }

    void InitializePhase(int index)
    {
        if (index >= phases.Count) return;

        currentPhaseIndex = index;
        maxHP = phases[index].maxHP;
        currentHP = maxHP;
        currentTimer = phases[index].timeLimit;
        isTimerActive = currentTimer > 0;

        // --- 修正ポイント：タイマーにフェーズの種類を伝える ---
        if (BossTimerUI.Instance != null)
        {
            BossTimerUI.Instance.SetPhaseType(phases[index].type);
        }

        if (currentPatternObj != null) Destroy(currentPatternObj);
        if (phases[index].patternPrefab != null)
        {
            currentPatternObj = Instantiate(phases[index].patternPrefab, transform.position, Quaternion.identity, transform);
        }
    }
    void Update()
    {
        // タイマーのカウントダウン
        if (isTimerActive && !isTransitioning)
        {
            currentTimer -= Time.deltaTime;
            if (currentTimer <= 0)
            {
                currentTimer = 0;
                isTimerActive = false;
                // 時間切れによる強制フェーズ移行
                StartCoroutine(PhaseTransitionSequence());
            }
        }
    }

    // --- ダメージ計算の修正 ---
    // --- EnemyStatus.cs のダメージ処理部分を以下に差し替え ---

    // 1. 弾(SendMessage)から呼ばれる、引数1つのバージョン
    public void TakeDamage(float damage)
    {
        // 内部的に引数2つのバージョンを「ボムではない(false)」として呼び出す
        TakeDamage(damage, false);
    }

    // 2. ボムや特殊攻撃から呼ばれる、引数2つのバージョン
    public void TakeDamage(float damage, bool isBomb)
    {
        if (isTransitioning || currentHP <= 0) return;

        float finalDamage = damage;
        float def = phases[currentPhaseIndex].defense;

        // ボムダメージの場合、カット率を適用
        if (isBomb)
        {
            float cut = phases[currentPhaseIndex].bombDamageCut;
            finalDamage *= (1f - cut);
        }

        // 防御力を適用して減算
        currentHP -= finalDamage * (1f - def);

        if (currentHP <= 0)
        {
            currentHP = 0;
            // タイマー停止と移行処理
            isTimerActive = false;
            StartCoroutine(PhaseTransitionSequence());
        }
    }
    // --- 追加：現在表示中のバーがどのインデックスから始まったかを探すヘルパー ---
    private int GetCurrentBarGroupStartIndex()
    {
        int start = currentPhaseIndex;
        // 現在のインデックスから遡って、startsNewBar が true の場所を探す
        while (start > 0 && !phases[start].startsNewBar)
        {
            start--;
        }
        return start;
    }

    // --- 修正：バー全体の最大HP（起点から計算） ---
    public float GetBarTotalMaxHP()
    {
        float total = 0;
        int startIndex = GetCurrentBarGroupStartIndex();

        for (int i = startIndex; i < phases.Count; i++)
        {
            // 次の「新しいバー」が出てくるまで加算
            if (i > startIndex && phases[i].startsNewBar) break;
            total += phases[i].maxHP;
        }
        return total;
    }

    // --- 修正：現在のバー内の残り合計HP ---
    public float GetBarCurrentHP()
    {
        float total = currentHP; // 現在のフェーズの残りHP
        int startIndex = GetCurrentBarGroupStartIndex();

        // 現在のフェーズより後の、同じバーに属するHPを足す
        for (int i = currentPhaseIndex + 1; i < phases.Count; i++)
        {
            if (phases[i].startsNewBar) break;
            total += phases[i].maxHP;
        }
        return total;
    }

    // --- 修正：ピンの位置計算（起点から計算） ---
    public List<float> GetBarThresholds()
    {
        List<float> thresholds = new List<float>();
        float barMax = GetBarTotalMaxHP();
        if (barMax <= 0) return thresholds;

        int startIndex = GetCurrentBarGroupStartIndex();
        float accumulatedHP = 0;

        for (int i = startIndex; i < phases.Count; i++)
        {
            if (i > startIndex && phases[i].startsNewBar) break;

            accumulatedHP += phases[i].maxHP;
            // 次のフェーズが同じバー内ならピンを打つ
            if (i + 1 < phases.Count && !phases[i + 1].startsNewBar)
            {
                thresholds.Add((barMax - accumulatedHP) / barMax);
            }
        }
        return thresholds;
    }
    IEnumerator PhaseTransitionSequence()
    {
        isTransitioning = true;
        if (currentPatternObj != null) Destroy(currentPatternObj);

        // 1. 弾幕クリア演出
        if (bulletClearPrefab != null)
        {
            GameObject effector = Instantiate(bulletClearPrefab, transform.position, Quaternion.identity);
            effector.GetComponent<BulletClearEffect>()?.StartClearing(transform.position);
        }

        // --- 追加：次のフェーズの型を先に確認し、タイマーの位置移動を開始させる ---
        int nextIndex = currentPhaseIndex + 1;
        if (nextIndex < phases.Count && BossTimerUI.Instance != null)
        {
            // ボスが動き出すのと同時にタイマーの移動フラグを送る
            BossTimerUI.Instance.SetPhaseType(phases[nextIndex].type);
        }

        // 2. 中央へ移動 (-2.0, 2.0)
        BossController ctrl = GetComponent<BossController>();
        if (ctrl != null) ctrl.SetMoving(true);

        Vector3 target = new Vector3(-2.0f, 2.0f, 0);
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            // ボスが徐々に中央へ向かう
            transform.position = Vector3.MoveTowards(transform.position, target, 3f * Time.deltaTime);
            yield return null;
        }
        if (ctrl != null) ctrl.SetMoving(false);

        yield return new WaitForSeconds(0.5f);

        // 3. 次のフェーズ判定
        if (nextIndex < phases.Count)
        {
            currentPhaseIndex = nextIndex;

            if (phases[currentPhaseIndex].startsNewBar)
            {
                if (currentUI != null) Destroy(currentUI.gameObject);
                InitializePhase(currentPhaseIndex);
                SpawnHealthBar();
            }
            else
            {
                InitializePhase(currentPhaseIndex);
            }
            isTransitioning = false;
        }
        else
        {
            if (currentUI != null) Destroy(currentUI.gameObject);
            Die();
        }
    }
    private void SpawnHealthBar()
    {
        GameObject canvas = GameObject.Find("BossHealthCanvas");
        if (canvas != null && healthBarPrefab != null)
        {
            GameObject barObj = Instantiate(healthBarPrefab, canvas.transform);
            currentUI = barObj.GetComponent<BossCircularHealth>();
            if (currentUI != null)
            {
                // 引数を自動計算したしきい値に変更
                currentUI.Initialize(this, GetBarThresholds());
            }
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}