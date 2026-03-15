using KanKikuchi.AudioManager;
using System.Collections;
using UnityEngine;

public class PlayerHitHandler : MonoBehaviour
{
    // DeathBombState（被弾猶予中）を追加
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

    void Update()
    {
        if (playerMove != null && playerAnim != null)
        {
            playerAnim.isInvincible = playerMove.IsInvincible;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 敵弾に触れた場合、無敵状態に関わらず弾を消去する
        if (collision.CompareTag("EnemyBullet"))
        {
            EnemyBullet bullet = collision.GetComponent<EnemyBullet>();
            if (bullet != null)
            {
                // 弾をプールに戻す、または破壊する処理（消滅エフェクトあり）
                bullet.Deactivate(true);
            }
        }

        // 2. ミス（被弾）判定を行うかどうかのガード
        // 無敵中、またはすでに被弾処理（猶予中含む）中なら、ここから先のミス処理は行わない
        if (playerMove.IsInvincible || currentState != PlayerState.Normal) return;

        // 3. ミス（被弾）の開始判定
        // 敵弾、または敵本体（ボス）に接触した場合
        if (collision.CompareTag("EnemyBullet") || collision.CompareTag("Enemy"))
        {
            // 当たった瞬間に状態を「被弾猶予中」に変え、食らいボム判定へ
            currentState = PlayerState.DeathBomb;
            StartCoroutine(CheckDeathBombRoutine());
        }
    }

    IEnumerator CheckDeathBombRoutine()
    {
        SEManager.Instance.Play(SEPath.SE_PLAYER_COLLISION,0.3f);
        // 食らいボム猶予時間を開始
        playerMove.StartDeathBombWindow(deathBombWindow);

        // 猶予時間が終わるか、ボムが成功して無敵になるまで待機
        while (playerMove.IsInDeathBombWindow)
        {
            yield return null;
        }

        // --- 修正ポイント：最終判定 ---
        // 猶予が終わった後、プレイヤーが無敵（ボム発動済み）ならミスをキャンセルする
        if (playerMove.IsInvincible)
        {
            currentState = PlayerState.Normal;
            yield break; // ミス処理を行わずに終了
        }

        // ボムが間に合わなかった場合のみミス処理へ
        StartCoroutine(ExplosionAndRebirthRoutine());
    }

    IEnumerator ExplosionAndRebirthRoutine()
    {
        // 念のため、開始直前にもう一度チェック（1フレームの隙を潰す）
        if (playerMove.IsInvincible)
        {
            currentState = PlayerState.Normal;
            yield break;
        }

        Vector3 deathPos = transform.position;
        currentState = PlayerState.Hit;

        // 1. 爆発エフェクトを生成
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, deathPos, Quaternion.identity);
        }

        // 2. 弾消し効果を生成
        if (bulletClearPrefab != null)
        {
            GameObject clearObj = Instantiate(bulletClearPrefab);
            clearObj.SendMessage("StartClearing", deathPos, SendMessageOptions.DontRequireReceiver);
        }

        // 自機を一時的に無効化
        playerMove.enabled = false;
        transform.position = new Vector3(-2.0f, -100f, 0);
        GetComponent<SpriteRenderer>().enabled = false;

        yield return new WaitForSeconds(downTime);

        // --- 復帰処理 ---
        currentState = PlayerState.Rebirth;
        transform.position = new Vector3(-2.0f, -6.0f, 0);
        GetComponent<SpriteRenderer>().enabled = true;

        float elapsed = 0;
        Vector3 startPos = transform.position;
        Vector3 targetPos = new Vector3(-2.0f, -3.5f, 0);
        while (elapsed < 0.6f)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / 0.6f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerMove.enabled = true;
        currentState = PlayerState.Normal;
        playerMove.SetInvincible(invincibilityTime);
    }
}