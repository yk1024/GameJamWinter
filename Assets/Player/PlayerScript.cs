using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
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

    [Header("Thorn Ground Tag")]
    [SerializeField] private string thornGroundTag = "ThornGround";

    [Header("Landing Sprite")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite landingSprite;
    [SerializeField] private float landingSpriteDuration = 0.08f;

    [Header("Fast Fall")]
    [SerializeField] private Sprite fastFallSprite;
    [SerializeField] private KeyCode fastFallKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode fastFallAltKey = KeyCode.S;
    [SerializeField] private float fastFallVelocity = -18f;
    [Tooltip("trueなら上昇中でも急降下を開始/適用できます")]
    [SerializeField] private bool allowFastFallInUpward = true;

    [Header("Afterimage (Fast Fall Only)")]
    [SerializeField] private Afterimage2D afterimagePrefab;
    [SerializeField] private float afterimageInterval = 0.05f;
    [SerializeField] private float afterimageMinSpeed = 0.1f;
    private float afterimageTimer;

    [Header("Flutter Jump (押しっぱで落下開始時に発動 / スタミナで終了)")]
    [SerializeField] private KeyCode braceKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode braceAltKey = KeyCode.W;
    [Header("Flutterの効果中、落下速度がこれより速くならない")]
    [SerializeField] private float braceFallVelocityLimit = -5f;
    [Header("Flutterのスタミナ（秒）。押しっぱで落下に転じたら発動し、この時間だけ継続")]
    [SerializeField] private float braceHoldTime = 0.35f;
    [Header("発動瞬間に一度だけ上向きに押し上げる速度（小さめ: 1〜4程度）")]
    [SerializeField] private float braceUpVelocity = 3.5f;

    [Header("Sound Effects (OneShot)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip jumpSfx;
    [SerializeField] private AudioClip braceStartSfx;
    [SerializeField] private AudioClip braceBoostSfx; // exhausted/finish用として流用してもOK
    [SerializeField] private float sfxVolume = 1f;

    [Header("Sound Effects (FastFall Loop)")]
    [SerializeField] private AudioSource fastFallLoopSource;
    [SerializeField] private AudioClip fastFallLoopClip;
    [SerializeField] private float fastFallLoopVolume = 1f;

    [Header("Facing / FlipX")]
    [SerializeField] private bool defaultFacingRight = true;
    [SerializeField] private bool keepFacingWhenNoInput = true;

    [Header("Wrap (Horizontal Only)")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float wrapMargin = 0.2f;

    [Header("GroundCheck (BoxCast)")]
    [Tooltip("通常は Everything のままでOK。必要なら Ground/Spring/Thorn のレイヤーだけに絞ってください。")]
    [SerializeField] private LayerMask groundCheckMask = ~0;
    [Tooltip("足元に出すBoxの横幅（Collider幅に対する倍率）")]
    [SerializeField, Range(0.2f, 1.2f)] private float groundCheckWidthScale = 0.92f;
    [Tooltip("足元に出すBoxの高さ（小さめ推奨）")]
    [SerializeField] private float groundCheckHeight = 0.08f;
    [Tooltip("BoxCastの距離（小さめ推奨）")]
    [SerializeField] private float groundCheckDistance = 0.08f;
    [Tooltip("足元Boxの中心オフセット")]
    [SerializeField] private Vector2 groundCheckOffset = Vector2.zero;
    [Tooltip("床と見なす法線の最小Y。0.5=かなり厳しい、0.2~0.35あたりが無難")]
    [SerializeField, Range(0f, 1f)] private float groundNormalMinY = 0.25f;
    [Tooltip("y速度がこの値以下のときだけ「接地中」と扱う（上昇中に接地判定が残っても空中扱いにする）")]
    [SerializeField] private float groundedMaxUpwardVelocity = 0.02f;

    [Header("Tap Move")]
    [SerializeField] private bool enableTapMove = true;
    [SerializeField] private bool splitScreenLeftRight = true;
    [SerializeField, Range(0f, 1f)] private float tapMoveStrength = 1f;
    [SerializeField] private bool combineKeyboardAndTap = true;

    [Header("Thorn Hit (Frames / SFX)")]
    [Tooltip("Sprite Editorでスライスしたコマを左→右順に入れてください")]
    [SerializeField] private Sprite[] thornHitFrames;
    [Tooltip("1コマの秒数")]
    [SerializeField] private float thornHitFrameTime = 0.06f;
    [Tooltip("最後のコマを保持するならON（OFFなら通常スプライトへ戻る）")]
    [SerializeField] private bool holdLastThornFrame = true;
    [Tooltip("トゲに触れた時の効果音（OneShot）")]
    [SerializeField] private AudioClip thornHitSfx;
    [Tooltip("アニメ後、Resultへ遷移するまでの追加待ち時間（0で即遷移）")]
    [SerializeField] private float thornHitExtraDelay = 0f;

    private Rigidbody2D rb;
    private Collider2D myCollider;

    // 状態
    private bool isFastFalling;

    // Flutter Jump (ヨッシー風)
    private bool isFluttering;
    private bool flutterUsedInAir;
    private float flutterTimeLeft;

    private bool isLandingSpritePlaying;

    // FixedUpdateで物理を書き換えるため、Updateで入力をキャッシュする
    private float cachedMoveInputX;
    private bool braceHeldCached;
    private bool fastFallPressedQueued;

    // Ground state (BoxCast)
    private bool wasGrounded;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];

    // Thorn / GameOver
    private bool isDead;
    private Coroutine thornRoutine;

    private enum GroundKind { None, Ground, Spring, Thorn }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        myCollider = GetComponent<Collider2D>();

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
        if (isDead) return;

        float keyboardX = Input.GetAxisRaw("Horizontal");
        float tapX = enableTapMove ? GetTapHorizontalInput() : 0f;

        if (combineKeyboardAndTap)
            cachedMoveInputX = Mathf.Clamp(keyboardX + tapX * tapMoveStrength, -1f, 1f);
        else
            cachedMoveInputX = Mathf.Abs(tapX) > 0.01f ? tapX * tapMoveStrength : keyboardX;

        UpdateFacing(cachedMoveInputX);

        if (Input.GetKeyDown(fastFallKey) || Input.GetKeyDown(fastFallAltKey))
            fastFallPressedQueued = true;

        braceHeldCached = Input.GetKey(braceKey) || Input.GetKey(braceAltKey);

        UpdateSpriteByState();
        UpdateAfterimage();
    }

    private float GetTapHorizontalInput()
    {
        bool pressed = Input.GetMouseButton(0);
        if (!pressed) return 0f;

        float x = Input.mousePosition.x;
        float mid = Screen.width * 0.5f;

        if (!splitScreenLeftRight) { /* 今は左右分割のみ */ }

        return (x < mid) ? -1f : 1f;
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        if (rb == null) return;

        Vector2 v = rb.linearVelocity;

        GroundKind groundKind;
        bool groundedNow = CheckGround(out groundKind, out RaycastHit2D hit);
        bool consideredGrounded = groundedNow && v.y <= groundedMaxUpwardVelocity;

        if (!wasGrounded && consideredGrounded)
        {
            bool landedFromAbove = hit.collider != null && hit.normal.y >= groundNormalMinY;

            if (groundKind == GroundKind.Thorn && landedFromAbove)
            {
                TriggerGameOverByThorn();
                return;
            }

            if (groundKind == GroundKind.Ground || groundKind == GroundKind.Spring)
            {
                OnLanded(groundKind, ref v);
                consideredGrounded = false;
                groundedNow = false;
            }
        }

        if (consideredGrounded)
            SetFastFall(false);

        if (fastFallPressedQueued)
        {
            fastFallPressedQueued = false;

            if (!consideredGrounded)
            {
                CancelFlutter(consume: true);
                SetFastFall(true);
            }
        }

        v.x = cachedMoveInputX * moveSpeed;

        TryStartFlutter(ref v, consideredGrounded);
        ApplyFlutter(ref v);

        if (isFastFalling && !consideredGrounded)
        {
            if (allowFastFallInUpward || v.y <= 0f)
                v.y = Mathf.Min(v.y, fastFallVelocity);
        }

        rb.linearVelocity = v;

        wasGrounded = consideredGrounded;
    }

    private void LateUpdate()
    {
        if (isDead) return;
        DoHorizontalWrap();
    }

    // ======== GroundCheck (BoxCast) ========
    private bool CheckGround(out GroundKind kind, out RaycastHit2D bestHit)
    {
        kind = GroundKind.None;
        bestHit = default;

        if (myCollider == null) return false;

        Bounds b = myCollider.bounds;

        Vector2 boxSize = new Vector2(b.size.x * groundCheckWidthScale, groundCheckHeight);
        Vector2 origin = new Vector2(b.center.x, b.min.y + boxSize.y * 0.5f) + groundCheckOffset;

        int count = Physics2D.BoxCastNonAlloc(
            origin,
            boxSize,
            0f,
            Vector2.down,
            groundHits,
            groundCheckDistance,
            groundCheckMask
        );

        if (count <= 0) return false;

        bool found = false;
        float bestNormalY = -999f;
        float bestDist = 999f;

        for (int i = 0; i < count; i++)
        {
            var h = groundHits[i];
            if (h.collider == null) continue;
            if (h.collider.isTrigger) continue;

            GroundKind k = GroundKind.None;
            if (h.collider.CompareTag(thornGroundTag)) k = GroundKind.Thorn;
            else if (h.collider.CompareTag(springGroundTag)) k = GroundKind.Spring;
            else if (h.collider.CompareTag(groundTag)) k = GroundKind.Ground;
            else continue;

            if (h.normal.y < groundNormalMinY) continue;

            if (!found || h.normal.y > bestNormalY || (Mathf.Approximately(h.normal.y, bestNormalY) && h.distance < bestDist))
            {
                found = true;
                bestNormalY = h.normal.y;
                bestDist = h.distance;
                bestHit = h;
                kind = k;
            }
        }

        return found;
    }

    // ======== Landing / AutoJump ========
    private void OnLanded(GroundKind kind, ref Vector2 v)
    {
        SetFastFall(false);

        isFluttering = false;
        flutterUsedInAir = false;
        flutterTimeLeft = 0f;

        if (landingSprite != null && spriteRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(PlayLandingSprite());
        }

        float jumpV = (kind == GroundKind.Spring) ? springJumpVelocity : jumpVelocity;
        v.y = jumpV;

        PlaySfx(jumpSfx);
    }

    // ======== Flutter ========
    private void TryStartFlutter(ref Vector2 v, bool grounded)
    {
        if (grounded) return;
        if (isFastFalling) return;
        if (isFluttering) return;
        if (flutterUsedInAir) return;

        if (!braceHeldCached) return;
        if (v.y > 0f) return;

        isFluttering = true;
        flutterTimeLeft = braceHoldTime;

        v.y = Mathf.Max(v.y, braceUpVelocity);

        PlaySfx(braceStartSfx);
    }

    private void ApplyFlutter(ref Vector2 v)
    {
        if (!isFluttering) return;

        if (wasGrounded)
        {
            CancelFlutter(consume: false);
            return;
        }

        if (v.y < braceFallVelocityLimit)
            v.y = braceFallVelocityLimit;

        flutterTimeLeft -= Time.fixedDeltaTime;

        if (flutterTimeLeft <= 0f)
        {
            isFluttering = false;
            flutterTimeLeft = 0f;
            flutterUsedInAir = true;

            PlaySfx(braceBoostSfx);
        }
    }

    private void CancelFlutter(bool consume)
    {
        if (!isFluttering && !consume) return;

        isFluttering = false;
        flutterTimeLeft = 0f;

        if (consume)
            flutterUsedInAir = true;
    }

    // ======== Visual ========
    private void UpdateFacing(float xInput)
    {
        if (spriteRenderer == null) return;

        if (keepFacingWhenNoInput && Mathf.Approximately(xInput, 0f))
            return;

        bool wantFaceRight = xInput > 0f;
        if (xInput < 0f) wantFaceRight = false;

        bool flip = defaultFacingRight ? !wantFaceRight : wantFaceRight;
        spriteRenderer.flipX = flip;
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

    private IEnumerator PlayLandingSprite()
    {
        if (spriteRenderer == null || landingSprite == null) yield break;

        isLandingSpritePlaying = true;
        spriteRenderer.sprite = landingSprite;
        yield return new WaitForSeconds(landingSpriteDuration);
        isLandingSpritePlaying = false;

        if (normalSprite != null)
            spriteRenderer.sprite = normalSprite;
    }

    private void UpdateAfterimage()
    {
        if (!isFastFalling) return;
        if (afterimagePrefab == null) return;
        if (spriteRenderer == null) return;

        if (rb.linearVelocity.sqrMagnitude < afterimageMinSpeed * afterimageMinSpeed)
            return;

        afterimageTimer += Time.deltaTime;
        if (afterimageTimer < afterimageInterval) return;
        afterimageTimer = 0f;

        var g = Instantiate(afterimagePrefab, spriteRenderer.transform.position, spriteRenderer.transform.rotation);
        g.InitFrom(spriteRenderer);
    }

    // ======== Wrap ========
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

    // ======== Thorn Safety Net（BoxCast漏れ対策） ========
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (!collision.collider.CompareTag(thornGroundTag)) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            var c = collision.GetContact(i);
            if (c.normal.y >= groundNormalMinY)
            {
                TriggerGameOverByThorn();
                return;
            }
        }
    }

    // ======== Audio / FastFall State ========
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

    // ======== Thorn Death (Frames) ========
    private void TriggerGameOverByThorn()
    {
        if (isDead) return;
        isDead = true;
        PlaySfx(thornHitSfx);
        StopPlayerMovement(); 

        if (thornRoutine != null) StopCoroutine(thornRoutine);
        thornRoutine = StartCoroutine(ThornDeathSequence());
    }

    private IEnumerator ThornDeathSequence()
    {
        if (spriteRenderer != null && thornHitFrames != null && thornHitFrames.Length > 0)
        {
            for (int i = 0; i < thornHitFrames.Length; i++)
            {
                var s = thornHitFrames[i];
                if (s != null) spriteRenderer.sprite = s;
                yield return new WaitForSeconds(thornHitFrameTime);
            }

            if (!holdLastThornFrame && normalSprite != null)
                spriteRenderer.sprite = normalSprite;
        }

        if (thornHitExtraDelay > 0f)
            yield return new WaitForSeconds(thornHitExtraDelay);

        var cam = FindAnyObjectByType<CameraFollowScore2D>();
        if (cam != null)
        {
            cam.GameOverNow();
        }
        else
        {
            Debug.LogWarning("CameraFollowScore2D が見つからないため、ThornGroundのGameOverを実行できません。");
        }
    }

    public void StopPlayerMovement()
    {
        if (isDead) return;
        isDead = true;

        // 入力・処理停止
        fastFallPressedQueued = false;
        braceHeldCached = false;
        cachedMoveInputX = 0f;

        // 効果解除＆SE停止
        SetFastFall(false);
        StopFastFallLoop();

        // コルーチン停止（着地アニメ等）
        StopAllCoroutines();

        // 速度停止 & 物理停止
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false; // Staticより安全
        }
    }
}