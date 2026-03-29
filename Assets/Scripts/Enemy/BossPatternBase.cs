using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BossPatternBase : MonoBehaviour
{
    protected BossController controller;
    protected EnemyStatus status;

    protected readonly float CENTER_X = -2.0f;
    protected readonly float CENTER_Y = 0.0f;

    [Header("Move Area Settings")]
    protected Vector2 moveAreaMin = new Vector2(-4.5f, 1.5f);
    protected Vector2 moveAreaMax = new Vector2(0.5f, 3.5f);

    protected Collider2D parentCollider;
    protected SpriteRenderer bossRenderer;

    // --- 弾データ配列 ---
    protected BulletData[] RED => BulletManager.Instance.RED;
    protected BulletData[] ORANGE => BulletManager.Instance.ORANGE;
    protected BulletData[] YELLOW => BulletManager.Instance.YELLOW;
    protected BulletData[] GREEN => BulletManager.Instance.GREEN;
    protected BulletData[] AQUA => BulletManager.Instance.AQUA;
    protected BulletData[] BLUE => BulletManager.Instance.BLUE;
    protected BulletData[] PURPLE => BulletManager.Instance.PURPLE;
    protected BulletData[] WHITE => BulletManager.Instance.WHITE;

    protected Material GetAdditive() => BulletManager.Instance.additiveMaterial;
    protected GameObject GetLaserHeadPrefab() => BulletManager.Instance.laserStreamHeadPrefab;


    protected virtual void Awake()
    {
        controller = GetComponentInParent<BossController>();
        status = GetComponentInParent<EnemyStatus>();
    }

    #region Laser Methods (Fixed index [2] and Additive)

    /// <summary> 
    /// 全ての弾が同じ軌道をなぞる「共通軌道型」レーザーを射出します。
    /// 画像は自動的に各色の[2]（中弾・長弾サイズ想定）を使用し、加算合成が適用されます。
    /// </summary>
    protected void CreateMultiTransformLaser(float x, float y, float width, int count, BulletData[] colorArray,
                                             float speed, float angle, float delay, List<EnemyBullet.BulletTransformData> transforms,
                                             float accel = 0, float angVel = 0, float maxSpeed = -1f,
                                             int interval = 2) // ★最後に追加
    {
        GameObject prefab = GetLaserHeadPrefab();
        if (colorArray == null || colorArray.Length < 3 || prefab == null) return;
        BulletData data = colorArray[2];

        GameObject obj = Instantiate(prefab);
        EnemyLaserStream laser = obj.GetComponent<EnemyLaserStream>();

        // Setupに interval を渡す
        laser.Setup(x, y, width, count, data, delay, GetAdditive(), speed, angle, accel, angVel, maxSpeed, interval);

        foreach (var t in transforms)
        {
            laser.AddTransformData(t);
        }

        laser.Fire();
    }

    #endregion



    // --- 弾生成関数（配列から直接渡されたデータを使用） ---

    protected GameObject CreateShot(BulletData data, Vector3 position, float speed, float angle, float delay = 0, bool isAdditive = false)
    {
        Material mat = isAdditive ? GetAdditive() : null;
        return DanmakuFunctions.CreateShotA1(data.bulletPrefab, data, position, speed, angle, delay, mat);
    }
    /// <summary>
    /// 複数回の軌道変化を行う弾を生成する
    /// </summary>
    protected GameObject CreateMultiTransformShot(BulletData data, Vector3 position, float speed, float angle, float delay, List<EnemyBullet.BulletTransformData> transforms)
    {
        if (data == null) return null;

        if (BulletPool.Instance == null) return null;
        GameObject bulletObj = BulletPool.Instance.Get(data.bulletPrefab, position, Quaternion.identity);
        EnemyBullet eb = bulletObj.GetComponent<EnemyBullet>();

        if (eb != null)
        {
            eb.InitializeMultiTransform(speed, angle, delay, data, transforms);
        }
        return bulletObj;
    }
    protected void CreateRoundShot(BulletData data, Vector3 position, int count, float speed, float startAngle, float delay = 0, bool isAdditive = false)
    {
        Material mat = isAdditive ? GetAdditive() : null;
        if (data == null) return;
        DanmakuFunctions.RoundShot01(data.bulletPrefab, data, position, count, speed, startAngle, delay, mat);
    }

    protected void CreateRoundShot02(BulletData data, Vector3 position, int count, float speed, float accel, float maxSpeed, float angVel, float startAngle, float delay = 0, bool isAdditive = false)
    {
        Material mat = isAdditive ? GetAdditive() : null;
        if (data == null) return;
        DanmakuFunctions.RoundShot02(data.bulletPrefab, data, position, count, speed, accel, maxSpeed, angVel, startAngle, delay, mat);
    }

    protected void CreateWideShot(BulletData data, Vector3 position, float speed, float angle, float wideAngle, int way, float delay = 0, bool isAdditive = false)
    {
        Material mat = isAdditive ? GetAdditive() : null;
        if (data == null) return;
        DanmakuFunctions.WideShot01(data.bulletPrefab, data, position, speed, angle, wideAngle, way, delay, mat);
    }

    protected void CreateLineShot(BulletData data, Vector3 position, float speed, float maxSpeed, float angle, int count, float delay = 0, bool isAdditive = false)
    {
        Material mat = isAdditive ? GetAdditive() : null;
        if (data == null) return;
        DanmakuFunctions.LineShot(data.bulletPrefab, data, position, speed, maxSpeed, angle, count, delay, mat);
    }

    protected void CreateRoundLineShot(BulletData data, Vector3 position, float speed, float maxSpeed, float angle, int way, int count, float delay = 0, bool isAdditive = false)
    {
        Material mat = isAdditive ? GetAdditive() : null;
        if (data == null) return;
        DanmakuFunctions.RoundLineShot(data.bulletPrefab, data, position, speed, maxSpeed, angle, way, count, delay, mat);
    }

    protected GameObject CreateReflectShot(
        BulletData data, Vector3 position, float speed, float angle, int count,
        bool L, bool R, bool U, bool D, float delay = 0, float accel = 0, BulletData nextData = null, bool isAdditive = false)
    {
        Material mat = isAdditive ? GetAdditive() : null;
        if (data == null) return null;
        return DanmakuFunctions.CreateReflectShot(data.bulletPrefab, data, position, speed, angle, count, L, R, U, D, accel, nextData, delay, mat);
    }

    protected GameObject CreateGravityShot(BulletData data, Vector3 position, float speed, float angle, float gAccel, float gAngle, float gMax, float delay = 0, bool isAdditive = false)
    {
        Material mat = isAdditive ? GetAdditive() : null;
        if (data == null) return null;
        return DanmakuFunctions.CreateGravityShot(data.bulletPrefab, data, position, speed, angle, gAccel, gAngle, gMax, delay, mat);
    }

    // --- 移動・特殊演出関連の共通メソッド ---

    public IEnumerator SetMovePosition03(float tx, float ty, float weight)
    {
        if (controller != null) controller.SetMoving(true);
        Vector3 startPos = transform.parent.position;
        Vector3 targetPos = new Vector3(tx, ty, 0);

        float duration = Mathf.Max(0.01f, weight / 60.0f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.parent.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.parent.position = targetPos;
        if (controller != null) controller.SetMoving(false);
    }

    public IEnumerator SetMovePositionRand03(float minX, float maxX, float minY, float maxY, float weight)
    {
        Vector3 currentPos = transform.parent.position;
        float moveY = Random.Range(minY, maxY);
        float centerY = (moveAreaMin.y + moveAreaMax.y) * 0.5f;
        float targetY = (currentPos.y > centerY) ? currentPos.y - moveY : currentPos.y + moveY;

        float targetX;
        if (PlayerMove.Instance != null)
        {
            float playerX = PlayerMove.Instance.transform.position.x;
            float moveX = Random.Range(minX, maxX);
            targetX = (playerX > currentPos.x) ? currentPos.x + moveX : currentPos.x - moveX;
        }
        else
        {
            targetX = currentPos.x + Random.Range(-maxX, maxX);
        }

        targetX = Mathf.Clamp(targetX, moveAreaMin.x, moveAreaMax.x);
        targetY = Mathf.Clamp(targetY, moveAreaMin.y, moveAreaMax.y);

        yield return StartCoroutine(SetMovePosition03(targetX, targetY, weight));
    }

    protected IEnumerator FadeToStealth(float fadeDuration, float targetAlpha = 0f)
    {
        if (parentCollider == null) parentCollider = GetComponentInParent<Collider2D>();
        if (bossRenderer == null) bossRenderer = transform.parent.GetComponentInChildren<SpriteRenderer>();

        if (parentCollider != null) parentCollider.enabled = false;

        if (bossRenderer != null)
        {
            float elapsed = 0f;
            Color startColor = bossRenderer.color;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(startColor.a, targetAlpha, elapsed / fadeDuration);
                bossRenderer.color = new Color(startColor.r, startColor.g, startColor.b, a);
                yield return null;
            }
        }
    }
    // BossPatternBase.cs 修正版

    private Dictionary<int, EnemyLaserBeam> beamDict = new Dictionary<int, EnemyLaserBeam>();

    // --- Laser A (設置型) ---
    // 引数を BulletData から BulletManager.LaserColor に変更
    // --- Laser A (設置型) ---
    protected void CreateLaserA(int id, float x, float y, float length, float width, BulletManager.LaserColor color, int delay)
    {
        // プレハブを極太レーザー専用のものに変更
        GameObject obj = Instantiate(BulletManager.Instance.laserBeamPrefab);
        EnemyLaserBeam beam = obj.GetComponent<EnemyLaserBeam>();
        beam.SetupA(x, y, length, width, color, delay);
        beamDict[id] = beam;
    }

    // --- Laser B (ボス追従型) ---
    protected void CreateLaserB(int id, float length, float width, BulletManager.LaserColor color, int delay)
    {
        GameObject obj = Instantiate(BulletManager.Instance.laserBeamPrefab);
        EnemyLaserBeam beam = obj.GetComponent<EnemyLaserBeam>();
        beam.SetupB(length, width, color, delay, transform.parent);
        beamDict[id] = beam;
    }

    // --- Laser A (設置型) ---
    // 引数の最後に bool startClosing を追加（デフォルト値 false）
    protected void SetLaserDataA(int id, int frame, float angle, float angVel, float lengthVel, float moveSpeed, float moveAngle, bool startClosing = false)
    {
        if (!beamDict.ContainsKey(id)) return;
        beamDict[id].AddData(new EnemyLaserBeam.LaserTransformData
        {
            frame = frame,
            angle = angle,
            angVel = angVel,
            lengthVel = lengthVel,
            moveSpeed = moveSpeed,
            moveAngle = moveAngle,
            startClosing = startClosing // フラグを渡す
        });
    }

    // --- Laser B (ボス追従型) ---
    // 引数の最後に bool startClosing を追加
    protected void SetLaserDataB(int id, int frame, float lengthVel, float dist, float distVel, float dAngle, float dAngleVel, float lAngle, float lAngleVel, bool startClosing = false)
    {
        if (!beamDict.ContainsKey(id)) return;
        beamDict[id].AddData(new EnemyLaserBeam.LaserTransformData
        {
            frame = frame,
            lengthVel = lengthVel,
            dist = dist,
            distVel = distVel,
            distAngle = dAngle,
            distAngleVel = dAngleVel,
            laserAngle = lAngle,
            laserAngleVel = lAngleVel,
            startClosing = startClosing // フラグを渡す
        });
    }

    // 共通の発射関数
    protected void FireShot(int id)
    {
        if (beamDict.TryGetValue(id, out EnemyLaserBeam beam))
        {
            beam.Fire();
            beamDict.Remove(id);
        }
    }
    protected virtual void OnDestroy()
    {
        if (parentCollider != null) parentCollider.enabled = true;
        if (bossRenderer != null)
        {
            bossRenderer.color = new Color(bossRenderer.color.r, bossRenderer.color.g, bossRenderer.color.b, 1f);
        }
    }
}