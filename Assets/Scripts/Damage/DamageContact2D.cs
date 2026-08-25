using System.Collections.Generic;
using UnityEngine;

public class DamageContact2D : MonoBehaviour
{
    [Header("Damage Setting")]
    [SerializeField] private DamageProfile damageProfile;

    [Header("Target")]
    [SerializeField] private LayerMask targetLayer;

    [Header("Damage Mode")]
    [SerializeField] private bool damageOnEnter = true;
    [SerializeField] private bool damageOnStay = false;

    private readonly Dictionary<IDamageable, float> lastDamageTime = new Dictionary<IDamageable, float>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!damageOnEnter) return;
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!damageOnStay) return;
        TryDamage(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!damageOnEnter) return;
        TryDamage(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!damageOnStay) return;
        TryDamage(collision.collider);
    }

    private void TryDamage(Collider2D other)
    {
        if (damageProfile == null) return;

        if (!IsInTargetLayer(other.gameObject)) return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();

        if (damageable == null) return;

        if (!CanDamageNow(damageable)) return;

        Vector2 direction = GetDamageDirection(other.transform);

        DamageInfo damageInfo = damageProfile.CreateDamageInfo(gameObject, direction);

        damageable.TakeDamage(damageInfo);

        lastDamageTime[damageable] = Time.time;
    }

    private bool CanDamageNow(IDamageable damageable)
    {
        if (!lastDamageTime.ContainsKey(damageable)) return true;

        float lastTime = lastDamageTime[damageable];
        float cooldown = damageProfile.damageCooldown;

        return Time.time - lastTime >= cooldown;
    }

    private Vector2 GetDamageDirection(Transform target)
    {
        float xDirection = target.position.x >= transform.position.x ? 1f : -1f;
        return new Vector2(xDirection, 1f);
    }

    private bool IsInTargetLayer(GameObject target)
    {
        return (targetLayer.value & (1 << target.layer)) != 0;
    }
}