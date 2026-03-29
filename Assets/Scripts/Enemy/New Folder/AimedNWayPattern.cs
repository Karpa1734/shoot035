using KanKikuchi.AudioManager;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AimedNWayPattern : BossPatternBase
{
    [Header("Bullet Settings")]
    public BulletData bulletData;
    public BulletData bulletData2;
    public int nWay = 5;
    public float spreadAngle = 30f;
    public float bulletSpeed = 4f;
    public float fireInterval = 1.2f; // 間隔を少し短く

    [Header("Movement Settings (Kibikibi!)")]
    public float moveMinX = 0.5f;
    public float moveMaxX = 1.5f;
    public float moveMinY = 0.5f;
    public float moveMaxY = 0.5f;

    // --- 重要：ここを調整 ---
    public float moveWeight = 8.0f;

    // 最高速度を思い切って上げます。
    public float moveMaxSpeed = 12.0f;
    private float angle = 0;
    private bool isMoving = false;
    private Coroutine fireRoutine;
    private Coroutine mainAttackRoutine;
    private Coroutine moveRoutine;
    protected override void Awake()
    {
        base.Awake(); //
    }
    void OnEnable()
    {
        // 以前のルーチンが残っていれば停止
        if (mainAttackRoutine != null) StopCoroutine(mainAttackRoutine);
        if (moveRoutine != null) StopCoroutine(moveRoutine);

        // 攻撃と移動、それぞれ独立したコルーチンとして開始する
        mainAttackRoutine = StartCoroutine(AttackRoutine()); // ★StartCoroutineを使用
        moveRoutine = StartCoroutine(ContinuousMoveRoutine());
    }
    /*
    IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            var list = new List<EnemyBullet.BulletTransformData>();

            float wigglePower = 2.5f;
            float interval = 0.6f;

            // ★修正：angle = -999f を指定して、角度の上書きを禁止する
            list.Add(new EnemyBullet.BulletTransformData
            {
                triggerTime = interval * 1,
                angVel = -wigglePower,
            });

            list.Add(new EnemyBullet.BulletTransformData
            {
                triggerTime = interval * 2,
                angVel = wigglePower,
            });

            list.Add(new EnemyBullet.BulletTransformData
            {
                triggerTime = interval * 3,
                angVel = -wigglePower,
            });
            list.Add(new EnemyBullet.BulletTransformData
            {
                triggerTime = interval * 4,
                angVel = wigglePower,
            });

            list.Add(new EnemyBullet.BulletTransformData
            {
                triggerTime = interval * 5,
                angVel = -wigglePower,
            });
            list.Add(new EnemyBullet.BulletTransformData
            {
                triggerTime = interval * 6,
                angle = -999f,    // 角度は変えない
                angVel =0,
            });

            float startAngle = 0;
            for (int i = 0; i < 36; i++)
            {
                CreateMultiTransformLaser(
                    transform.position.x, transform.position.y,
                    1.0f, 15, RED, 3f, startAngle, 10, list,
                    0f, wigglePower, 6.0f, 3
                );
                startAngle += 360f / 36;
            }

            yield return new WaitForSeconds(4.0f);
        }
    }
    */
    IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            if (PlayerMove.Instance == null) yield break;

            float startAngle = 0f;
            int laserCount = 18;
            float rotSpeed = 0.5f; // 1フレームあたりの回転度数

            for (int i = 0; i < laserCount; i++)
            {
                // --- 1. 生成 (CreateLaserB: ボス追従型) ---
                // IDは被らないように i を使用。長さ 20、幅 1.2、赤色、予告 60F
                CreateLaserB(i, 120.0f, 1.2f, BulletManager.LaserColor.RED, 60);

                // --- 2. 動きの設定 (SetLaserDataB) ---
                // 第2引数 0 (初期設定): ボスからの距離 0、初期角度 startAngle、回転速度 rotSpeed を設定
                // 引数順: id, frame, lengthVel, dist, distVel, dAngle, dAngleVel, lAngle, lAngleVel
                SetLaserDataB(i, 0, 0f, 0f, 0f, 0f, 0f, startAngle, rotSpeed);

                // --- 3. 消滅の設定 ---
                // 180フレーム後（実体化から約2秒後）に、細くなって消滅を開始
                // 第10引数 (startClosing) を true にします
                SetLaserDataB(i, 180, 0f, -999f, -999f, -999f, -999f, -999f, -999f, true);

                // --- 4. 発射実行 ---
                FireShot(i);

                startAngle += 360f / laserCount;
            }

            // 次の波形までの待機（レーザーが消えるのを待つ）
            yield return new WaitForSeconds(5.0f);
        }
    }
    // 移動専用のループ
    IEnumerator ContinuousMoveRoutine()
    {
        while (true)
        {
            yield return StartCoroutine(SetMovePositionRand03(
                moveMinX, moveMaxX,
                moveMinY, moveMaxY,
                moveWeight
            ));

            // 移動し終わったら少し待機して次の移動へ
            yield return new WaitForSeconds(fireInterval);
        }
    }
    void FireRound()
    {
        if (PlayerMove.Instance == null) return; //

        SEManager.Instance.Play(SEPath.SHOT1, 0.5f);
        //CreateRoundShot(bulletData, transform.position,5, bulletSpeed, angle ,10);

        CreateReflectShot(RED[0], transform.position, bulletSpeed, angle, 2, true, true, true, true, 10 ,-1f, BLUE[0]);

        angle += 11;
    }
    
    // StreamLaserPattern.cs
  
    IEnumerator MoveRoutine()
    {
        isMoving = true;

        // 第6引数（maxSpeed）は不要になったので、引数を整理して呼び出し
        yield return StartCoroutine(SetMovePositionRand03(
            moveMinX, moveMaxX,
            moveMinY, moveMaxY,
            moveWeight
        ));

        isMoving = false;
    }
}