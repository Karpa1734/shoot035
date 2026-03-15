using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    public static PlayerStatus Instance { get; private set; }

    [Header("Timers")]
    public float invincibleTimer = 0f;    // 無敵残り時間
    public float deathBombTimer = 0f;     // 食らいボム受付残り時間

    public bool IsInvincible => invincibleTimer > 0;
    public bool IsDeathBombWindow => deathBombTimer > 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (invincibleTimer > 0) invincibleTimer -= Time.deltaTime;
        if (deathBombTimer > 0) deathBombTimer -= Time.deltaTime;
    }

    // 無敵時間を設定（以前の PlayerMove.SetInvincible 相当）
    public void SetInvincible(float duration)
    {
        invincibleTimer = duration;
        deathBombTimer = 0; // ボムが成功したので被弾猶予を消す
    }

    // 被弾猶予を開始（食らいボム受付）
    public void StartDeathBombWindow(float duration)
    {
        if (!IsInvincible)
        {
            deathBombTimer = duration;
        }
    }
}