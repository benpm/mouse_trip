public interface IDamageable
{
    /// <returns>true if the damage was applied (not blocked by death/invincibility).</returns>
    bool TakeDamage(DamageInfo damageInfo);
}
