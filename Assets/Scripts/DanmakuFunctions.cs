using UnityEngine;

public static class DanmakuFunctions
{
    // ’P”­’e (delay ˆø”‚ğ’Ç‰Á)
    public static GameObject CreateShotA1(GameObject prefab, BulletData data, Vector3 pos, float speed, float angle, float delay = 0)
    {
        if (BulletPool.Instance == null) return null;

        GameObject bulletObj = BulletPool.Instance.Get(prefab, pos, Quaternion.identity);
        EnemyBullet eb = bulletObj.GetComponent<EnemyBullet>();

        // EnemyBullet ‚Ì Initialize ‚É delay ‚Æ data ‚ğ“n‚·
        if (eb != null) eb.Initialize(speed, angle, 0, speed, 0, delay, data);

        return bulletObj;
    }

    // ‘S•ûˆÊ’e (delay ˆø”‚ğ’Ç‰Á)
    public static void CreateRing(GameObject prefab, BulletData data, Vector3 pos, int count, float speed, float startAngle, float delay = 0)
    {
        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float targetAngle = startAngle + (step * i);
            CreateShotA1(prefab, data, pos, speed, targetAngle, delay);
        }
    }
}