using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BulletClearEffect : MonoBehaviour
{
    public float expandSpeed = 20f; // 円が広がる速度
    public float maxRadius = 15f;   // 画面全体を覆うのに十分な半径

    public void StartClearing(Vector3 center)
    {
        transform.position = center;
        StartCoroutine(ClearRoutine());
    }

    IEnumerator ClearRoutine()
    {
        float currentRadius = 0f;

        while (currentRadius < maxRadius)
        {
            currentRadius += expandSpeed * Time.deltaTime;

            // 画面内の「EnemyBullet」タグが付いた全オブジェクトを取得
            GameObject[] bullets = GameObject.FindGameObjectsWithTag("EnemyBullet");

            foreach (GameObject b in bullets)
            {
                if (b == null) continue;

                // 中心からの距離を計算
                float distance = Vector2.Distance(transform.position, b.transform.position);

                // 範囲内に入っていたら消滅エフェクト付きで消す
                if (distance < currentRadius)
                {
                    EnemyBullet eb = b.GetComponent<EnemyBullet>();
                    if (eb != null)
                    {
                        eb.Deactivate(true); // 消滅アニメーションを再生して消す
                    }
                }
            }

            yield return null;
        }

        // 全て消し終わったらこのエフェクトオブジェクト自体を破棄
        Destroy(gameObject);
    }
}