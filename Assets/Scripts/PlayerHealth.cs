using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth;

    [Header("State")]
    [SerializeField] private bool isInvincible = false;
    [SerializeField] private bool isDead = false;

    [Header("Flash Effect")]
    [SerializeField] private float flashInterval = 0.1f;
    [SerializeField] private int flashCount = 6;

    [Header("Death")]
    [SerializeField] private float restartDelay = 0.5f;

    [Header("Events")]
    public UnityEvent<int, int> onHealthChanged;
    public UnityEvent onDeath;

    private SpriteRenderer[] spriteRenderers;

    private void Awake()
    {
        currentHealth = maxHealth;

        // 获取自己和子物体上的所有 SpriteRenderer
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            currentHealth = maxHealth;
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (isDead) return;
        if (isInvincible) return;

        int finalDamage = damageInfo.instantKill ? currentHealth : damageInfo.amount;

        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(InvincibleFlashCoroutine(damageInfo.invincibleTime));
    }

    private IEnumerator InvincibleFlashCoroutine(float invincibleTime)
    {
        isInvincible = true;

        float timer = 0f;

        while (timer < invincibleTime)
        {
            SetSpriteVisible(false);
            yield return new WaitForSeconds(flashInterval);

            SetSpriteVisible(true);
            yield return new WaitForSeconds(flashInterval);

            timer += flashInterval * 2f;
        }

        SetSpriteVisible(true);
        isInvincible = false;
    }

    private void SetSpriteVisible(bool visible)
    {
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            if (sr != null)
            {
                sr.enabled = visible;
            }
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        onDeath?.Invoke();

        StartCoroutine(RestartLevelCoroutine());
    }

    private IEnumerator RestartLevelCoroutine()
    {
        yield return new WaitForSeconds(restartDelay);

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}