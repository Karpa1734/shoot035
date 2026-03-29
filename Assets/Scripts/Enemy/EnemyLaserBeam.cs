using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyLaserBeam : MonoBehaviour
{
    private const int ANIM_FRAMES = 10;

    public enum LaserType { A_Stationary, B_FollowBoss }
    private LaserType type;

    private SpriteRenderer sr;
    private BoxCollider2D col;
    private BulletManager.LaserSet visualSet;
    private Transform bossTransform;

    private float targetWidth, currentLength;
    private int delayFrames, elapsedFrames, closingFrames;
    private bool isFired = false;
    private bool isClosing = false;

    private float lengthVel, angle, angVel, moveSpeed, moveAngle;
    private float dist, distVel, distAngle, distAngleVel, laserAngle, laserAngleVel;

    private List<LaserTransformData> transformQueue = new List<LaserTransformData>();

    [System.Serializable]
    public class LaserTransformData
    {
        public int frame;
        public float angle = -999f, angVel = -999f, lengthVel = -999f;
        public float moveSpeed = -999f, moveAngle = -999f;
        public float dist = -999f, distVel = -999f, distAngle = -999f, distAngleVel = -999f, laserAngle = -999f, laserAngleVel = -999f;
        public bool startClosing = false;
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
        sr.drawMode = SpriteDrawMode.Simple;

        // ★対策1：最初は描画をオフにしておく
        sr.enabled = false;
    }

    public void SetupA(float x, float y, float length, float width, BulletManager.LaserColor color, int delay)
    {
        type = LaserType.A_Stationary;
        transform.position = new Vector3(x, y, 0);
        InitializeBase(length, width, color, delay);
    }

    public void SetupB(float length, float width, BulletManager.LaserColor color, int delay, Transform boss)
    {
        type = LaserType.B_FollowBoss;
        bossTransform = boss;
        // ★対策2：Type Bの場合、生成された瞬間にボスの位置へ移動させる
        if (bossTransform != null) transform.position = bossTransform.position;
        InitializeBase(length, width, color, delay);
    }

    private void InitializeBase(float length, float width, BulletManager.LaserColor color, int delay)
    {
        visualSet = BulletManager.Instance.GetLaserSet(color);
        this.currentLength = length;
        this.targetWidth = width;
        this.delayFrames = delay;
        this.elapsedFrames = 0;
        this.closingFrames = 0;
        this.isClosing = false;

        this.sr.sprite = visualSet.mainSprite;
        this.sr.material = BulletManager.Instance.additiveMaterial;
        this.sr.color = new Color(1, 1, 1, 0.4f);
        this.col.enabled = false;

        UpdateVisuals(targetWidth * 0.5f);
    }

    public void AddData(LaserTransformData d)
    {
        transformQueue.Add(d);
        transformQueue.Sort((a, b) => a.frame.CompareTo(b.frame));

        // 0フレーム目のデータなら、生成直後にその場で反映させる
        if (d.frame == 0)
        {
            ApplyTransform(d);
            // ★対策3：0フレーム目の角度や距離を即座に計算し、座標を確定させる
            if (type == LaserType.A_Stationary) UpdateA();
            else UpdateB();
        }
    }

    public void Fire()
    {
        isFired = true;
        // ★対策1：Fireが呼ばれた（＝パラメータ設定が全て終わった）ので描画を開始する
        sr.enabled = true;
    }

    public void ForceClose()
    {
        if (isClosing) return;
        isClosing = true;
        col.enabled = false;
        lengthVel = 0;
    }

    void FixedUpdate()
    {
        if (!isFired) return;

        if (transformQueue.Count > 0 && elapsedFrames >= transformQueue[0].frame)
        {
            ApplyTransform(transformQueue[0]);
            transformQueue.RemoveAt(0);
        }

        if (elapsedFrames == delayFrames && !isClosing)
        {
            sr.color = Color.white;
            col.enabled = true;
        }

        float widthToSet = 0;
        if (elapsedFrames < delayFrames) widthToSet = targetWidth * 0.5f;
        else if (elapsedFrames < delayFrames + ANIM_FRAMES)
        {
            float t = (float)(elapsedFrames - delayFrames) / ANIM_FRAMES;
            widthToSet = Mathf.Lerp(0, targetWidth, t);
        }
        else if (isClosing)
        {
            closingFrames++;
            float t = (float)closingFrames / ANIM_FRAMES;
            widthToSet = Mathf.Lerp(targetWidth, 0, t);
            if (closingFrames >= ANIM_FRAMES) Destroy(gameObject);
        }
        else widthToSet = targetWidth;

        if (type == LaserType.A_Stationary) UpdateA();
        else UpdateB();

        UpdateVisuals(widthToSet);
        elapsedFrames++;

        if (currentLength < 0) currentLength = 0;
    }

    private void UpdateA()
    {
        angle += angVel;
        if (!isClosing) currentLength += lengthVel;
        Vector3 move = new Vector3(Mathf.Cos(moveAngle * Mathf.Deg2Rad), Mathf.Sin(moveAngle * Mathf.Deg2Rad), 0) * moveSpeed;
        transform.position += move;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }

    private void UpdateB()
    {
        if (bossTransform == null) { Destroy(gameObject); return; }
        dist += distVel; distAngle += distAngleVel; laserAngle += laserAngleVel;
        if (!isClosing) currentLength += lengthVel;

        Vector3 offset = new Vector3(Mathf.Cos(distAngle * Mathf.Deg2Rad), Mathf.Sin(distAngle * Mathf.Deg2Rad), 0) * dist;
        transform.position = bossTransform.position + offset;
        transform.rotation = Quaternion.Euler(0, 0, laserAngle - 90f);
    }

    private void UpdateVisuals(float w)
    {
        transform.localScale = new Vector3(w, currentLength, 1f);
    }

    private void ApplyTransform(LaserTransformData t)
    {
        if (t.startClosing && !isClosing && elapsedFrames > delayFrames)
        {
            isClosing = true;
            col.enabled = false;
            lengthVel = 0;
        }

        if (t.lengthVel != -999f) lengthVel = t.lengthVel;
        if (type == LaserType.A_Stationary)
        {
            if (t.angle != -999f) angle = t.angle;
            if (t.angVel != -999f) angVel = t.angVel;
            if (t.moveSpeed != -999f) moveSpeed = t.moveSpeed;
            if (t.moveAngle != -999f) moveAngle = t.moveAngle;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f); // Aの回転更新
        }
        else
        {
            if (t.dist != -999f) dist = t.dist;
            if (t.distVel != -999f) distVel = t.distVel;
            if (t.distAngle != -999f) distAngle = t.distAngle;
            if (t.distAngleVel != -999f) distAngleVel = t.distAngleVel;
            if (t.laserAngle != -999f) laserAngle = t.laserAngle;
            if (t.laserAngleVel != -999f) laserAngleVel = t.laserAngleVel;
            transform.rotation = Quaternion.Euler(0, 0, laserAngle - 90f); // Bの回転更新
        }
    }
}