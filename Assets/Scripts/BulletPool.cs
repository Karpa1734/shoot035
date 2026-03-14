using UnityEngine;
using System.Collections.Generic;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance;

    // プレハブごとにスタック（在庫）を管理する辞書
    private Dictionary<GameObject, Stack<GameObject>> poolDict = new Dictionary<GameObject, Stack<GameObject>>();

    void Awake()
    {
        Instance = this;
    }

    // プールから弾を取得する
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!poolDict.ContainsKey(prefab))
        {
            poolDict[prefab] = new Stack<GameObject>();
        }

        GameObject obj;
        if (poolDict[prefab].Count > 0)
        {
            // 在庫があるなら再利用
            obj = poolDict[prefab].Pop();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
        }
        else
        {
            // 在庫がないなら新規生成
            obj = Instantiate(prefab, position, rotation);
            // 弾側にどのプレハブの在庫に戻るべきかを教える（後述のスクリプト用）
            obj.GetComponent<EnemyBullet>().originPrefab = prefab;
        }
        return obj;
    }

    // プールに弾を戻す（非アクティブ化）
    public void Release(GameObject prefab, GameObject obj)
    {
        obj.SetActive(false);
        poolDict[prefab].Push(obj);
    }
}