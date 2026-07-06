using UnityEngine;

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

    public Vector2 gravityDir = Vector2.down;
    // Privates

    Rigidbody2D rb;
    Vector2 velocity;
    bool isGrounded;
    bool jumpHeld;
    bool jumpCutQueued;
    float coyoteTimer;
    float jumpBufferTimer;
    bool isTouchingWall;
    float wallContactTimer;

    // Gravity frame, similar to an ortho-normal basis. 
    Vector2 Up => -gravityDir;
    Vector2 RightAxis => Vector2.Perpendicular(gravityDir); // (1,0) when gravity is down

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Jump buffer — pressed just before landing
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

    void UpdateGravityDirection()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 centerWorld = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));

        Vector2 dir = mouseWorld - centerWorld;
        if (dir.sqrMagnitude < 0.0001f) return; 

        Vector2 targetDir = dir.normalized;

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

        // Decompose world velocity into the gravity frame:
        float vAlong = Vector2.Dot(velocity, RightAxis);
        float vUp = Vector2.Dot(velocity, Up);

        if (jumpCutQueued)
        {
            if (vUp > 0f) vUp *= jumpCutMultiplier;
            jumpCutQueued = false;
        }

        CheckContacts(ref vAlong);
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

        // at the very end of FixedUpdate, after rb.linearVelocity = velocity;
        Debug.Log($"WROTE {velocity}  |  READ-BACK next frame: {rb.linearVelocity}");
    }

    void CheckContacts(ref float vAlong)
    {
        ContactPoint2D[] contacts = new ContactPoint2D[8];
        var filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayer);
        filter.useLayerMask = true;
        int count = rb.GetContacts(filter, contacts);

        bool groundContact = false;
        bool wallLeft = false, wallRight = false;

        for (int i = 0; i < count; i++)
        {
            Vector2 normal = contacts[i].normal;
            float upDot = Vector2.Dot(normal, Up);
            float sideDot = Vector2.Dot(normal, RightAxis);

            if (upDot > 0.5f)
            {
                groundContact = true;
            }
            else if (Mathf.Abs(sideDot) > 0.9f && Mathf.Abs(upDot) < 0.2f)
            {
                if (sideDot > 0) wallLeft = true;
                if (sideDot < 0) wallRight = true;
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

    void CheckWall(ref float vAlong)
    {
        ContactPoint2D[] contacts = new ContactPoint2D[8];
        int count = rb.GetContacts(contacts);

        bool wallLeft = false;
        bool wallRight = false;

        for (int i = 0; i < count; i++)
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

        bool nearWall = wallContactTimer > 0f;
        if (nearWall) coyoteTimer = -1f;
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