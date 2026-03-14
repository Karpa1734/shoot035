using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject bossPrefab;   // ボスのプレハブ
    public GameObject markerPrefab; // エネミーマーカーのプレハブ

    [Header("Spawn Settings")]
    public Vector3 bossSpawnPosition = new Vector3(0, -2, 0);

    void Start()
    {
        // 今回は起動と同時にボスを出す設定
        Invoke("SpawnBoss", 1f); // 1秒後にボスを出す
    }

    public void SpawnBoss()
    {
        // 1. ボスを生成
        GameObject bossObj = Instantiate(bossPrefab, bossSpawnPosition, Quaternion.identity);

        // 2. ボスから EnemyStatus コンポーネントを取得
        EnemyStatus status = bossObj.GetComponent<EnemyStatus>();

        if (status != null && markerPrefab != null)
        {
            // 3. エネミーマーカーを生成
            GameObject markerObj = Instantiate(markerPrefab);
            EnemyMarker markerScript = markerObj.GetComponent<EnemyMarker>();

            // 4. マーカーに「誰を追跡するか（Status）」を教える
            if (markerScript != null)
            {
                markerScript.targetStatus = status;
            }
        }
    }
}