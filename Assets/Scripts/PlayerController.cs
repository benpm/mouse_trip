using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float maxSpeed = 10f;
    [SerializeField] float acceleration = 80f;
    [SerializeField] float deceleration = 100f;
    [SerializeField] float turnBoost = 1.6f;
    [SerializeField] float airAcceleration = 50f;

    [Header("Jump")]
    [SerializeField] float jumpForce = 18f;
    [SerializeField] float gravityScale = 5f;
    [SerializeField] float fallGravityMult = 1.6f;
    [SerializeField] float jumpCutMultiplier = 0.4f;
    [SerializeField] float coyoteTime = 0.12f;
    [SerializeField] float jumpBufferTime = 0.15f;

    [Header("Ground Check")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float groundCheckDistance = 0.1f;

    [Header("Gravity Direction")]
    [SerializeField] bool rotatePlayerToGravity = true;
    [Tooltip("0 = snap instantly, higher = smooth toward mouse direction")]
    [SerializeField] float gravityTurnSpeed = 0f;
    [SerializeField] bool useMouseGravityDir = false;

    [Header("Tile Damage")]
    [Tooltip("Cooldown/knockback/invincibility for damaging tiles; amount comes from GameplayTile.damage")]
    [SerializeField] DamageProfile tileDamageProfile;

    public Vector2 gravityDir = Vector2.down;

    Rigidbody2D rb;
    IDamageable damageable;
    Vector2 velocity;
    bool isGrounded;
    bool jumpHeld;
    bool jumpCutQueued;
    float coyoteTimer;
    float jumpBufferTimer;
    bool isTouchingWall;
    float wallContactTimer;
    bool warnedNoProfile;

    readonly ContactPoint2D[] contacts = new ContactPoint2D[8];
    int contactCount;

    // Gravity frame, similar to an ortho-normal basis.
    Vector2 Up => -gravityDir;
    Vector2 RightAxis => Vector2.Perpendicular(gravityDir); // (1,0) when gravity is down

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        damageable = GetComponent<IDamageable>();
    }

    void Update()
    {
        // Jump buffer - pressed just before landing
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }
        jumpHeld = Input.GetButton("Jump");
        if (Input.GetButtonUp("Jump")) jumpCutQueued = true;
        UpdateGravityDirection();
    }

    Vector2 GetMouseGravityDir()
    {
        Camera cam = Camera.main;
        if (cam == null) return gravityDir;

        Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 centerWorld = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));

        Vector2 dir = mouseWorld - centerWorld;
        if (dir.sqrMagnitude < 0.0001f) return gravityDir;

        return dir.normalized;
    }

    void UpdateGravityDirection()
    {
        Vector2 targetDir = gravityDir;
        if (useMouseGravityDir)
        {
            targetDir = GetMouseGravityDir();
        }

        if (gravityTurnSpeed <= 0f)
        {
            gravityDir = targetDir;
        }
        else
        {
            // Smoothly rotate current gravity toward the target direction
            float maxRadians = gravityTurnSpeed * Time.deltaTime;
            gravityDir = Vector3.RotateTowards(gravityDir, targetDir, maxRadians, 0f).normalized;
        }
    }

    void FixedUpdate()
    {
        velocity = rb.linearVelocity;
        contactCount = rb.GetContacts(contacts);

        // Decompose world velocity into the gravity frame:
        float vAlong = Vector2.Dot(velocity, RightAxis);
        float vUp = Vector2.Dot(velocity, Up);

        if (jumpCutQueued)
        {
            if (vUp > 0f) vUp *= jumpCutMultiplier;
            jumpCutQueued = false;
        }

        CheckContacts(ref vAlong, ref vUp);
        ApplyHorizontal(ref vAlong);
        CheckWall(ref vAlong);
        ApplyGravity(ref vUp);
        TryJump(ref vUp);

        // Recompose back into world space
        velocity = RightAxis * vAlong + Up * vUp;
        rb.linearVelocity = velocity;

        if (rotatePlayerToGravity)
        {
            float angle = Mathf.Atan2(Up.y, Up.x) * Mathf.Rad2Deg - 90f;
            rb.rotation = angle;
        }
    }

    // Ground detection and tile effects. Wall handling lives in CheckWall (must run after ApplyHorizontal).
    void CheckContacts(ref float vAlong, ref float vUp)
    {
        bool groundContact = false;
        bool damaged = false;

        for (int i = 0; i < contactCount; i++)
        {
            ContactPoint2D contact = contacts[i];
            float upDot = Vector2.Dot(contact.normal, Up);
            if (upDot > 0.5f) groundContact = true;

            if (!damaged && TryGetGameplayTile(contact, out GameplayTile tile) && tile.damagesPlayer)
            {
                damaged = ApplyTileDamage(contact, tile, ref vAlong, ref vUp);
            }
        }

        if (groundContact)
        {
            isGrounded = true;
            coyoteTimer = coyoteTime;
        }
        else
        {
            isGrounded = false;
            coyoteTimer -= Time.fixedDeltaTime;
        }
    }

    bool TryGetGameplayTile(ContactPoint2D contact, out GameplayTile tile)
    {
        tile = null;
        if (contact.collider == null || !contact.collider.TryGetComponent(out Tilemap tilemap)) return false;

        // Nudge the hit point slightly inside the tile using the hit normal
        Vector3 hitPosition = contact.point - contact.normal * 0.01f;
        tile = tilemap.GetTile(tilemap.WorldToCell(hitPosition)) as GameplayTile;
        return tile != null;
    }

    bool ApplyTileDamage(ContactPoint2D contact, GameplayTile tile, ref float vAlong, ref float vUp)
    {
        if (damageable == null) return false;
        if (tileDamageProfile == null)
        {
            if (!warnedNoProfile)
            {
                Debug.LogWarning($"{name}: tileDamageProfile not set, damaging tiles are ignored", this);
                warnedNoProfile = true;
            }
            return false;
        }

        // Knockback away from the tile surface, expressed in the gravity frame
        float sideDot = Vector2.Dot(contact.normal, RightAxis);
        Vector2 dir = new Vector2(Mathf.Abs(sideDot) > 0.01f ? Mathf.Sign(sideDot) : 1f, 1f);

        DamageInfo info = tileDamageProfile.CreateDamageInfo(contact.collider.gameObject, dir);
        info.amount = tile.damage;

        if (!damageable.TakeDamage(info)) return false;

        vAlong = info.knockback.x;
        vUp = info.knockback.y;
        return true;
    }

    void CheckWall(ref float vAlong)
    {
        bool wallLeft = false;
        bool wallRight = false;

        for (int i = 0; i < contactCount; i++)
        {
            Vector2 normal = contacts[i].normal;

            // Express the contact normal in the gravity frame
            float sideDot = Vector2.Dot(normal, RightAxis);
            float upDot = Vector2.Dot(normal, Up);

            if (Mathf.Abs(sideDot) > 0.9f && Mathf.Abs(upDot) < 0.2f)
            {
                if (sideDot > 0) wallLeft = true;
                if (sideDot < 0) wallRight = true;
            }
        }

        isTouchingWall = wallLeft || wallRight;

        if (isTouchingWall)
        {
            wallContactTimer = 0.1f;
            if (wallLeft && vAlong < 0) vAlong = 0;
            if (wallRight && vAlong > 0) vAlong = 0;
        }
        else
        {
            wallContactTimer -= Time.fixedDeltaTime;
        }

        if (wallContactTimer > 0f) coyoteTimer = -1f;
    }

    void ApplyHorizontal(ref float vAlong)
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float targetSpeed = inputX * maxSpeed;
        float speedDiff = targetSpeed - vAlong;

        float accel = isGrounded ? acceleration : airAcceleration;
        if (Mathf.Abs(inputX) < 0.01f) accel = deceleration;
        if (Mathf.Abs(inputX) > 0.01f && Mathf.Sign(inputX) != Mathf.Sign(vAlong))
            accel *= turnBoost;

        vAlong += Mathf.Sign(speedDiff)
                  * Mathf.Min(Mathf.Abs(speedDiff), accel * Time.fixedDeltaTime);
    }

    void ApplyGravity(ref float vUp)
    {
        if (isGrounded && vUp < 0)
        {
            vUp = -1f;
            return;
        }

        float mult = (vUp < 0 || !jumpHeld) ? fallGravityMult : 1f;
        vUp -= gravityScale * mult * Mathf.Abs(Physics2D.gravity.y) * Time.fixedDeltaTime;
    }

    void TryJump(ref float vUp)
    {
        bool canJump = (isGrounded || coyoteTimer > 0f) && wallContactTimer <= 0f;
        if (jumpBufferTimer > 0f && canJump)
        {
            vUp = jumpForce;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }
    }
}
