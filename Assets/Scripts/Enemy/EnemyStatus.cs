using KanKikuchi.AudioManager;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// フェーズの種類を定義
public enum PhaseType { Normal, SpellCard, Endurance }

[Serializable]
public struct BossPhaseData
{
    public string phaseName;     // ○符「○○○○」の名前
    public float maxHP;
    public PhaseType type;
    [Range(0f, 1f)] public float defense;
    public GameObject patternPrefab;
    public bool startsNewBar;

    [Header("Special Settings")]
    public float timeLimit;
    [Range(0f, 1f)] public float bombDamageCut;
    public float invincibleDuration;

    // --- 追加：宣言タイミングの設定 ---
    [Tooltip("チェックを入れると、移動開始と同時にスペル宣言します。オフなら移動完了後に宣言します。")]
    public bool declareImmediately;
}

public class EnemyStatus : MonoBehaviour
{
    [Header("Boss Profile")]
    public string bossName = "Hayase Yuuka"; // ボスの名前
    [Header("Phase Settings")]
    [SerializeField] private List<BossPhaseData> phases;
    [SerializeField] private GameObject bulletClearPrefab;

    [Header("UI Settings")]
    public EnemySpellCardUI enemySpellUI; // インスペクターで SpellCardUI オブジェクトをドラッグ&ドロップ
    public GameObject enemyMarkerPrefab; // インスペクターでマーカーのプレハブをセット
    public GameObject healthBarPrefab;
    public List<float> phaseThresholds = new List<float>();

    // --- 復元：点滅開始のHPしきい値 ---
    // インスペクターからの固定値入力をなくし、計算用に NonSerialized にします
    [Header("Marker Settings")]
    [NonSerialized] public float flickerLifeThreshold;

    [NonSerialized] public float currentHP;
    [NonSerialized] public float maxHP;

    private int currentPhaseIndex = 0;
    private bool isTransitioning = false;
    private GameObject currentPatternObj;
    private BossCircularHealth currentUI;
    [Header("Timer Display")]
    [NonSerialized] public float currentTimer;
    private bool isTimerActive = false;
    private bool isInvincible = false; // 無敵中かどうかのフラグ


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
    // --- EnemyStatus.cs の Start メソッドを以下に差し替え ---
    void Start()
    {
        // 1. シーン内の Canvas (BossHealthCanvas) を探す
        GameObject canvas = GameObject.Find("EnemySpellCanvas");
        if (canvas != null)
        {
            // 2. その Canvas の子から "EnemySpellCardUI" を探す (非アクティブでも OK)
            // ここでの名前はヒエラルキー上の名前に合わせてください
            Transform spellUITrans = canvas.transform.Find("EnemySpellUI");
            if (spellUITrans != null)
            {
                enemySpellUI = spellUITrans.GetComponent<EnemySpellCardUI>();
            }
        }

        SpawnHealthBar(); //
        SpawnMarker(); //

        // タイマーUIへの登録
        if (BossTimerUI.Instance != null)
        {
            BossTimerUI.Instance.targetStatus = this;
        }

        if (BossLifeCountUI.Instance != null)
        {
            BossLifeCountUI.Instance.Initialize(this);
        }
    }

    // --- EnemyStatus.cs ---

    void InitializePhase(int index)
    {
        if (index >= phases.Count) return;

        currentPhaseIndex = index;
        // --- 修正点1：先に maxHP を更新してから 2割 を計算する ---
        maxHP = phases[index].maxHP;
        currentHP = maxHP;
        flickerLifeThreshold = maxHP * 0.2f;

        currentTimer = phases[index].timeLimit;
        isTimerActive = currentTimer > 0;

        if (BossTimerUI.Instance != null)
        {
            BossTimerUI.Instance.SetPhaseType(phases[index].type);
        }
        // --- 修正ポイント：無敵コルーチンを開始 ---
        if (phases[index].invincibleDuration > 0)
        {
            StartCoroutine(InvincibilityRoutine(phases[index].invincibleDuration));
        }
        else
        {
            isInvincible = false;
        }
        if (currentPatternObj != null) Destroy(currentPatternObj);
        if (phases[index].patternPrefab != null)
        {
            currentPatternObj = Instantiate(phases[index].patternPrefab, transform.position, Quaternion.identity, transform);
        }
        // --- スペルカード演出のトリガー ---
        if (EnemySpellCardUI.Instance != null)
        {
            if (phases[index].type == PhaseType.SpellCard)
            {
                // インスペクターで設定した phaseName を渡す
                // get/challengeCount は今は仮で 0 を入れています
                EnemySpellCardUI.Instance.DisplaySpell(phases[index].phaseName, 0, 0);
            }
            else
            {
                // 通常フェーズ（Normal）になったら右へはけさせる
                EnemySpellCardUI.Instance.HideSpell();
            }
        }


    }

    private void SpawnMarker()
    {
        // ライフバーと同じCanvasを取得して生成
        GameObject canvas = GameObject.Find("MarkerCanvas");
        if (canvas != null && enemyMarkerPrefab != null)
        {
            GameObject markerObj = Instantiate(enemyMarkerPrefab, canvas.transform);
            // 生成したマーカーに「自分」を追跡対象として教える
            markerObj.GetComponent<EnemyMarker>().SetTarget(this);
        }
    }

    // 無敵時間をカウントするコルーチン
    private IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;
        yield return new WaitForSeconds(duration);
        isInvincible = false;
    }
    // --- 修正版：フェーズ移行シーケンス ---
    IEnumerator PhaseTransitionSequence()
    {
        isTransitioning = true;
        // --- 修正ポイント：撃破した瞬間にスペル UI を引っ込める ---
        // 現在終了したフェーズがスペルカードだった場合、即座に退避アニメーションを開始する
        if (enemySpellUI != null && phases[currentPhaseIndex].type == PhaseType.SpellCard)
        {
            enemySpellUI.HideSpell();
        }

        if (currentPatternObj != null) Destroy(currentPatternObj);

        // 1. 弾幕クリア演出
        if (bulletClearPrefab != null)
        {
            GameObject effector = Instantiate(bulletClearPrefab, transform.position, Quaternion.identity);
            effector.GetComponent<BulletClearEffect>()?.StartClearing(transform.position);
        }

        int nextIndex = currentPhaseIndex + 1;
        bool hasNextPhase = nextIndex < phases.Count;

        if (hasNextPhase)
        {
            // 次のフェーズの準備
            if (phases[nextIndex].startsNewBar && currentUI != null) Destroy(currentUI.gameObject);

            currentPhaseIndex = nextIndex;
            maxHP = phases[currentPhaseIndex].maxHP;
            currentHP = maxHP;
            flickerLifeThreshold = maxHP * 0.2f;

            // --- 修正ポイント：即時宣言の設定を確認 ---
            if (phases[currentPhaseIndex].declareImmediately)
            {
                // 移動開始と同時に UI を更新（名前表示 ＆ タイマー移動開始）
                TriggerSpellDeclaration(currentPhaseIndex);
            }
        }
        else if (currentUI != null) Destroy(currentUI.gameObject);

        // 2. 中央へ移動
        BossController ctrl = GetComponent<BossController>();
        if (ctrl != null) ctrl.SetMoving(true);
        Vector3 target = new Vector3(-2.0f, 2.0f, 0);
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, 3f * Time.deltaTime);
            yield return null;
        }
        if (ctrl != null) ctrl.SetMoving(false);

        yield return new WaitForSeconds(0.5f);

        if (hasNextPhase)
        {
            // --- 修正ポイント：即時宣言でなかった場合は、ここで宣言 ---
            if (!phases[currentPhaseIndex].declareImmediately)
            {
                TriggerSpellDeclaration(currentPhaseIndex);
            }

            // 実際の攻撃パターンとタイマー（カウントダウン）を開始
            InitializePhaseLogic(currentPhaseIndex);

            if (phases[currentPhaseIndex].startsNewBar) SpawnHealthBar();
            isTransitioning = false;
        }
        else Die();
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

        if (isTransitioning || currentHP <= 0 || isInvincible) return;

        if (currentHP > flickerLifeThreshold)
        {
            SEManager.Instance.Play(SEPath.SE_DAMAGE00,0.5f);
        }
        else
        {
            SEManager.Instance.Play(SEPath.SE_DAMAGE01, 0.5f);
        }
        // 内部的に引数2つのバージョンを「ボムではない(false)」として呼び出す
        TakeDamage(damage, false);




    }

    // 2. ボムや特殊攻撃から呼ばれる、引数2つのバージョン
    public void TakeDamage(float damage, bool isBomb)
    {
        if (isTransitioning || currentHP <= 0 || isInvincible) return;
        if (currentHP > flickerLifeThreshold)
        {
            SEManager.Instance.Play(SEPath.SE_DAMAGE00, 0.5f);
        }
        else
        {
            SEManager.Instance.Play(SEPath.SE_DAMAGE01, 0.5f);
        }
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
    // スペル名表示やタイマー位置の「見た目」の更新
    private void TriggerSpellDeclaration(int index)
    {
        // タイマーのタイプ（位置）を同期
        if (BossTimerUI.Instance != null)
        {
            BossTimerUI.Instance.SetPhaseType(phases[index].type);
        }

        // --- 修正：Instance ではなく直接参照(enemySpellUI)を使用 ---
        if (enemySpellUI != null)
        {
            if (phases[index].type == PhaseType.SpellCard)
            {
                // インスペクターの phaseName を使用して宣言
                enemySpellUI.DisplaySpell(phases[index].phaseName, 0, 0);
            }
            else
            {
                enemySpellUI.HideSpell();
            }
        }
    }

    // 実際の攻撃やタイマーの「動作」の更新
    private void InitializePhaseLogic(int index)
    {
        currentTimer = phases[index].timeLimit;
        isTimerActive = currentTimer > 0;

        if (phases[index].invincibleDuration > 0)
            StartCoroutine(InvincibilityRoutine(phases[index].invincibleDuration));
        else
            isInvincible = false;

        if (phases[index].patternPrefab != null)
            currentPatternObj = Instantiate(phases[index].patternPrefab, transform.position, Quaternion.identity, transform);
    }

}