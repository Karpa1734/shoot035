using KanKikuchi.AudioManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Research02 : BossPatternBase
{
    [Header("Polygon Settings")]
    public int edges = 3;
    public float baseSpeed = 3.0f;
    public int bulletCount = 30;
    public float fireInterval = 1.5f;
    float angle = 0;


    private Coroutine mainAttackRoutine;
    private Coroutine moveRoutine;

    private bool isMoving = false;
    void OnEnable()
    {
        // 既存のルーチンがあれば停止して重複を防ぐ
        if (mainAttackRoutine != null) StopCoroutine(mainAttackRoutine);
        if (moveRoutine != null) StopCoroutine(moveRoutine);

        // 攻撃と移動、それぞれ独立したコルーチンとして開始
        mainAttackRoutine = StartCoroutine(BitAttackRoutine());
        moveRoutine = StartCoroutine(MoveRoutine());
    }

    IEnumerator BitAttackRoutine()
    {
        // ビットを使用する場合はここで生成（現在はコメントアウトの状態を維持）
        // CreateOrbitBits(5, 3.0f, 2.0f, 40.0f, bitPrefab);
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            // 自機への角度計算
            //Vector2 dir = PlayerMove.Instance.transform.position - transform.position;
            //float angleToPlayer = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            // 1回目の多角形発射
            SEManager.Instance.Play(SEPath.SHOT1, 0.5f);
            // transform.position を渡すことで、移動中の現在地から発射される
            CreateWideShot(RED[0], transform.position, baseSpeed, angle,30, bulletCount, 5);

            yield return new WaitForSeconds(fireInterval);


            angle += 31;
        }
    }
    // --- 修正版：移動専用のループルーチン ---
    IEnumerator MoveRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        // 攻撃が続いている間、無限に移動を繰り返す
        while (true)
        {
            isMoving = true;

            // ランダムな座標へ移動開始
            yield return StartCoroutine(SetMovePositionRand03(
                moveMinX, moveMaxX,
                moveMinY, moveMaxY,
                moveWeight
            ));

            isMoving = false;

            // 移動完了後に少し休憩（これがないと休みなく動き続けます）
            yield return new WaitForSeconds(moveInterval);
        }
    }

    // 段階移行時などのクリーンアップ
    protected override void OnDisable()
    {
        base.OnDisable(); // ビットの破棄などを実行
        if (mainAttackRoutine != null) StopCoroutine(mainAttackRoutine);
        if (moveRoutine != null) StopCoroutine(moveRoutine);
    }
}