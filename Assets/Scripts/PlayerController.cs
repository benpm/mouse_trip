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

    [Header("Debug")]
    [Tooltip("Log contacts / jump state each physics step and draw contact normals")]
    [SerializeField] bool debugContacts = false;

    public Vector2 gravityDir = Vector2.down;

    Rigidbody2D rb;
    readonly ContactPoint2D[] contacts = new ContactPoint2D[16];
    readonly Vector2[] orientedContactNormals = new Vector2[16];
    int contactCount;
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
        rb.gravityScale = 0f; // gravity is applied manually in ApplyGravity
    }

    void Update()
    {
        // Jump buffer - pressed just before landing
        if (Input.GetButtonDown("Jump"))
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - Time.deltaTime);
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
        Vector2 targetDir = useMouseGravityDir ? GetMouseGravityDir() : gravityDir;

        if (gravityTurnSpeed <= 0f)
        {
            gravityDir = targetDir;
        }
        else
        {
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

    // Unity does not document whether ContactPoint2D.collider or .otherCollider is this body
    // for Rigidbody2D.GetContacts, nor which way the normal points. Resolve both explicitly:
    // the normal is flipped (if needed) so it always points from the surface into the player,
    // which for a convex collider means Dot(normal, centerOfMass - point) > 0.
    void CheckContacts(ref float vAlong)
    {
        contactCount = rb.GetContacts(contacts);

        bool groundContact = false;
        bool wallLeft = false, wallRight = false;
        Vector2 com = rb.worldCenterOfMass;

        for (int i = 0; i < contactCount; i++)
        {
            ContactPoint2D contact = contacts[i];
            Vector2 normal = contact.normal;
            if (Vector2.Dot(normal, com - contact.point) < 0f) normal = -normal;
            orientedContactNormals[i] = normal;
            float upDot = Vector2.Dot(normal, Up);
            float sideDot = Vector2.Dot(normal, RightAxis);

            Collider2D other = contact.collider.attachedRigidbody == rb ? contact.otherCollider : contact.collider;
            if (other && other.TryGetComponent(out Tilemap tilemap))
            {
                // Nudge the hit point slightly inside the tile (against the oriented normal)
                Vector3Int cell = tilemap.WorldToCell(contact.point - normal * 0.01f);
                if (tilemap.GetTile(cell) is GameplayTile tile && tile.damagesPlayer)
                {
                    ///TODO: Player takes damage
                    Debug.Log($"Player took {tile.damage} damage from tile at {cell}");
                }
            }

            if (debugContacts)
                Debug.Log($"contact[{i}] other={(other ? other.name : "null")} n={normal} up={upDot:F2} side={sideDot:F2}");

            if (upDot > 0.5f)
                groundContact = true;
            else if (Mathf.Abs(sideDot) > 0.9f && Mathf.Abs(upDot) < 0.2f)
            {
                if (sideDot > 0) wallLeft = true;
                if (sideDot < 0) wallRight = true;
            }
        }

        isGrounded = groundContact;
        if (groundContact) coyoteTimer = coyoteTime;
        else coyoteTimer = Mathf.Max(0f, coyoteTimer - Time.fixedDeltaTime);

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

        // No coyote jumps off walls; grounded jumps are still allowed (see TryJump)
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
        bool canJump = isGrounded || coyoteTimer > 0f;
        if (debugContacts && jumpBufferTimer > 0f)
            Debug.Log($"jump requested: grounded={isGrounded} coyote={coyoteTimer:F3} wallTimer={wallContactTimer:F3} canJump={canJump}");
        if (jumpBufferTimer > 0f && canJump)
        {
            vUp = jumpForce;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            if (debugContacts) Debug.Log($"JUMP vUp={vUp}");
        }
    }

    void OnDrawGizmos()
    {
        if (!debugContacts || !Application.isPlaying) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        for (int i = 0; i < contactCount; i++)
        {
            Gizmos.DrawSphere(contacts[i].point, 0.05f);
            Gizmos.DrawLine(contacts[i].point, contacts[i].point + orientedContactNormals[i] * 0.5f);
        }
    }
}
