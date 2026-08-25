using UnityEngine;

[CreateAssetMenu(fileName = "NewDamageProfile", menuName = "Game/Damage Profile")]
public class DamageProfile : ScriptableObject
{
    [Header("Damage Info")]
    public string damageId;
    public int damageAmount = 1;
    public bool instantKill = false;

    [Header("Timing")]
    public float damageCooldown = 0.8f;
    public float invincibleTimeAfterHit = 0.8f;

    [Header("Knockback")]
    public Vector2 knockback = new Vector2(4f, 5f);

    public DamageInfo CreateDamageInfo(GameObject source, Vector2 direction)
    {
        Vector2 finalKnockback = new Vector2(
            knockback.x * direction.x,
            knockback.y
        );

        return new DamageInfo
        {
            source = source,
            amount = damageAmount,
            instantKill = instantKill,
            invincibleTime = invincibleTimeAfterHit,
            knockback = finalKnockback
        };
    }
}