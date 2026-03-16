using KanKikuchi.AudioManager;
using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerHitHandler : MonoBehaviour
{
    public enum PlayerState { Normal, DeathBomb, Hit, Down, Rebirth }
    public PlayerState currentState = PlayerState.Normal;

    [Header("Settings")]
    public float deathBombWindow = 0.15f;
    public float invincibilityTime = 3.0f;
    public float downTime = 0.8f;

    [Header("References")]
    public GameObject explosionEffectPrefab;
    public PlayerAnimation playerAnim;
    public PlayerMove playerMove;

    [Header("Bullet Clear")]
    public GameObject bulletClearPrefab;

    // --- 追加：本体のSpriteRendererを特定するため ---
    private SpriteRenderer characterRenderer;

    void Awake()
    {
        // 親オブジェクトや他の子オブジェクトから必要なコンポーネントを自動取得
        if (playerMove == null) playerMove = GetComponentInParent<PlayerMove>();
        if (playerAnim == null) playerAnim = GetComponentInParent<PlayerAnimation>();

        // 通常、キャラの画像は親か別の子にあるので、それを見つける
        characterRenderer = GetComponentInParent<SpriteRenderer>();
        if (characterRenderer == null)
        {
            characterRenderer = transform.parent.GetComponentInChildren<SpriteRenderer>();
        }
    }

    void Update()
    {
        if (playerMove != null && playerAnim != null)
        {
            playerAnim.isInvincible = playerMove.IsInvincible;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 敵弾に触れた場合、無敵に関わらず弾を消す
        if (collision.CompareTag("EnemyBullet"))
        {
            EnemyBullet bullet = collision.GetComponent<EnemyBullet>();
            if (bullet != null) bullet.Deactivate(true);
        }

        // 2. ミス判定のガード
        if (playerMove.IsInvincible || currentState != PlayerState.Normal) return;

        // 3. 被弾開始（敵弾または敵本体）
        if (collision.CompareTag("EnemyBullet") || collision.CompareTag("Enemy"))
        {
            // --- 追加：被弾した「瞬間」にスペル失敗を通知する ---
            // シーン内の EnemyStatus を探して通知
            EnemyStatus boss = FindObjectOfType<EnemyStatus>();
            if (boss != null)
            {
                boss.FailSpell();
            }

            currentState = PlayerState.DeathBomb;
            StartCoroutine(CheckDeathBombRoutine());
        }
    }

    IEnumerator CheckDeathBombRoutine()
    {
        SEManager.Instance.Play(SEPath.SE_PLAYER_COLLISION, 0.3f);
        playerMove.StartDeathBombWindow(deathBombWindow);

        while (playerMove.IsInDeathBombWindow)
        {
            yield return null;
        }

        if (playerMove.IsInvincible)
        {
            currentState = PlayerState.Normal;
            yield break;
        }

        StartCoroutine(ExplosionAndRebirthRoutine());
    }

    IEnumerator ExplosionAndRebirthRoutine()
    {
        if (playerMove.IsInvincible)
        {
            currentState = PlayerState.Normal;
            yield break;
        }

        // 被弾位置は現在の座標でOK（エフェクト用）
        Vector3 deathPos = transform.position;
        currentState = PlayerState.Hit;

        // 1. エフェクト生成
        if (explosionEffectPrefab != null) Instantiate(explosionEffectPrefab, deathPos, Quaternion.identity);

        // 2. 弾消し
        if (bulletClearPrefab != null)
        {
            GameObject clearObj = Instantiate(bulletClearPrefab);
            clearObj.SendMessage("StartClearing", deathPos, SendMessageOptions.DontRequireReceiver);
        }

        // --- 修正ポイント：親（Player本体）ごと画面外へ飛ばす ---
        playerMove.enabled = false;
        transform.parent.position = new Vector3(-2.0f, -100f, 0);
        if (characterRenderer != null) characterRenderer.enabled = false;

        yield return new WaitForSeconds(downTime);

        // --- 復帰処理：親（Player本体）の座標を戻す ---
        currentState = PlayerState.Rebirth;
        transform.parent.position = new Vector3(-2.0f, -6.0f, 0);
        if (characterRenderer != null) characterRenderer.enabled = true;

        float elapsed = 0;
        Vector3 startPos = transform.parent.position;
        Vector3 targetPos = new Vector3(-2.0f, -3.5f, 0);
        while (elapsed < 0.6f)
        {
            // 親の座標をアニメーションさせる
            transform.parent.position = Vector3.Lerp(startPos, targetPos, elapsed / 0.6f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerMove.enabled = true;
        currentState = PlayerState.Normal;
        playerMove.SetInvincible(invincibilityTime);
    }
}