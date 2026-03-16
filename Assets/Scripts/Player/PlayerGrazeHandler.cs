using UnityEngine;
using KanKikuchi.AudioManager;

public class PlayerGrazeHandler : MonoBehaviour
{
    [Header("References")]
    public GameObject grazeEffectPrefab; // eff_splash.png を使ったエフェクト

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("EnemyBullet"))
        {
            EnemyBullet bullet = collision.GetComponent<EnemyBullet>();

            // まだグレイズされていない弾かチェック
            if (bullet != null && bullet.TryGraze())
            {
                DoGraze(collision.transform.position);
            }
        }
    }

    private void DoGraze(Vector3 bulletPos)
    {
        // 1. SEの再生 (Reimu.txt の SE_GRAZE に相当) 
        // SEPath.SE_GRAZE は file.txt で定義されているパス 
        SEManager.Instance.Play(SEPath.SE_GRAZE, 0.5f);

        // 2. スコア加算処理（必要に応じて追加）
        // ScoreManager.Instance.AddGraze();

        // 3. エフェクトの生成
        if (grazeEffectPrefab != null)
        {
            // 自機と弾の中間地点に生成する (Effect.txt の計算式を再現) 
            Vector3 grazePos = (transform.position + bulletPos) / 2f;
            Instantiate(grazeEffectPrefab, grazePos, Quaternion.identity);
        }
    }
}