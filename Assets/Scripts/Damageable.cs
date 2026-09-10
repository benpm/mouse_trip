using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class Damageable : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = 3;

    [Header("Hit Response")]
    [SerializeField, Min(0f)] private float invincibilityDuration = 0.8f;
    [SerializeField, Min(0.01f)] private float flashInterval = 0.1f;
    [SerializeField, Min(0f)] private float knockbackSpeed = 6.5f;

    [Header("Death")]
    [SerializeField, Min(0f)] private float restartDelay = 0.5f;

    public int CurrentHealth { get; private set; }
    public bool IsInvincible { get; private set; }

    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private SpriteRenderer[] spriteRenderers;
    private Coroutine flashCoroutine;
    private bool isDead;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        CurrentHealth = maxHealth;
    }

    private void OnDisable()
    {
        SetSpritesVisible(true);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryTakeTileDamage(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryTakeTileDamage(collision);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTakeTileDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryTakeTileDamage(other);
    }

    public void TakeDamage(int amount, Vector2 knockbackDirection)
    {
        if (isDead || IsInvincible || amount <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        ApplyKnockback(knockbackDirection);

        if (CurrentHealth == 0)
        {
            Die();
            return;
        }

        IsInvincible = true;
        flashCoroutine = StartCoroutine(FlashWhileInvincible());
    }

    private void TryTakeTileDamage(Collision2D collision)
    {
        if (isDead || IsInvincible)
        {
            return;
        }

        foreach (ContactPoint2D contact in collision.contacts)
        {
            Tilemap tilemap = contact.collider.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                tilemap = contact.otherCollider.GetComponent<Tilemap>();
            }

            if (tilemap == null)
            {
                continue;
            }

            Vector2 knockbackDirection = contact.normal;
            Vector2 towardPlayer = (Vector2)transform.position - contact.point;
            if (Vector2.Dot(knockbackDirection, towardPlayer) < 0f)
            {
                knockbackDirection = -knockbackDirection;
            }

            Vector2 samplePoint = contact.point - (knockbackDirection.normalized * 0.01f);
            if (TryTakeTileDamage(tilemap, samplePoint, knockbackDirection))
            {
                return;
            }
        }
    }

    private void TryTakeTileDamage(Collider2D other)
    {
        if (isDead || IsInvincible)
        {
            return;
        }

        Tilemap tilemap = other.GetComponent<Tilemap>();
        if (tilemap == null)
        {
            return;
        }

        ColliderDistance2D distance = Physics2D.Distance(bodyCollider, other);
        if (!distance.isValid)
        {
            return;
        }

        Vector2 knockbackDirection = distance.normal;
        if (knockbackDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            knockbackDirection = (Vector2)transform.position - (Vector2)other.bounds.center;
        }

        Vector2 samplePoint = distance.pointB - (knockbackDirection.normalized * 0.01f);
        if (!TryTakeTileDamage(tilemap, samplePoint, knockbackDirection))
        {
            TryTakeOverlappingTileDamage(tilemap, knockbackDirection);
        }
    }

    private bool TryTakeTileDamage(Tilemap tilemap, Vector2 samplePoint, Vector2 knockbackDirection)
    {
        Vector3Int cellPosition = tilemap.WorldToCell(samplePoint);
        GameplayTile tile = tilemap.GetTile(cellPosition) as GameplayTile;

        if (tile == null || !tile.damagesPlayer || tile.damage <= 0)
        {
            return false;
        }

        TakeDamage(tile.damage, knockbackDirection);
        return true;
    }

    private bool TryTakeOverlappingTileDamage(Tilemap tilemap, Vector2 knockbackDirection)
    {
        Bounds bounds = bodyCollider.bounds;
        Vector3Int minCell = tilemap.WorldToCell(bounds.min + (Vector3.one * 0.001f));
        Vector3Int maxCell = tilemap.WorldToCell(bounds.max - (Vector3.one * 0.001f));

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                GameplayTile tile = tilemap.GetTile(new Vector3Int(x, y, minCell.z)) as GameplayTile;
                if (tile == null || !tile.damagesPlayer || tile.damage <= 0)
                {
                    continue;
                }

                TakeDamage(tile.damage, knockbackDirection);
                return true;
            }
        }

        return false;
    }

    private void ApplyKnockback(Vector2 direction)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon || knockbackSpeed <= 0f)
        {
            return;
        }

        body.linearVelocity = direction.normalized * knockbackSpeed;
    }

    private IEnumerator FlashWhileInvincible()
    {
        float elapsed = 0f;

        while (elapsed < invincibilityDuration)
        {
            SetSpritesVisible(false);
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;

            SetSpritesVisible(true);
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        SetSpritesVisible(true);
        IsInvincible = false;
        flashCoroutine = null;
    }

    private void SetSpritesVisible(bool visible)
    {
        if (spriteRenderers == null)
        {
            return;
        }

        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visible;
            }
        }
    }

    private void Die()
    {
        isDead = true;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        SetSpritesVisible(true);
        StartCoroutine(RestartLevel());
    }

    private IEnumerator RestartLevel()
    {
        yield return new WaitForSeconds(restartDelay);

        Scene scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0)
        {
            SceneManager.LoadScene(scene.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(scene.name);
        }
    }
}
