using KanKikuchi.AudioManager;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// フェーズの種類を定義
public enum PhaseType { Normal, SpellCard, Endurance }
// EnemyStatus.cs の冒頭（PhaseType の下あたり）に追加
public enum SpecialDropType { None, Bomb, BombPiece, Extend, ExtendPiece }
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

    // ★追加：このスペル専用の背景プレハブ
    [Tooltip("スペル発動時に生成する背景プレハブ。ルートにSpellBackgroundControllerが必要")]
    public GameObject spellBackgroundPrefab;

    [Header("Drop Items")]
    public int powerDropCount; // このフェーズ終了時に出すパワーアイテム数
    public int scoreDropCount; // このフェーズ終了時に出すスコアアイテム数
    // ★追加：特殊アイテムのドロップ設定
    public SpecialDropType specialDrop;
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

    public SpellRingEffect_Line spellRing; // インスペクターでセット

    // ★追加：現在表示中の背景のコントローラー
    private SpellBackgroundController currentBackground;
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
        // --- 追加：10点加算 ---
        if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(10);
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
        // --- 追加：10点加算 ---
        if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(10);
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

    // 既存の Die() は削除またはコメントアウトし、こちらに差し替え
    private IEnumerator DeathSequence(int bonus, float clearTime, float realTime, bool isGet, bool isFail)
    {
        isTransitioning = true;
        BossController ctrl = GetComponent<BossController>();
        if (ctrl != null) ctrl.SetMoving(true);

        SEManager.Instance.Play(SEPath.BOSS_END_BEGIN, 0.5f);

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), UnityEngine.Random.Range(-0.2f, 0.2f), 0);

        int totalFrames = 90;
        for (int i = 0; i <= totalFrames; i++)
        {
            float t = (float)i / totalFrames;
            float easedT = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(startPos, targetPos, easedT);

            if (i % 3 == 0)
            {
                BossEffectManager.Instance?.PlayBurstEffect(Color.white, 1, transform.position);
            }
            yield return null;
        }

        // ==========================================
        // ★ ここが「最後の爆発」タイミング：すべてを一斉に消去
        // ==========================================

        if (ctrl != null) ctrl.SetMoving(false);
        SEManager.Instance.Play(SEPath.BOSS_END_END, 0.5f);
        BossEffectManager.Instance?.PlayBurstEffect(Color.white, 60, transform.position);
        if (CameraShake.Instance != null) CameraShake.Instance.Shake(1.0f, 0.6f);
        // ★追加：大爆発の前に弾をすべてアイテムに変えておく
        ConvertBulletsToItems();
        // 1. スペル名表示を引っ込める [cite: 21, 22]
        if (enemySpellUI != null) enemySpellUI.HideSpell();

        // 2. タイマーを非表示にする（isTimerActiveをfalseにするだけでなくUI自体を消す） 
        isTimerActive = false;
        if (BossTimerUI.Instance != null) BossTimerUI.Instance.gameObject.SetActive(false);

        // 3. 魔法陣（スペルリング）を消す [cite: 21]
        if (spellRing != null) spellRing.Deactivate();
        // ★追加：ボスの名前とライフカウント（星）を消す
        if (BossLifeCountUI.Instance != null) BossLifeCountUI.Instance.Hide();
        // 4. オーラ/チャージエフェクトなどを強制停止
        // ボスの子オブジェクトとして配置されている演出用パーツをすべてオフにする
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }

        // 5. ボーナスリザルトを表示
        if (enemySpellUI != null)
        {
            enemySpellUI.ShowSpellResult(bonus, clearTime, realTime, isGet, isFail);
        }

        // ==========================================
        // ★追加：撃破時のアイテムドロップ
        // ==========================================
        if (ItemSpawner.Instance != null)
        {
            ItemSpawner.Instance.DropItemsOnDeath(
                transform.position,
                phases[currentPhaseIndex].powerDropCount,
                phases[currentPhaseIndex].scoreDropCount
            );
        }
        // ★追加：特殊アイテムのドロップ
        DropSpecialItem(currentPhaseIndex);
        if (isGet && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore((long)bonus);
        }
        // ==========================================
        // ★追加：専用背景を爆発に合わせて消去
        // ==========================================
        if (currentBackground != null)
        {
            currentBackground.FadeOutAndDestroy();
            currentBackground = null;
        }
        // 本体を非表示にして、UIが消えるのを待ってから Destroy
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        yield return new WaitForSeconds(4.5f);
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
        // 輝針城風の青白いチャージ演出
        //BossEffectManager.Instance?.PlayChargeEffect(0.5f, new Color(0.5f, 0.8f, 1f, 0.8f));
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
        // 追加：現在のフェーズデータを変数に代入して使いやすくする
        var phase = phases[index];
        if (BossTimerUI.Instance != null) BossTimerUI.Instance.SetPhaseType(phases[index].type);

        for (int i = 0; i < 4; i++)
        {
            BossEffectManager.Instance?.PlayChargeEffect(0.3f,Color.white,transform.position);
        }

        // --- 修正：カウントダウンと計測を「宣言（移動開始）」と同時に開始する ---
        currentTimer = phases[index].timeLimit;
        isTimerActive = currentTimer > 0;
        realElapsedTime = 0f;
        if (spellRing != null)
        {
            // スペルカードまたは耐久フェーズならリングを出す
            if (phases[index].type == PhaseType.SpellCard || phases[index].type == PhaseType.Endurance)
                spellRing.Activate(phases[index].timeLimit);
            else
                spellRing.Deactivate();
        }
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

        // ==========================================
        // ★追加：専用背景の生成とフェードイン
        // ==========================================
        if (phase.spellBackgroundPrefab != null)
        {
            // すでに背景があれば消す（念のため）
            if (currentBackground != null) currentBackground.FadeOutAndDestroy();

            // 生成 (カメラの奥、または専用レイヤーに配置する設定がプレハブ側に必要)
            GameObject bgObj = Instantiate(phase.spellBackgroundPrefab, Vector3.zero, Quaternion.identity);
            currentBackground = bgObj.GetComponent<SpellBackgroundController>();

            if (currentBackground != null)
            {
                currentBackground.FadeIn();
            }
            else
            {
                Debug.LogError($"背景プレハブ {phase.spellBackgroundPrefab.name} に SpellBackgroundController が付いていません。");
            }
        }

    }
    /// <summary>
    /// 画面内のすべての敵弾を SCORE00 アイテムに変換する
    /// </summary>
    public void ConvertBulletsToItems()
    {
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("EnemyBullet");

        foreach (GameObject bulletObj in bullets)
        {
            // 1. アイテムを生成（即座に回収モードをON）
            if (ItemSpawner.Instance != null)
            {
                ItemSpawner.Instance.SpawnItem(ItemController.ITEM_TYPE.SCORE00, bulletObj.transform.position, true);
            }

            // 2. ★修正：Destroy ではなく弾の消滅メソッドを呼ぶ
            EnemyBullet bulletScript = bulletObj.GetComponent<EnemyBullet>();
            if (bulletScript != null)
            {
                // 引数に true を渡すことで、弾側の「消滅エフェクト再生」を走らせる
                bulletScript.Deactivate(true);
            }
            else
            {
                // スクリプトがない場合のみ Destroy で対応
                Destroy(bulletObj);
            }
        }
    }
    IEnumerator PhaseTransitionSequence()
    {
        isTransitioning = true;
        int nextIndex = currentPhaseIndex + 1;
        bool hasNextPhase = nextIndex < phases.Count;
        if (hasNextPhase)
        {
            if (spellRing != null) spellRing.Deactivate();
        }
        // 1. リザルト用データの計算（重複を整理）
        int bonusAmount = (int)currentSpellBonus;
        float clearTime = phases[currentPhaseIndex].timeLimit - currentTimer;
        bool isTimeUp = currentTimer <= 0.01f;
        bool isCaptureTimeoutFailure = (phases[currentPhaseIndex].type == PhaseType.SpellCard) && !isSpellFailed && isTimeUp;

        bool isGetBonus = false;
        if (!isSpellFailed)
        {
            if (phases[currentPhaseIndex].type == PhaseType.Endurance) isGetBonus = isTimeUp;
            else isGetBonus = currentHP <= 0;
        }

        // スペル名などの UI を一旦隠す（リザルト表示は次で行う）
        if (hasNextPhase && enemySpellUI != null) enemySpellUI.HideSpell();
        if (currentPatternObj != null) Destroy(currentPatternObj);
        // ========================================================
        // ★修正ポイント：途中フェーズの場合のみ、ここで背景を消す
        // ========================================================
        if (hasNextPhase && currentBackground != null)
        {
            currentBackground.FadeOutAndDestroy();
            currentBackground = null;
        }
        // 弾消しエフェクトの発生タイミング
        if (bulletClearPrefab != null)
        {
            // ★追加：エフェクト発生と同時に、本物の弾をアイテムに変える
            ConvertBulletsToItems();

            GameObject effector = Instantiate(bulletClearPrefab, transform.position, Quaternion.identity);
            effector.GetComponent<BulletClearEffect>()?.StartClearing(transform.position);
        }


        if (hasNextPhase)
        {
            // ========================================================
            // 途中フェーズ：その場でリザルトを表示
            // ========================================================
            if (enemySpellUI != null && (phases[currentPhaseIndex].type == PhaseType.SpellCard || phases[currentPhaseIndex].type == PhaseType.Endurance))
            {
                enemySpellUI.ShowSpellResult(bonusAmount, clearTime, realElapsedTime, isGetBonus, isCaptureTimeoutFailure);
                if (isGetBonus && ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.AddScore((long)bonusAmount);
                }
            }

            // 次のフェーズの準備
            if ((phases[nextIndex].startsNewBar || phases[nextIndex].hideHealthBar) && currentUI != null)
            {
                Destroy(currentUI.gameObject);
                currentUI = null;
            }
            if (ItemSpawner.Instance != null)
            {
                // 現在のフェーズのドロップ設定を参照
                ItemSpawner.Instance.DropItemsOnDeath(
                    transform.position,
                    phases[currentPhaseIndex].powerDropCount,
                    phases[currentPhaseIndex].scoreDropCount
                );
            }
            // ★追加：特殊アイテムのドロップ
            DropSpecialItem(currentPhaseIndex);
            currentPhaseIndex = nextIndex;
            maxHP = phases[currentPhaseIndex].maxHP;
            currentHP = maxHP;
            flickerLifeThreshold = maxHP * 0.2f;
            isSpellFailed = false;

            if (phases[currentPhaseIndex].declareImmediately) TriggerSpellDeclaration(currentPhaseIndex);

            // 移動処理
            BossController ctrl = GetComponent<BossController>();
            if (ctrl != null) ctrl.SetMoving(true);
            Vector3 transitionTarget = new Vector3(-2.0f, 2.0f, 0);
            while (Vector3.Distance(transform.position, transitionTarget) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, transitionTarget, 3f * Time.deltaTime);
                yield return null;
            }
            if (ctrl != null) ctrl.SetMoving(false);

            yield return new WaitForSeconds(0.5f);

            if (!phases[currentPhaseIndex].declareImmediately) TriggerSpellDeclaration(currentPhaseIndex);
            InitializePhaseLogic(currentPhaseIndex);

            if (phases[currentPhaseIndex].startsNewBar && !phases[currentPhaseIndex].hideHealthBar)
            {
                SpawnHealthBar();
            }
            isTransitioning = false;
        }
        else
        {
            // ========================================================
            // 最終フェーズ：背景は消さずに DeathSequence へ
            // ========================================================
            if (currentUI != null) Destroy(currentUI.gameObject);
            StartCoroutine(DeathSequence(bonusAmount, clearTime, realElapsedTime, isGetBonus, isCaptureTimeoutFailure));
        }

        realElapsedTime = 0f;
    }
    private void DropSpecialItem(int phaseIndex)
    {
        if (ItemSpawner.Instance == null) return;

        SpecialDropType dropType = phases[phaseIndex].specialDrop;
        if (dropType == SpecialDropType.None) return;

        ItemController.ITEM_TYPE itemToSpawn;

        // enum から ITEM_TYPE へ変換
        switch (dropType)
        {
            case SpecialDropType.Bomb: itemToSpawn = ItemController.ITEM_TYPE.BOMB_UP01; break;
            case SpecialDropType.BombPiece: itemToSpawn = ItemController.ITEM_TYPE.BOMB_UP02; break;
            case SpecialDropType.Extend: itemToSpawn = ItemController.ITEM_TYPE.LIFE_UP01; break;
            case SpecialDropType.ExtendPiece: itemToSpawn = ItemController.ITEM_TYPE.LIFE_UP02; break;
            default: return;
        }

        // アイテムを1つ生成
        ItemSpawner.Instance.SpawnItem(itemToSpawn, transform.position);
    }
}