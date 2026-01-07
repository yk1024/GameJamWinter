using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAutoJump2D : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Auto Jump")]
    [SerializeField] private float jumpVelocity = 10f;
    [SerializeField] private float springJumpVelocity = 16f;

    [Header("Ground Tags")]
    [SerializeField] private string groundTag = "Ground";
    [SerializeField] private string springGroundTag = "SpringGround";
    [SerializeField] private string thornGroundTag = "ThornGround";

    [Header("Horizontal Wrap (Screen Edge)")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float wrapMargin = 0.2f;

    [Header("Sprites")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite landSprite;
    [SerializeField] private float landSpriteTime = 0.08f;
    [SerializeField] private Sprite fastFallSprite;

    [Header("Facing")]
    [Tooltip("デフォルトが右向きならONのままでOK。デフォルトが左向きの絵ならOFFにしてください。")]
    [SerializeField] private bool defaultFacingRight = true;
    [Tooltip("入力が0の時も最後の向きを維持する")]
    [SerializeField] private bool keepFacingWhenNoInput = true;

    [Header("Fast Fall")]
    [SerializeField] private KeyCode fastFallKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode fastFallAltKey = KeyCode.S;
    [SerializeField] private float fastFallVelocity = -18f;
    [SerializeField] private bool allowFastFallInUpward = false;

    [Header("Afterimage (Fast Fall Only)")]
    [SerializeField] private Afterimage2D afterimagePrefab;
    [SerializeField] private float afterimageInterval = 0.05f;
    [SerializeField] private float afterimageMinSpeed = 0.1f;
    private float afterimageTimer;

    [Header("Brace (踏ん張り - Hold)")]
    [SerializeField] private KeyCode braceKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode braceAltKey = KeyCode.W;
    [SerializeField] private float braceFallVelocityLimit = -3f;
    [SerializeField] private float braceHoldTime = 0.35f;
    [SerializeField] private float braceUpVelocity = 2.5f;

    [Header("Sound Effects (OneShot)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip jumpSfx;
    [SerializeField] private AudioClip braceStartSfx;
    [SerializeField] private AudioClip braceBoostSfx;
    [SerializeField] private float sfxVolume = 1f;

    [Header("Sound Effects (FastFall Loop)")]
    [SerializeField] private AudioSource fastFallLoopSource;
    [SerializeField] private AudioClip fastFallLoopClip;
    [SerializeField] private float fastFallLoopVolume = 1f;

    private Rigidbody2D rb;

    private Coroutine landRoutine;
    private bool isLandingSpritePlaying;

    private bool isFastFalling;

    private bool braceUsedInAir;
    private bool isBracing;
    private float braceHoldTimer;
    private bool braceBoosted;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        if (targetCamera == null) targetCamera = Camera.main;

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null && normalSprite == null)
            normalSprite = spriteRenderer.sprite;

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        }
        sfxSource.playOnAwake = false;

        if (fastFallLoopSource == null)
        {
            fastFallLoopSource = gameObject.AddComponent<AudioSource>();
        }
        fastFallLoopSource.playOnAwake = false;
        fastFallLoopSource.loop = true;
    }

    private void Update()
    {
        // 左右移動
        float x = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(x * moveSpeed, rb.linearVelocity.y);

        // 追加：向き更新
        UpdateFacing(x);

        HandleFastFallInput();
        HandleBraceInputAndState();

        ApplyFastFallIfActive();
        ApplyBraceIfActive();

        UpdateSpriteByState();
        UpdateAfterimage();
    }

    private void LateUpdate()
    {
        DoHorizontalWrap();
    }

    // 追加
    private void UpdateFacing(float xInput)
    {
        if (spriteRenderer == null) return;

        if (keepFacingWhenNoInput && Mathf.Approximately(xInput, 0f))
            return;

        // 右入力なら「右向き」、左入力なら「左向き」にしたい
        bool wantFaceRight = xInput > 0f;
        if (xInput < 0f) wantFaceRight = false;

        // デフォルトが右向きの絵：右向き=flipX:false、左向き=flipX:true
        // デフォルトが左向きの絵：逆になるので defaultFacingRight で補正
        bool flip = defaultFacingRight ? !wantFaceRight : wantFaceRight;
        spriteRenderer.flipX = flip;
    }

    private void HandleFastFallInput()
    {
        bool pressedThisFrame =
            Input.GetKeyDown(fastFallKey) ||
            Input.GetKeyDown(fastFallAltKey);

        if (!pressedThisFrame) return;

        StopBrace(consume: true);

        isFastFalling = true;
        PlayFastFallLoop();
    }

    private void HandleBraceInputAndState()
    {
        bool down = Input.GetKeyDown(braceKey) || Input.GetKeyDown(braceAltKey);
        bool held = Input.GetKey(braceKey) || Input.GetKey(braceAltKey);
        bool up = Input.GetKeyUp(braceKey) || Input.GetKeyUp(braceAltKey);

        if (down)
        {
            if (!braceUsedInAir && !braceBoosted)
            {
                SetFastFall(false);

                StartBrace();
                PlaySfx(braceStartSfx);
            }
        }

        if (up && isBracing)
        {
            StopBrace(consume: true);
        }

        if (isBracing)
        {
            if (held)
            {
                braceHoldTimer += Time.deltaTime;

                if (!braceBoosted && braceHoldTimer >= braceHoldTime)
                {
                    braceBoosted = true;

                    float newY = Mathf.Max(rb.linearVelocity.y, braceUpVelocity);
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, newY);

                    PlaySfx(braceBoostSfx);
                    StopBrace(consume: true);
                }
            }
            else
            {
                StopBrace(consume: true);
            }
        }
    }

    private void StartBrace()
    {
        isBracing = true;
        braceHoldTimer = 0f;
    }

    private void StopBrace(bool consume)
    {
        if (!isBracing && !consume) return;

        isBracing = false;
        braceHoldTimer = 0f;

        if (consume) braceUsedInAir = true;
    }

    private void ApplyFastFallIfActive()
    {
        if (!isFastFalling) return;

        float newY = Mathf.Min(rb.linearVelocity.y, fastFallVelocity);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, newY);
    }

    private void ApplyBraceIfActive()
    {
        if (!isBracing) return;

        if (rb.linearVelocity.y < braceFallVelocityLimit)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, braceFallVelocityLimit);
        }
    }

    private void UpdateSpriteByState()
    {
        if (spriteRenderer == null) return;

        if (isFastFalling && fastFallSprite != null)
        {
            if (spriteRenderer.sprite != fastFallSprite)
                spriteRenderer.sprite = fastFallSprite;
            return;
        }

        if (isLandingSpritePlaying) return;

        if (normalSprite != null && spriteRenderer.sprite != normalSprite)
            spriteRenderer.sprite = normalSprite;
    }

    private void DoHorizontalWrap()
    {
        if (targetCamera == null) return;

        float halfHeight = targetCamera.orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;

        float camX = targetCamera.transform.position.x;
        float left = camX - halfWidth;
        float right = camX + halfWidth;

        Vector2 pos = rb.position;

        if (pos.x > right + wrapMargin) pos.x = left - wrapMargin;
        else if (pos.x < left - wrapMargin) pos.x = right + wrapMargin;

        rb.position = pos;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // まず「上から着地」かどうか
        if (!IsLandingFromAbove(collision, out bool isSpring)) return;

        // 追加：トゲ足場なら即GameOver
        if (collision.collider.CompareTag(thornGroundTag))
        {
            TriggerGameOverByThorn();
            return;
        }

        // ここからは通常の地面処理（既存のまま）
        SetFastFall(false);

        // 着地で踏ん張りリセット（あなたの既存処理）
        isBracing = false;
        braceHoldTimer = 0f;
        braceUsedInAir = false;
        braceBoosted = false;

        PlayLandingSprite();

        float v = isSpring ? springJumpVelocity : jumpVelocity;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, v);

        PlaySfx(jumpSfx);
    }

    private bool IsLandingFromAbove(Collision2D collision, out bool isSpring)
    {
        isSpring = false;

        var col = collision.collider;
        bool isGround = col.CompareTag(groundTag);
        isSpring = col.CompareTag(springGroundTag);

        if (!isGround && !isSpring) return false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            var c = collision.GetContact(i);
            if (c.normal.y > 0.5f) return true;
        }
        return false;
    }

    private void PlayLandingSprite()
    {
        if (spriteRenderer == null) return;
        if (landSprite == null) return;

        if (landRoutine != null) StopCoroutine(landRoutine);
        landRoutine = StartCoroutine(LandingSpriteRoutine());
    }

    private IEnumerator LandingSpriteRoutine()
    {
        isLandingSpritePlaying = true;

        if (!isFastFalling)
            spriteRenderer.sprite = landSprite;

        yield return new WaitForSeconds(landSpriteTime);

        isLandingSpritePlaying = false;
        UpdateSpriteByState();

        landRoutine = null;
    }

    private void UpdateAfterimage()
    {
        if (afterimagePrefab == null) return;
        if (spriteRenderer == null) return;

        if (!isFastFalling)
        {
            afterimageTimer = 0f;
            return;
        }

        if (rb.linearVelocity.sqrMagnitude < afterimageMinSpeed * afterimageMinSpeed)
            return;

        afterimageTimer += Time.deltaTime;
        if (afterimageTimer < afterimageInterval) return;
        afterimageTimer = 0f;

        var g = Instantiate(afterimagePrefab, spriteRenderer.transform.position, spriteRenderer.transform.rotation);
        g.InitFrom(spriteRenderer);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        if (sfxSource == null) return;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    private void SetFastFall(bool active)
    {
        if (active)
        {
            if (!isFastFalling)
            {
                isFastFalling = true;
                PlayFastFallLoop();
            }
        }
        else
        {
            if (isFastFalling)
            {
                isFastFalling = false;
                StopFastFallLoop();
            }
        }
    }

    private void PlayFastFallLoop()
    {
        if (fastFallLoopSource == null) return;
        if (fastFallLoopClip == null) return;

        if (fastFallLoopSource.clip != fastFallLoopClip)
            fastFallLoopSource.clip = fastFallLoopClip;

        fastFallLoopSource.volume = fastFallLoopVolume;

        if (!fastFallLoopSource.isPlaying)
            fastFallLoopSource.Play();
    }

    private void StopFastFallLoop()
    {
        if (fastFallLoopSource == null) return;
        if (fastFallLoopSource.isPlaying)
            fastFallLoopSource.Stop();
    }

    

    private void TriggerGameOverByThorn()
    {
        // プレイヤーを止める（見た目上「刺さった」感じに）
        SetFastFall(false);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }

        // Camera側のGameOverに任せる（フェード→ResultScene）
        var cam = FindAnyObjectByType<CameraFollowScore2D>();
        if (cam != null)
        {
            cam.GameOverNow();
        }
        else
        {
            // カメラが見つからない保険（最悪でも止まるだけになる）
            Debug.LogWarning("CameraFollowScore2D が見つからないため、ThornGroundのGameOverを実行できません。");
        }
    }
}