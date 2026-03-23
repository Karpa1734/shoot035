using UnityEngine;
using System.Collections;

public static class DanmakuFunctions
{
    // --- 基本の生成関数 ---

    // 単発弾 (Shot01相当) [cite: 1-4]
    // 単発弾 (Shot01相当)
    public static GameObject CreateShotA1(GameObject prefab, BulletData data, Vector3 pos, float speed, float angle, float delay = 0)
    {
        if (BulletPool.Instance == null) return null;
        GameObject bulletObj = BulletPool.Instance.Get(prefab, pos, Quaternion.identity);
        EnemyBullet eb = bulletObj.GetComponent<EnemyBullet>();
        if (eb != null) eb.Initialize(speed, angle, 0, speed, 0, delay, data);
        return bulletObj;
    }

    // 高機能単発弾 (Shot02相当：加速・回転対応)
    public static GameObject CreateShotA2(GameObject prefab, BulletData data, Vector3 pos, float speed, float angle, float accel, float maxSpeed, float angVel, float delay = 0)
    {
        if (BulletPool.Instance == null) return null;
        GameObject bulletObj = BulletPool.Instance.Get(prefab, pos, Quaternion.identity);
        EnemyBullet eb = bulletObj.GetComponent<EnemyBullet>();
        if (eb != null) eb.Initialize(speed, angle, accel, maxSpeed, angVel, delay, data);
        return bulletObj;
    }

    // 全方位弾 (RoundShot01相当)
    public static void RoundShot01(GameObject prefab, BulletData data, Vector3 pos, int count, float speed, float startAngle, float delay = 0)
    {
        float step = 360f / count; // 360度をカウントで割る
        for (int i = 0; i < count; i++)
        {
            float targetAngle = startAngle + (step * i);
            CreateShotA1(prefab, data, pos, speed, targetAngle, delay);
        }
    }

    // 全方位弾 (RoundShot02相当：加速・回転対応)
    public static void RoundShot02(GameObject prefab, BulletData data, Vector3 pos, int count, float speed, float accel, float maxSpeed, float angVel, float startAngle, float delay = 0)
    {
        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float targetAngle = startAngle + (step * i);
            CreateShotA2(prefab, data, pos, speed, targetAngle, accel, maxSpeed, angVel, delay);
        }
    }
    // --- 定型弾幕関数 (function_bullet.txt の移植) ---

    // 扇状(nWay)弾 [cite: 1-4]
    public static void WideShot01(GameObject prefab, BulletData data, Vector3 pos, float speed, float angle, float wideAngle, int way, float delay = 0)
    {
        float wayAngle = 0;
        if (way > 1)
        {
            wayAngle = wideAngle / (way - 1); // [cite: 2]
            angle -= wideAngle / 2f; // [cite: 2]
        }
        for (int i = 0; i < way; i++)
        {
            CreateShotA1(prefab, data, pos, speed, angle, delay); // [cite: 3]
            angle += wayAngle; // [cite: 4]
        }
    }

    // 直線弾（速度差のある弾） [cite: 29-31]
    public static void LineShot(GameObject prefab, BulletData data, Vector3 pos, float speed, float maxSpeed, float angle, int num, float delay = 0)
    {
        float numSpeed = 0;
        if (num > 1)
        {
            numSpeed = (maxSpeed - speed) / (num - 1); // [cite: 29]
        }
        for (int i = 0; i < num; i++)
        {
            CreateShotA1(prefab, data, pos, speed, angle, delay); // [cite: 30]
            speed += numSpeed; // [cite: 31]
        }
    }

    // 扇状(nWay)の直線連弾 [cite: 32-36]
    public static void WideLineShot(GameObject prefab, BulletData data, Vector3 pos, float speed, float maxSpeed, float angle, float wideAngle, int way, int num, float delay = 0)
    {
        float numSpeed = 0;
        if (num > 1) numSpeed = (maxSpeed - speed) / (num - 1); // [cite: 32]

        float wayAngle = 0;
        if (way > 1)
        {
            wayAngle = wideAngle / (way - 1);
            angle -= wideAngle / 2f; // [cite: 33, 34]
        }

        for (int i = 0; i < way; i++)
        {
            float _speed = speed;
            for (int j = 0; j < num; j++)
            {
                CreateShotA1(prefab, data, pos, _speed, angle, delay); // [cite: 35]
                _speed += numSpeed;
            }
            angle += wayAngle; // [cite: 36]
        }
    }

    // 全方位の直線連弾 [cite: 37-40]
    public static void RoundLineShot(GameObject prefab, BulletData data, Vector3 pos, float speed, float maxSpeed, float angle, int way, int num, float delay = 0)
    {
        float numSpeed = 0;
        if (num > 1) numSpeed = (maxSpeed - speed) / (num - 1); // [cite: 37]

        float wayAngle = 360f / way; // [cite: 38]
        for (int i = 0; i < way; i++)
        {
            float _speed = speed;
            for (int j = 0; j < num; j++)
            {
                CreateShotA1(prefab, data, pos, _speed, angle, delay); // [cite: 39]
                _speed += numSpeed;
            }
            angle += wayAngle; // [cite: 40]
        }
    }

    // 反射弾の発射 [cite: 46-65, 89]
    // Unityでは弾自身に反射ロジックを持たせるのが一般的なため、
    // 生成時に反射回数等のパラメータを付与する形にします。
    public static GameObject CreateReflectShot(GameObject prefab, BulletData data, Vector3 pos, float speed, float angle, int reflectNum, float delay = 0)
    {
        // 通常の弾を生成
        GameObject bulletObj = CreateShotA1(prefab, data, pos, speed, angle, delay);

        if (bulletObj != null)
        {
            // 反射コンポーネントを動的に追加
            BulletReflector reflector = bulletObj.GetComponent<BulletReflector>();
            if (reflector == null)
            {
                reflector = bulletObj.AddComponent<BulletReflector>();
            }

            // 反射回数を設定
            reflector.SetReflectCount(reflectNum);
        }

        return bulletObj;
    }

    // --- 特定の値を取得する関数 (function_value.txt の移植) ---

    // 2点間の距離 [cite: 70]
    public static float GetGapLength(Vector2 a, Vector2 b)
    {
        return Vector2.Distance(a, b); // [cite: 71]
    }

    // 2点間の角度 [cite: 71]
    public static float GetGapAngle(Vector2 a, Vector2 b)
    {
        return Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg; // 
    }

    // 指定距離・角度のX座標 
    public static float GetGapX(float xA, float length, float angleDeg)
    {
        return xA + length * Mathf.Cos(angleDeg * Mathf.Deg2Rad); // [cite: 73]
    }

    // 指定距離・角度のY座標 [cite: 73]
    public static float GetGapY(float yA, float length, float angleDeg)
    {
        return yA + length * Mathf.Sin(angleDeg * Mathf.Deg2Rad);
    }
}