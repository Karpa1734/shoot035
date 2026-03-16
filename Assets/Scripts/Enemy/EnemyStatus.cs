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
    public string phaseName;
    public float maxHP;
    public PhaseType type;

    // --- 既存の defense と bombDamageCut を以下の2つに差し替え ---
    [Header("Damage Rates (0-100)")]
    [Range(0, 100)] public float shotDamageRate; // 自弾へのダメージ割合
    [Range(0, 100)] public float bombDamageRate; // ボムへのダメージ割合

    public GameObject patternPrefab;
    public bool startsNewBar;

    [Header("Special Settings")]
    public float timeLimit;
    public float spellBonus;
    public float invincibleDuration;
    public bool hideHealthBar;
    public bool declareImmediately;
}

public class EnemyStatus : MonoBehaviour
{
    [Header("Boss Profile")]
    public string bossName = "Hayase Yuuka";
    [Header("Phase Settings")]
    [SerializeField] private List<BossPhaseData> phases;
    [SerializeField] private GameObject bulletClearPrefab;

    [Header("UI Settings")]
    public EnemySpellCardUI enemySpellUI;
    public GameObject enemyMarkerPrefab;
    public GameObject healthBarPrefab;

    [NonSerialized] public float flickerLifeThreshold;
    [NonSerialized] public float currentHP;
    [NonSerialized] public float maxHP;

    // --- スペルボーナス関連の変数 ---
    [NonSerialized] public float currentSpellBonus;
    private bool isSpellFailed = false; // 被弾やボムで true にする
    private const float bonusWaitTime = 4.0f; // 最初の4秒は減らない (Obj_Cutin.txt の loop(240) 相当)

    private int currentPhaseIndex = 0;
    private bool isTransitioning = false;
    private GameObject currentPatternObj;
    private BossCircularHealth currentUI;
    [NonSerialized] public float currentTimer;
    private bool isTimerActive = false;
    private bool isInvincible = false;
    private float realElapsedTime = 0f; // 実経過時間の計測 [cite: 349]
    void Awake() { InitializePhase(0); }


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
        GameObject canvas = GameObject.Find("EnemySpellCanvas");
        if (canvas != null)
        {
            Transform spellUITrans = canvas.transform.Find("EnemySpellUI");
            if (spellUITrans != null) enemySpellUI = spellUITrans.GetComponent<EnemySpellCardUI>();
        }

        SpawnHealthBar();
        SpawnMarker();

        if (BossTimerUI.Instance != null) BossTimerUI.Instance.targetStatus = this;
        if (BossLifeCountUI.Instance != null) BossLifeCountUI.Instance.Initialize(this);
        TriggerSpellDeclaration(0);
    }
    // --- EnemyStatus.cs ---

    public void FailSpell()
    {
        if (phases[currentPhaseIndex].type == PhaseType.SpellCard || phases[currentPhaseIndex].type == PhaseType.Endurance)
        {
            isSpellFailed = true;
            currentSpellBonus = 0;

            // --- 修正：即座に "Failed" 表示に更新 ---
            if (enemySpellUI != null)
                enemySpellUI.UpdateBonusText(0, true);
        }
    }

    void InitializePhase(int index)
    {
        if (index >= phases.Count) return;
        currentPhaseIndex = index;
        maxHP = phases[index].maxHP;
        currentHP = maxHP;
        flickerLifeThreshold = maxHP * 0.2f;

        InitializePhaseLogic(index);
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

    void Update()
    {
        // 修正：!isTransitioning を外し、タイマーがアクティブなら常に減らす
        if (isTimerActive && !isTransitioning)
        {
            // ポーズ中（Time.timeScaleが0）は計測をスキップする判定を追加
            if (Time.timeScale > 0)
            {
                currentTimer -= Time.deltaTime;
                realElapsedTime += Time.unscaledDeltaTime;
            }


            if (phases[currentPhaseIndex].type == PhaseType.SpellCard ||
                phases[currentPhaseIndex].type == PhaseType.Endurance)
            {
                UpdateSpellBonusLogic();
            }

            if (currentTimer <= 0)
            {
                currentTimer = 0;
                isTimerActive = false;
                StartCoroutine(PhaseTransitionSequence());
            }
        }
    }
    private void UpdateSpellBonusLogic()
    {
        if (isSpellFailed)
        {
            currentSpellBonus = 0;
        }
        // --- 修正：耐久フェーズなら時間切れ(currentTimer <= 0)でも 0 にしない ---
        else if (phases[currentPhaseIndex].type == PhaseType.Endurance)
        {
            currentSpellBonus = phases[currentPhaseIndex].spellBonus;
        }
        else
        {
            // 通常スペル：時間切れ(currentTimer <= 0)なら 0
            if (currentTimer <= 0)
            {
                currentSpellBonus = 0;
            }
            else
            {
                float totalTime = phases[currentPhaseIndex].timeLimit;
                float elapsedTime = totalTime - currentTimer;

                if (elapsedTime < bonusWaitTime)
                    currentSpellBonus = phases[currentPhaseIndex].spellBonus;
                else
                    currentSpellBonus = (currentTimer / (totalTime - bonusWaitTime)) * phases[currentPhaseIndex].spellBonus;
            }
        }

        if (enemySpellUI != null)
        {
            enemySpellUI.UpdateBonusText((int)currentSpellBonus, isSpellFailed);
        }
    }
    public void TakeDamage(float damage)
    {
        // 移行中や撃破済みなら何もしない
        if (isTransitioning || currentHP <= 0) return;
        float rate = phases[currentPhaseIndex].shotDamageRate;
        if (isInvincible || rate <= 0)
        {
            SEManager.Instance.Play(SEPath.SE_NODAMAGE, 0.3f);
            return;
        }

        // 通常時のダメージSE演出
        if (currentHP > flickerLifeThreshold)
            SEManager.Instance.Play(SEPath.SE_DAMAGE00, 0.5f);
        else
            SEManager.Instance.Play(SEPath.SE_DAMAGE01, 0.5f);

        // --- 修正：SetDamageRate の仕様に基づいたダメージ計算 ---
        // (元の威力) × (レート / 100)
        float finalDamage = damage * (rate / 100f);

        currentHP -= finalDamage;

        if (currentHP <= 0)
        {
            currentHP = 0;
            isTimerActive = false; //
            StartCoroutine(PhaseTransitionSequence()); //
        }
    }
    public void TakeDamage(float damage, bool isBomb)
    {
        // 移行中や撃破済みなら何もしない
        if (isTransitioning || currentHP <= 0) return;

        // 現在のフェーズ設定からレートを取得 (0-100)
        float rate = isBomb ? phases[currentPhaseIndex].bombDamageRate : phases[currentPhaseIndex].shotDamageRate;

        // --- 修正：無敵フラグがON、またはダメージレートが0なら「無効音」を鳴らして終了 ---
        // これで Endurance フェーズ等で確実に無敵を表現できます
        if (isInvincible || rate <= 0)
        {
            SEManager.Instance.Play(SEPath.SE_NODAMAGE, 0.3f);
            return;
        }

        // 通常時のダメージSE演出
        if (currentHP > flickerLifeThreshold)
            SEManager.Instance.Play(SEPath.SE_DAMAGE00, 0.5f);
        else
            SEManager.Instance.Play(SEPath.SE_DAMAGE01, 0.5f);

        // --- 修正：SetDamageRate の仕様に基づいたダメージ計算 ---
        // (元の威力) × (レート / 100)
        float finalDamage = damage * (rate / 100f);

        currentHP -= finalDamage;

        if (currentHP <= 0)
        {
            currentHP = 0;
            isTimerActive = false; //
            StartCoroutine(PhaseTransitionSequence()); //
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
        // 1. このフェーズで非表示設定なら生成しない
        if (phases[currentPhaseIndex].hideHealthBar) return;

        GameObject canvas = GameObject.Find("BossHealthCanvas");
        if (canvas != null && healthBarPrefab != null)
        {
            GameObject barObj = Instantiate(healthBarPrefab, canvas.transform);
            currentUI = barObj.GetComponent<BossCircularHealth>();
            if (currentUI != null)
            {
                currentUI.Initialize(this, GetBarThresholds());
            }
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
    // スペル名表示やタイマー位置の「見た目」の更新


    private void InitializePhaseLogic(int index)
    {
        // --- 修正：タイマー初期化をここで行わない（TriggerSpellDeclarationに移行） ---
        // currentTimer = phases[index].timeLimit; // 削除
        // isTimerActive = currentTimer > 0;       // 削除

        // フェーズ開始時に失敗フラグはリセット
        isSpellFailed = false;

        // ボーナスの初期表示更新
        if (phases[index].type == PhaseType.SpellCard || phases[index].type == PhaseType.Endurance)
        {
            currentSpellBonus = phases[index].spellBonus;
            if (enemySpellUI != null)
            {
                enemySpellUI.UpdateBonusText((int)currentSpellBonus, isSpellFailed);
            }
        }
        else
        {
            currentSpellBonus = 0;
        }

        // 無敵やパターンの生成（既存通り） 
        if (phases[index].invincibleDuration > 0)
            StartCoroutine(InvincibilityRoutine(phases[index].invincibleDuration));
        else
            isInvincible = false;

        if (phases[index].patternPrefab != null)
            currentPatternObj = Instantiate(phases[index].patternPrefab, transform.position, Quaternion.identity, transform);
    }
    private void TriggerSpellDeclaration(int index)
    {
        if (BossTimerUI.Instance != null) BossTimerUI.Instance.SetPhaseType(phases[index].type);

        // --- 修正：カウントダウンと計測を「宣言（移動開始）」と同時に開始する ---
        currentTimer = phases[index].timeLimit;
        isTimerActive = currentTimer > 0;
        realElapsedTime = 0f;

        if (enemySpellUI != null)
        {
            if (phases[index].type == PhaseType.SpellCard || phases[index].type == PhaseType.Endurance)
            {
                enemySpellUI.DisplaySpell(phases[index].phaseName, 0, 0, phases[index].spellBonus, isSpellFailed);
            }
            else
            {
                enemySpellUI.HideSpell();
            }
        }
    }

    IEnumerator PhaseTransitionSequence()
    {
        isTransitioning = true;

        // --- 追加：スペル結果の表示ロジック ---
        if (enemySpellUI != null && (phases[currentPhaseIndex].type == PhaseType.SpellCard || phases[currentPhaseIndex].type == PhaseType.Endurance))
        {
            float clearTime = phases[currentPhaseIndex].timeLimit - currentTimer;
            bool isTimeUp = currentTimer <= 0.01f;

            // --- 修正ポイント：タイムアップSEを鳴らすべき「失敗」かどうかの判定 ---
            // 通常スペル(SpellCard) かつ 被弾していない(!isSpellFailed) かつ 時間切れ(isTimeUp)
            // この条件の時だけ、UI側で FAIL 音を鳴らすようにフラグを渡します
            bool isCaptureTimeoutFailure = (phases[currentPhaseIndex].type == PhaseType.SpellCard) && !isSpellFailed && isTimeUp;

            bool isGetBonus = false;
            if (!isSpellFailed)
            {
                if (phases[currentPhaseIndex].type == PhaseType.Endurance)
                    isGetBonus = isTimeUp; // 耐久：時間切れまで耐えたら成功
                else
                    isGetBonus = currentHP <= 0; // 通常：撃破したら成功
            }

            // 引数の最後を isTimeUp から isCaptureTimeoutFailure に変更
            enemySpellUI.ShowSpellResult((int)currentSpellBonus, clearTime, realElapsedTime, isGetBonus, isCaptureTimeoutFailure);
            enemySpellUI.HideSpell();
        }
        realElapsedTime = 0f; // リセット
        if (currentPatternObj != null) Destroy(currentPatternObj);

        if (bulletClearPrefab != null)
        {
            GameObject effector = Instantiate(bulletClearPrefab, transform.position, Quaternion.identity);
            effector.GetComponent<BulletClearEffect>()?.StartClearing(transform.position);
        }

        int nextIndex = currentPhaseIndex + 1;
        bool hasNextPhase = nextIndex < phases.Count;

        if (hasNextPhase)
        {
            // --- 修正点：次のフェーズが「新しいバーを開始」または「バーを隠す」場合、古いバーを消す ---
            if ((phases[nextIndex].startsNewBar || phases[nextIndex].hideHealthBar) && currentUI != null)
            {
                Destroy(currentUI.gameObject);
                currentUI = null;
            }
            currentPhaseIndex = nextIndex;
            maxHP = phases[currentPhaseIndex].maxHP;
            currentHP = maxHP;
            flickerLifeThreshold = maxHP * 0.2f;
            // --- 修正：新しいフェーズの「成否」リセットをここで行う ---
            isSpellFailed = false;
            if (phases[currentPhaseIndex].declareImmediately) TriggerSpellDeclaration(currentPhaseIndex);
        }
        else if (currentUI != null) Destroy(currentUI.gameObject);

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
            if (!phases[currentPhaseIndex].declareImmediately) TriggerSpellDeclaration(currentPhaseIndex);
            InitializePhaseLogic(currentPhaseIndex);
            // --- 修正点：非表示設定でない場合のみ、ライフバーを生成する ---
            if (phases[currentPhaseIndex].startsNewBar && !phases[currentPhaseIndex].hideHealthBar)
            {
                SpawnHealthBar();
            }
            isTransitioning = false;
        }
        else Die();
        realElapsedTime = 0f; // 次のためにリセット
    }
}